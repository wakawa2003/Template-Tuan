#!/usr/bin/env python3
"""Rebuild the Unity object index in one command, and keep it fresh via git hooks.

Steps: run ``tools/unity_object_index.py`` (parses every Unity YAML file and
writes ``.unity-index/index.json``). A lock file stops overlapping runs
(commit + merge + checkout can fire back to back), and each run appends one line
to ``.unity-index/refresh.log``.

Git hooks installed by ``--install-hooks`` call ``--spawn``, which detaches the
work so ``git commit`` returns immediately.

Usage:
    python tools/unity_refresh.py                  # run now, foreground
    python tools/unity_refresh.py --status         # index mtime, lock, hooks
    python tools/unity_refresh.py --install-hooks  # post-commit/merge/checkout
    python tools/unity_refresh.py --uninstall-hooks
"""

from __future__ import annotations

import argparse
import datetime as dt
import os
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / ".unity-index"
INDEX = OUT / "index.json"
LOG = OUT / "refresh.log"
LOCK = OUT / "refresh.lock"
INDEXER = ROOT / "tools" / "unity_object_index.py"

HOOK_NAMES = ("post-commit", "post-merge", "post-checkout")
HOOK_BEGIN = "# >>> unity-index (managed by tools/unity_refresh.py) >>>"
HOOK_END = "# <<< unity-index (managed by tools/unity_refresh.py) <<<"
HOOK_BODY = f"""{HOOK_BEGIN}
root="$(git rev-parse --show-toplevel 2>/dev/null)"
if [ -n "$root" ] && [ -f "$root/tools/unity_refresh.py" ]; then
  for py in python python3 py; do
    if command -v "$py" >/dev/null 2>&1; then
      ( cd "$root" && "$py" tools/unity_refresh.py --spawn ) >/dev/null 2>&1 || true
      break
    fi
  done
fi
{HOOK_END}"""

LOCK_TTL = 900  # seconds; a crashed run must not block refresh forever


def log(message: str) -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    stamp = dt.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    line = f"[{stamp}] {message}\n"
    try:
        if LOG.exists() and LOG.stat().st_size > 256_000:
            LOG.write_text(line, encoding="utf-8")
        else:
            with LOG.open("a", encoding="utf-8") as handle:
                handle.write(line)
    except OSError:
        pass


def say(message: str, quiet: bool) -> None:
    if not quiet:
        print(message)


def acquire_lock() -> bool:
    OUT.mkdir(parents=True, exist_ok=True)
    if LOCK.exists():
        try:
            if time.time() - LOCK.stat().st_mtime < LOCK_TTL:
                return False
        except OSError:
            pass
        LOCK.unlink(missing_ok=True)
    try:
        handle = os.open(LOCK, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
        os.write(handle, str(os.getpid()).encode())
        os.close(handle)
        return True
    except FileExistsError:
        return False


def release_lock() -> None:
    LOCK.unlink(missing_ok=True)


def refresh(quiet: bool = False) -> int:
    if not acquire_lock():
        say("[unity-index] another refresh is running, skipped", quiet)
        log("skipped: lock held by another run")
        return 0
    try:
        started = time.perf_counter()
        try:
            proc = subprocess.run(
                [sys.executable, str(INDEXER)], cwd=ROOT, capture_output=True,
                text=True, encoding="utf-8", errors="replace", timeout=600,
            )
        except (OSError, subprocess.TimeoutExpired) as exc:
            log(f"index: FAILED ({exc})")
            say(f"[unity-index] index: FAILED ({exc})", quiet)
            return 1
        took = time.perf_counter() - started
        output = ((proc.stdout or "") + (proc.stderr or "")).strip()
        tail = output.splitlines()[-1] if output else ""
        if proc.returncode != 0:
            log(f"index: exit {proc.returncode} in {took:.2f}s | {tail}")
            say(f"[unity-index] index: exit {proc.returncode} ({took:.2f}s) - {tail}", quiet)
            return 1
        log(f"index: ok in {took:.2f}s | {tail}")
        say(f"[unity-index] index: ok ({took:.2f}s)", quiet)
        return 0
    finally:
        release_lock()


def spawn() -> int:
    OUT.mkdir(parents=True, exist_ok=True)
    args = [sys.executable, str(Path(__file__).resolve()), "--worker", "--quiet"]
    kwargs = dict(
        cwd=ROOT, stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL, close_fds=True,
    )
    if os.name == "nt":
        kwargs["creationflags"] = 0x00000008 | 0x00000200  # DETACHED_PROCESS | CREATE_NEW_PROCESS_GROUP
    else:
        kwargs["start_new_session"] = True
    try:
        subprocess.Popen(args, **kwargs)
        return 0
    except OSError as exc:
        log(f"spawn failed: {exc}")
        return 1


def hooks_dir() -> Path | None:
    """<repo>/.git/hooks, or None when this folder is not a git repository."""
    proc = subprocess.run(
        ["git", "rev-parse", "--git-path", "hooks"], cwd=ROOT,
        capture_output=True, text=True,
    )
    if proc.returncode != 0:
        return None
    rel = (proc.stdout or "").strip()
    if not rel:
        return None
    return (ROOT / rel).resolve()


def hook_path(name: str) -> Path | None:
    directory = hooks_dir()
    return None if directory is None else directory / name


def install_hooks() -> int:
    if hooks_dir() is None:
        print("[hooks] skipped: not a git repository (index still works, run git init to get auto refresh)")
        return 0
    for name in HOOK_NAMES:
        path = hook_path(name)
        path.parent.mkdir(parents=True, exist_ok=True)
        existing = path.read_text(encoding="utf-8", errors="replace") if path.exists() else ""
        if HOOK_BEGIN in existing:
            print(f"[hooks] {name}: already installed")
            continue
        body = existing
        if HOOK_BEGIN in body or HOOK_END in body:
            body = body.replace(HOOK_END, "").replace(HOOK_BEGIN, "")
        if not body.strip():
            body = "#!/bin/sh\n"
        elif not body.endswith("\n"):
            body += "\n"
        path.write_text(body + HOOK_BODY + "\n", encoding="utf-8", newline="\n")
        try:
            path.chmod(path.stat().st_mode | 0o111)
        except OSError:
            pass
        print(f"[hooks] {name}: installed at {path}")
    return 0


def uninstall_hooks() -> int:
    if hooks_dir() is None:
        print("[hooks] nothing to uninstall: not a git repository")
        return 0
    for name in HOOK_NAMES:
        path = hook_path(name)
        if not path.exists():
            continue
        text = path.read_text(encoding="utf-8", errors="replace")
        if HOOK_BEGIN not in text:
            continue
        start = text.index(HOOK_BEGIN)
        end = text.index(HOOK_END, start) + len(HOOK_END)
        cleaned = (text[:start] + text[end:]).rstrip("\n") + "\n"
        if cleaned.strip() == "#!/bin/sh":
            path.unlink()
            print(f"[hooks] {name}: removed (left empty, deleted file)")
            continue
        path.write_text(cleaned, encoding="utf-8", newline="\n")
        print(f"[hooks] {name}: removed")
    return 0


def status() -> int:
    print(f"index      : {'yes' if INDEX.exists() else 'no'}")
    if INDEX.exists():
        stamp = dt.datetime.fromtimestamp(INDEX.stat().st_mtime)
        print(f"built at   : {stamp:%Y-%m-%d %H:%M:%S}")
        print(f"size       : {INDEX.stat().st_size / 1024:.0f} KB")
    print(f"lock       : {'HELD' if LOCK.exists() else 'free'}")
    if hooks_dir() is None:
        print("hooks      : n/a (not a git repository)")
        return 0
    for name in HOOK_NAMES:
        path = hook_path(name)
        installed = path.exists() and HOOK_BEGIN in path.read_text(encoding="utf-8", errors="replace")
        print(f"hook {name:<14}: {'installed' if installed else 'missing'}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument("--spawn", action="store_true", help="start a detached refresh and return")
    parser.add_argument("--worker", action="store_true", help=argparse.SUPPRESS)
    parser.add_argument("--quiet", action="store_true", help="no console output")
    parser.add_argument("--install-hooks", action="store_true")
    parser.add_argument("--uninstall-hooks", action="store_true")
    parser.add_argument("--status", action="store_true")
    args = parser.parse_args()

    if args.install_hooks:
        return install_hooks()
    if args.uninstall_hooks:
        return uninstall_hooks()
    if args.status:
        return status()
    if args.spawn:
        return spawn()
    return refresh(quiet=args.quiet)


if __name__ == "__main__":
    raise SystemExit(main())
