#!/usr/bin/env python3
"""Query the Unity object index built by ``tools/unity_object_index.py``.

Every command answers from ``.unity-index/index.json`` (2.4k objects, no
Unity editor needed) and prints compact text, so it stays cheap in tokens.

Commands:
    tree   <file>            hierarchy + attached scripts (default depth 8)
    find   <text>            objects, scripts, fields and assets matching text
    obj    <file>#<id|path>  serialized fields and references of one object
    refs   <asset>           every object that references an asset (reverse deps)
    deps   <file>            assets referenced by a file
    stats                    index summary

Examples:
    python tools/unity_query.py tree "Services.prefab"
    python tools/unity_query.py find "Magnet"
    python tools/unity_query.py refs "Magnet Ring Booster.asset"
    python tools/unity_query.py obj "Services.prefab::Services/ADS manager"
"""

from __future__ import annotations

import argparse
import json
import os
import sys
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
INDEX = ROOT / ".unity-index" / "index.json"


def _newer_than(path_mtime: float) -> tuple[int, str]:
    """(#Unity files newer than the index, newest path).

    A non-zero count means the answer would come from stale data (usually: an
    asset saved in the editor but not committed yet). The scan only looks at the
    files the indexer would parse, so vendored folders and oversized atlases do
    not keep the index permanently "stale".
    """
    if str(ROOT / "tools") not in sys.path:
        sys.path.insert(0, str(ROOT / "tools"))
    try:
        from unity_object_index import IGNORE_DIRS, MAX_BYTES, ROOTS, UNITY_EXTENSIONS
    except Exception:  # indexer renamed or broken: never block a query
        return 0, ""

    count = 0
    newest_path = ""
    newest = path_mtime
    stack = [ROOT / root for root in ROOTS if (ROOT / root).is_dir()]
    while stack:
        folder = stack.pop()
        try:
            entries = list(os.scandir(folder))
        except OSError:
            continue
        for entry in entries:
            try:
                if entry.is_dir(follow_symlinks=False):
                    rel = Path(entry.path).relative_to(ROOT).as_posix()
                    if not any(rel == d or rel.startswith(d + "/") for d in IGNORE_DIRS):
                        stack.append(Path(entry.path))
                    continue
                if not entry.name.lower().endswith(UNITY_EXTENSIONS):
                    continue
                stat = entry.stat()
                if stat.st_size > MAX_BYTES:
                    continue
            except OSError:
                continue
            if stat.st_mtime > path_mtime:
                count += 1
                if stat.st_mtime > newest:
                    newest = stat.st_mtime
                    newest_path = Path(entry.path).relative_to(ROOT).as_posix()
    return count, newest_path


def load_index() -> dict:
    if not INDEX.exists():
        print(
            f"index not found: {INDEX}\nrun `python tools/unity_object_index.py` first",
            file=sys.stderr,
        )
        raise SystemExit(1)
    index_mtime = INDEX.stat().st_mtime
    index = json.loads(INDEX.read_text(encoding="utf-8"))
    stale, newest = _newer_than(index_mtime)
    index["_stale"] = {"files": stale, "newest": newest}
    if stale:
        print(
            f"[unity-index] STALE: {stale} Unity file(s) newer than the index "
            f"(newest: {newest})\n"
            f"[unity-index] answers below may miss those edits - run "
            f"`python tools/unity_refresh.py` and repeat the query",
            file=sys.stderr,
        )
    return index


def _script_name(record: dict) -> str:
    script = record.get("script")
    return Path(script).stem if script else ""


def _match_files(index: dict, needle: str) -> list[str]:
    needle = needle.replace("\\", "/").strip()
    files = list(index["files"])
    for test in (
        lambda f: f == needle,
        lambda f: f.casefold() == needle.casefold(),
        lambda f: f.casefold().endswith("/" + needle.casefold()),
        lambda f: Path(f).stem.casefold() == Path(needle).stem.casefold()
        and Path(f).suffix.casefold() == (Path(needle).suffix.casefold() or Path(f).suffix.casefold()),
        lambda f: needle.casefold() in f.casefold(),
    ):
        hits = [f for f in files if test(f)]
        if hits:
            return sorted(hits)
    return []


def _one_file(index: dict, needle: str) -> str:
    hits = _match_files(index, needle)
    if not hits:
        raise SystemExit(f"no Unity file matches {needle!r}")
    if len(hits) > 1:
        print(f"ambiguous {needle!r}, using {hits[0]} (also: {', '.join(hits[1:6])})", file=sys.stderr)
    return hits[0]


def cmd_stats(index: dict, _args) -> int:
    counts = index["counts"]
    for key in ("files", "skipped_files", "objects", "gameobjects", "monobehaviours", "references", "guids"):
        print(f"{key:<16}{counts.get(key, 0)}")
    print(f"{'indexed':<16}{index['generated']}")
    print(f"{'index size':<16}{INDEX.stat().st_size / 1024:.0f} KB")
    stale = index.get("_stale") or {}
    newer = stale.get("files", 0)
    print(f"{'stale files':<16}{newer}" + (f"  (newest: {stale.get('newest')})" if newer else ""))
    return 0


def cmd_tree(index: dict, args) -> int:
    rel = _one_file(index, args.file)
    objects = index["objects"]
    prefix = rel + "#"

    children: dict[str, list[str]] = defaultdict(list)
    roots: list[str] = []
    scripts: dict[str, list[str]] = defaultdict(list)
    others: dict[str, int] = defaultdict(int)

    for key, record in objects.items():
        if not key.startswith(prefix):
            continue
        if record.get("class") == "GameObject":
            parent = record.get("parent")
            if parent and parent in objects:
                children[parent].append(key)
            else:
                roots.append(key)
        elif record.get("go"):
            name = _script_name(record)
            if name:
                scripts[record["go"]].append(name)
            else:
                others[record["go"]] += 1

    print(f"{rel} | {index['files'][rel]['objects']} objects, {len(roots)} root GameObject(s)")
    lines = 0
    budget = args.max_lines

    def walk(key: str, depth: int, indent: str, is_last: bool, base: str):
        nonlocal lines
        if lines >= budget:
            return
        record = objects[key]
        label = record.get("name") or "?"
        branch = "" if depth == 0 else ("└─ " if is_last else "├─ ")
        extra = ""
        if scripts.get(key):
            extra = "  ← " + ", ".join(sorted(set(scripts[key])))
        elif others.get(key):
            extra = f"  [{others[key]} builtin]"
        print(f"{indent}{branch}{label}{extra}")
        lines += 1
        if depth >= args.depth:
            kids = children.get(key) or []
            if kids:
                print(f"{indent}{'   ' if is_last else '│  '}… {len(kids)} child object(s), use --depth")
            return
        kids = sorted(children.get(key) or [])
        child_indent = indent if depth == 0 else indent + ("   " if is_last else "│  ")
        for i, kid in enumerate(kids):
            walk(kid, depth + 1, child_indent, i == len(kids) - 1, base)

    for i, root in enumerate(sorted(roots)):
        walk(root, 0, "", i == len(roots) - 1, "")
    if lines >= budget:
        print(f"… truncated at {budget} lines")
    return 0


def cmd_find(index: dict, args) -> int:
    needle = args.text.casefold()
    hits: list[tuple[int, str]] = []
    for key, record in index["objects"].items():
        parts = [
            record.get("name") or "",
            record.get("path") or "",
            _script_name(record),
            " ".join(record.get("fields") or {}),
            " ".join(str(v) for v in (record.get("fields") or {}).values()),
        ]
        haystack = " ".join(parts).casefold()
        if needle not in haystack:
            continue
        score = 0
        if needle in (record.get("name") or "").casefold() or needle in (record.get("path") or "").casefold():
            score += 2
        if needle in _script_name(record).casefold():
            score += 2
        hits.append((score, key))
    hits.sort(key=lambda item: (-item[0], item[1]))
    if not hits:
        print(f"no match for {args.text!r}")
        return 1

    shown = 0
    for _score, key in hits[: args.limit]:
        record = index["objects"][key]
        cls = record.get("class")
        if cls == "GameObject":
            print(f"{key}  [GameObject]  {record.get('path')}")
        else:
            owner = index["objects"].get(record.get("go") or "", {})
            where = owner.get("path") or key.split("#", 1)[0].rsplit("/", 1)[-1]
            script = _script_name(record) or cls
            print(f"{key}  [{script}]  {where}")
        shown += 1
    if len(hits) > shown:
        print(f"… {len(hits) - shown} more match(es), raise --limit")
    return 0


def _resolve_object(index: dict, spec: str) -> str:
    if "#" in spec:
        rel_part, ident = spec.split("#", 1)
        rel = _one_file(index, rel_part)
        key = f"{rel}#{ident}"
        if key in index["objects"]:
            return key
        raise SystemExit(f"no object {ident!r} in {rel}")

    rel_part, sep, path = spec.partition("::")
    rel = _one_file(index, rel_part)
    prefix = rel + "#"
    own = {k: v for k, v in index["objects"].items() if k.startswith(prefix)}

    if sep:
        needle = path.strip().casefold()
        for key, record in own.items():
            if record.get("class") == "GameObject" and (record.get("path") or "").casefold() == needle:
                return key
        # Also accept "<path>/<ScriptName>" for a component on that object.
        for key, record in own.items():
            if record.get("class") == "GameObject":
                continue
            owner = own.get(record.get("go") or "", {})
            label = f"{owner.get('path') or ''}/{_script_name(record)}".strip("/")
            if label.casefold() == needle:
                return key
        for key, record in own.items():
            if record.get("class") == "GameObject" and needle in (record.get("path") or "").casefold():
                return key
        raise SystemExit(f"no object at {path!r} in {rel}")

    # Bare file: the main asset object (first document that is not a component).
    for key, record in own.items():
        if record.get("class") not in ("GameObject", "Transform", "RectTransform"):
            return key
    for key, record in own.items():
        return key
    raise SystemExit(f"{rel} has no indexed objects")


def _pretty(name: str) -> str:
    if name.startswith("<") and ">k__BackingField" in name:
        return name[1 : name.index(">")]
    return name


def _print_fields(record: dict, indent: str = "    ") -> None:
    fields = record.get("fields") or {}
    shown = False
    for name in sorted(fields):
        print(f"{indent}{_pretty(name)} = {fields[name]}")
        shown = True
    if not shown:
        print(f"{indent}(no serialized fields)")


def cmd_obj(index: dict, args) -> int:
    key = _resolve_object(index, args.spec)
    record = index["objects"][key]
    class_name = record.get("class")
    if class_name == "MonoBehaviour" and key.split("#", 1)[0].endswith(".asset"):
        class_name = "ScriptableObject"
    print(f"{key}")
    print(f"  class    {class_name}")
    if record.get("path"):
        print(f"  path     {record['path']}")
    if record.get("script"):
        print(f"  script   {record['script']}")
    if record.get("go"):
        owner = index["objects"].get(record["go"], {})
        print(f"  attached {record['go']} ({owner.get('path')})")
    if record.get("fields") or record.get("refs"):
        print("  fields")
        _print_fields(record)

    if record.get("class") == "GameObject":
        for component_key, component in index["objects"].items():
            if component.get("go") != key:
                continue
            if not (component.get("script") or component.get("fields") or component.get("refs")):
                continue
            label = _script_name(component) or component.get("class")
            print(f"  {label} ({component_key.split('#', 1)[1]})")
            _print_fields(component, "      ")

    if not record.get("fields") and not record.get("refs") and not record.get("children"):
        print("  (no children, no serialized fields)")
    return 0


def cmd_refs(index: dict, args) -> int:
    matches = _match_files(index, args.asset)
    if not matches:
        matches = [p for p in index["guids"].values() if args.asset.casefold() in p.casefold()]
    if not matches:
        raise SystemExit(f"no indexed asset matches {args.asset!r}")

    targets = set(matches)
    per_file: dict[str, list[tuple[str, str]]] = defaultdict(list)
    for key, record in index["objects"].items():
        for ref in record.get("refs") or []:
            if ref["path"] not in targets:
                continue
            rel = key.split("#", 1)[0]
            owner = index["objects"].get(record.get("go") or "", {})
            where = (
                owner.get("path")
                or _script_name(record)
                or rel.rsplit("/", 1)[-1]
            )
            per_file[rel].append((where, ref["field"]))

    total = sum(len(v) for v in per_file.values())
    print(f"{', '.join(matches[:3])} <- {total} reference(s) in {len(per_file)} file(s)")
    for rel in sorted(per_file):
        print(f"  {rel}")
        for where, field in sorted(set(per_file[rel]))[: args.limit]:
            print(f"    {where}  ({field})")
    return 0


def cmd_deps(index: dict, args) -> int:
    rel = _one_file(index, args.file)
    targets: set[str] = set()
    for key, record in index["objects"].items():
        if not key.startswith(rel + "#"):
            continue
        for ref in record.get("refs") or []:
            if ref["path"] != rel:
                targets.add(ref["path"])
    print(f"{rel} -> {len(targets)} referenced asset(s)")
    for target in sorted(targets):
        print(f"  {target}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    sub = parser.add_subparsers(dest="command", required=True)

    p = sub.add_parser("stats", help="index summary")
    p.set_defaults(func=cmd_stats)

    p = sub.add_parser("tree", help="hierarchy of one prefab/scene")
    p.add_argument("file")
    p.add_argument("--depth", type=int, default=8)
    p.add_argument("--max-lines", type=int, default=300)
    p.set_defaults(func=cmd_tree)

    p = sub.add_parser("find", help="search objects/scripts/fields")
    p.add_argument("text")
    p.add_argument("--limit", type=int, default=25)
    p.set_defaults(func=cmd_find)

    p = sub.add_parser("obj", help="fields of one object (<file>::<path> or <file>#<id>)")
    p.add_argument("spec")
    p.set_defaults(func=cmd_obj)

    p = sub.add_parser("refs", help="who references an asset")
    p.add_argument("asset")
    p.add_argument("--limit", type=int, default=20)
    p.set_defaults(func=cmd_refs)

    p = sub.add_parser("deps", help="what a file references")
    p.add_argument("file")
    p.set_defaults(func=cmd_deps)

    args = parser.parse_args()
    return args.func(load_index(), args)


if __name__ == "__main__":
    raise SystemExit(main())
