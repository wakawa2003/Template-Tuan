#!/usr/bin/env python3
"""Index every object inside Unity's text-serialized files into one JSON file.

Unity stores a `.prefab` / `.unity` / `.asset` as a flat list of YAML documents
keyed by a local ``fileID``; relationships live in ``{fileID: ..., guid: ...}``
dictionaries that mean nothing without the ``.meta`` sidecars. Grep can find an
identifier but cannot rebuild a hierarchy or a reference, so this script does the
parsing once and writes a lookup table:

* every object (GameObject, component, ScriptableObject, prefab instance);
* parent/child hierarchy, with prefab instances re-attached to their parent;
* serialized field values, with object references resolved to a readable name;
* every GUID reference resolved to an asset path through ``.meta`` files.

The result, ``.unity-index/index.json``, is queried by ``tools/unity_query.py``.
Vendor folders are skipped (see ``IGNORE_DIRS``) and files above ``MAX_BYTES``
(font atlases) are skipped; both are reported in ``counts``.

Usage:
    python tools/unity_object_index.py            # rebuild the index
    python tools/unity_object_index.py --dry-run  # counts only, write nothing
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from collections import Counter, defaultdict
from datetime import datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
INDEX_DIR = ROOT / ".unity-index"
INDEX = INDEX_DIR / "index.json"

# Third-party code is not game data; mirroring this in .gitignore is not enough
# because these folders sit inside Assets/. Projects with a different vendor
# layout override this through `tools/unity_index_config.json` (see below).
DEFAULT_IGNORE_DIRS = (
    "Assets/AmplifyShaderEditor",
    "Assets/Plugins",
    "Assets/GoogleMobileAds",
    "Assets/SerializeInterfaces",
    "Assets/Packages",
    "Assets/VFX Game",
    "Packages",
)

DEFAULT_EXTENSIONS = (".asset", ".prefab", ".unity")
DEFAULT_MAX_BYTES = 4 * 1024 * 1024  # font / TMP atlases are not worth indexing
MAX_FIELD_LEN = 100

# Optional per-project settings (versioned, next to the scripts):
#   { "ignore_dirs": ["Assets/ThirdParty"], "extensions": [".asset",".prefab",".unity"],
#     "max_bytes": 4194304, "roots": ["Assets"] }
CONFIG_PATH = ROOT / "tools" / "unity_index_config.json"


def _load_config() -> dict:
    try:
        data = json.loads(CONFIG_PATH.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return {}
    return data if isinstance(data, dict) else {}


_CONFIG = _load_config()


def _setting(key: str, default):
    return _CONFIG[key] if key in _CONFIG else default


IGNORE_DIRS = {str(item).strip("/") for item in _setting("ignore_dirs", DEFAULT_IGNORE_DIRS)}
UNITY_EXTENSIONS = tuple(_setting("extensions", DEFAULT_EXTENSIONS))
MAX_BYTES = int(_setting("max_bytes", DEFAULT_MAX_BYTES))
ROOTS = tuple(_setting("roots", ("Assets",)))

DOC_RE = re.compile(r"^--- !u!(\d+)\s+&(\d+)[^\n]*$", re.M)
TAG_PREAMBLE = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n---\n"

# Serialization plumbing: never interesting, always identical across objects.
SKIP_FIELDS = {
    "m_ObjectHideFlags",
    "m_CorrespondingSourceObject",
    "m_PrefabInstance",
    "m_PrefabAsset",
    "m_GameObject",
    "m_Enabled",
    "m_EditorHideFlags",
    "m_EditorClassIdentifier",
    "m_Script",
    "m_Name",
    "serializedVersion",
    "m_RootOrder",
    "m_LocalEulerAnglesHint",
}

# Components that only describe shape/rendering; their fields are engine
# defaults and would triple the index with noise.
SKIP_FIELD_CLASSES = {
    "Transform",
    "RectTransform",
    "CanvasRenderer",
    "MeshFilter",
    "MeshRenderer",
    "SpriteRenderer",
    "ParticleSystemRenderer",
    "Animator",
    "BoxCollider2D",
    "CircleCollider2D",
    "Rigidbody2D",
}

_RE_NONWORD = re.compile(r"[\W_]+", re.UNICODE)


def normalize(value: str) -> str:
    return _RE_NONWORD.sub("_", value.casefold()).strip("_")


def trim(text: str) -> str:
    text = " ".join(text.split())
    return text if len(text) <= MAX_FIELD_LEN else text[: MAX_FIELD_LEN - 1] + "…"


def load_yaml():
    import yaml

    class UnityLoader(yaml.SafeLoader):
        pass

    def construct(loader, _suffix, node):
        if isinstance(node, yaml.ScalarNode):
            return loader.construct_scalar(node)
        if isinstance(node, yaml.SequenceNode):
            return loader.construct_sequence(node)
        if isinstance(node, yaml.MappingNode):
            return loader.construct_mapping(node)
        return None

    UnityLoader.add_multi_constructor("tag:unity3d.com,2011:", construct)
    UnityLoader.add_multi_constructor("!", construct)
    return yaml, UnityLoader


def is_ignored(rel_posix: str) -> bool:
    return any(rel_posix == d or rel_posix.startswith(d + "/") for d in IGNORE_DIRS)


def iter_unity_files():
    for root in ROOTS:
        base = ROOT / root
        if not base.is_dir():
            continue
        for path in sorted(base.rglob("*")):
            if not path.is_file() or path.suffix.lower() not in UNITY_EXTENSIONS:
                continue
            rel = path.relative_to(ROOT).as_posix()
            if is_ignored(rel):
                continue
            yield path, rel


def guid_index() -> dict[str, str]:
    """guid -> asset path for every .meta under the configured roots."""
    out: dict[str, str] = {}
    for root in ROOTS:
        base = ROOT / root
        if not base.is_dir():
            continue
        for meta in base.rglob("*.meta"):
            rel = meta.relative_to(ROOT).as_posix()
            if is_ignored(rel):
                continue
            try:
                text = meta.read_text(encoding="utf-8", errors="ignore")
            except OSError:
                continue
            match = re.search(r"^guid:\s*([0-9a-fA-F]{32})\s*$", text, re.M)
            if match:
                out[match.group(1).lower()] = rel[: -len(".meta")]
    return out


def split_documents(text: str):
    """Yield (class_id, file_id, class_name, body_text) for each Unity document."""
    matches = list(DOC_RE.finditer(text))
    for i, match in enumerate(matches):
        start = match.end()
        end = matches[i + 1].start() if i + 1 < len(matches) else len(text)
        chunk = text[start:end].lstrip("\r\n")
        head = chunk.split("\n", 1)[0].strip()
        class_name = head[:-1] if head.endswith(":") else head.split(":", 1)[0]
        yield int(match.group(1)), match.group(2), class_name, chunk


def parse_body(body_text: str, yaml, loader) -> dict | None:
    """Parse one document body, which is `<Type>: <mapping>`."""
    if not body_text.strip():
        return None
    try:
        doc = yaml.load(TAG_PREAMBLE + body_text, Loader=loader)
    except Exception:
        return None
    if isinstance(doc, dict) and len(doc) == 1:
        value = next(iter(doc.values()))
        return value if isinstance(value, dict) else None
    return None


def scalar(value) -> str | None:
    if isinstance(value, bool):
        return "true" if value else "false"
    if isinstance(value, (int, float)):
        return str(value)
    if isinstance(value, str):
        value = value.strip()
        return value or None
    return None


def ref_of(value) -> tuple[str, str] | None:
    """Return (kind, payload) for a Unity reference dict."""
    if not isinstance(value, dict):
        return None
    guid = str(value.get("guid") or "").strip().lower()
    file_id = str(value.get("fileID") or "0")
    if guid:
        return "guid", guid
    if file_id not in ("0", ""):
        return "local", file_id
    return None


def describe_ref(value, guids: dict[str, str]) -> str | None:
    ref = ref_of(value)
    if not ref:
        return None
    kind, payload = ref
    if kind == "guid":
        return "@" + guids.get(payload, f"guid:{payload[:8]}")
    return "#" + payload


def collect_refs(body: dict, guids: dict[str, str]) -> tuple[dict, list[dict]]:
    """Split a component body into displayable fields and external references."""
    fields: dict[str, str] = {}
    refs: list[dict] = []
    for key, raw in body.items():
        if key in SKIP_FIELDS:
            continue
        described = describe_ref(raw, guids)
        if described:
            fields[key] = described
            if described.startswith("@"):
                refs.append({"field": key, "path": described[1:]})
            continue
        if isinstance(raw, list):
            if raw and all(isinstance(item, str) for item in raw):
                joined = trim(", ".join(raw))
                if joined:
                    fields[key] = joined
            elif raw:
                fields[key] = f"[{len(raw)} items]"
            continue
        text = scalar(raw)
        if text:
            fields[key] = trim(text)
    return fields, refs


def parse_file(rel: str, path: Path, yaml, loader, guids: dict[str, str]) -> dict:
    text = path.read_text(encoding="utf-8", errors="ignore")
    docs: list[tuple[int, str, str, dict]] = []
    for class_id, file_id, class_name, body_text in split_documents(text):
        body = parse_body(body_text, yaml, loader)
        if body is None:
            continue
        docs.append((class_id, file_id, class_name, body))

    # Prefab instances in scenes: the real GameObject lives in another file, so
    # the scene only stores a stripped stub plus the overrides. Recover the name
    # and the parent from the PrefabInstance record.
    instances: dict[str, dict] = {}
    for _cid, file_id, class_name, body in docs:
        if class_name != "PrefabInstance":
            continue
        modification = body.get("m_Modification") or {}
        source_ref = ref_of(body.get("m_SourcePrefab"))
        name = None
        for entry in modification.get("m_Modifications") or []:
            if isinstance(entry, dict) and entry.get("propertyPath") == "m_Name":
                value = str(entry.get("value") or "").strip()
                if value:
                    name = value
        parent_ref = ref_of(modification.get("m_TransformParent"))
        instances[file_id] = {
            "source": guids.get(source_ref[1]) if source_ref and source_ref[0] == "guid" else None,
            "name": name,
            "parent": parent_ref[1] if parent_ref and parent_ref[0] == "local" else None,
        }

    instance_of: dict[str, str] = {}
    for _cid, file_id, class_name, body in docs:
        if class_name == "PrefabInstance":
            continue
        instance_ref = ref_of(body.get("m_PrefabInstance"))
        if instance_ref and instance_ref[0] == "local" and instance_ref[1] in instances:
            instance_of[file_id] = instance_ref[1]

    go_names: dict[str, str] = {}
    go_transform: dict[str, str] = {}
    transform_go: dict[str, str] = {}
    transform_father: dict[str, str] = {}
    transform_children: dict[str, list[str]] = defaultdict(list)

    instance_go: dict[str, str] = {}
    instance_transform: dict[str, str] = {}

    for _cid, file_id, class_name, body in docs:
        if class_name == "GameObject":
            name = str(body.get("m_Name") or "").strip()
            if not name:
                instance = instances.get(instance_of.get(file_id, ""), {})
                name = instance.get("name") or ""
                if not name and instance.get("source"):
                    name = Path(instance["source"]).stem
                name = name or "(unnamed)"
            go_names[file_id] = name
            instance_id = instance_of.get(file_id)
            if instance_id:
                instance_go[instance_id] = file_id
        elif class_name in ("Transform", "RectTransform"):
            instance_id = instance_of.get(file_id)
            if instance_id:
                instance_transform[instance_id] = file_id
            go_ref = ref_of(body.get("m_GameObject"))
            if go_ref and go_ref[0] == "local":
                transform_go[file_id] = go_ref[1]
                go_transform[go_ref[1]] = file_id
            father = ref_of(body.get("m_Father"))
            if father and father[0] == "local":
                transform_father[file_id] = father[1]
                transform_children[father[1]].append(file_id)

    # Link each prefab instance's stripped root transform into the scene tree.
    for instance_id, instance in instances.items():
        root_transform = instance_transform.get(instance_id)
        root_go = instance_go.get(instance_id)
        if not root_transform or not root_go:
            continue
        transform_go[root_transform] = root_go
        go_transform[root_go] = root_transform
        parent = instance.get("parent")
        if parent:
            transform_father[root_transform] = parent
            transform_children[parent].append(root_transform)

    def go_path(go_id: str) -> str:
        names: list[str] = []
        seen: set[str] = set()
        transform = go_transform.get(go_id)
        while transform and transform not in seen:
            seen.add(transform)
            owner = transform_go.get(transform)
            if owner and owner in go_names:
                names.append(go_names[owner])
            transform = transform_father.get(transform)
        return "/".join(reversed(names)) or go_names.get(go_id, "")

    objects: dict[str, dict] = {}
    for _cid, file_id, class_name, body in docs:
        key = f"{rel}#{file_id}"
        record: dict = {"class": class_name}

        if class_name == "GameObject":
            record["name"] = go_names.get(file_id, "")
            instance = instances.get(instance_of.get(file_id, ""))
            if instance and instance.get("source"):
                record["prefab"] = instance["source"]
            parent_transform = transform_father.get(go_transform.get(file_id, ""), "")
            if parent_transform:
                parent_go = transform_go.get(parent_transform)
                if parent_go:
                    record["parent"] = f"{rel}#{parent_go}"
            children = transform_children.get(go_transform.get(file_id, ""), [])
            child_gos = [f"{rel}#{transform_go[t]}" for t in children if t in transform_go]
            if child_gos:
                record["children"] = child_gos
            objects[key] = record
            continue

        if class_name == "PrefabInstance":
            instance = instances.get(file_id) or {}
            if instance.get("source"):
                record["prefab"] = instance["source"]
                record["refs"] = [{"field": "m_SourcePrefab", "path": instance["source"]}]
            if instance.get("name"):
                record["name"] = instance["name"]
            if instance.get("parent"):
                parent_go = transform_go.get(instance["parent"])
                if parent_go:
                    record["go"] = f"{rel}#{parent_go}"
            objects[key] = record
            continue

        go_ref = ref_of(body.get("m_GameObject"))
        if go_ref and go_ref[0] == "local":
            record["go"] = f"{rel}#{go_ref[1]}"

        script = body.get("m_Script")
        script_ref = ref_of(script) if isinstance(script, dict) else None
        if script_ref and script_ref[0] == "guid":
            script_path = guids.get(script_ref[1])
            if script_path:
                record["script"] = script_path

        if class_name not in SKIP_FIELD_CLASSES:
            fields, refs = collect_refs(body, guids)
            if fields:
                record["fields"] = fields
            if refs:
                record["refs"] = refs

        objects[key] = record

    for key, record in objects.items():
        if record.get("class") == "GameObject":
            record["path"] = go_path(key.split("#", 1)[1])

    # Local references are bare fileIDs; resolve them to the target object name
    # so a field reads `checkoutPoint = #169416658 (Game Controller/Check Out Point)`.
    for key, record in objects.items():
        fields = record.get("fields")
        if not fields:
            continue
        rel_prefix = key.split("#", 1)[0]
        for name, value in list(fields.items()):
            if not isinstance(value, str) or not value.startswith("#"):
                continue
            target = objects.get(f"{rel_prefix}#{value[1:].split(' ')[0]}")
            if not target:
                continue
            owner = objects.get(target.get("go") or "", {})
            label = owner.get("path") or target.get("path") or target.get("name")
            if not label and target.get("script"):
                label = Path(target["script"]).stem
            label = label or target.get("class")
            if label:
                fields[name] = f"{value} ({label})"

    return {
        "objects": objects,
        "counts": Counter(record["class"] for record in objects.values()),
    }


def build_index(report: bool = True) -> dict:
    yaml, loader = load_yaml()
    guids = guid_index()

    files: dict[str, dict] = {}
    objects: dict[str, dict] = {}
    parsed = skipped = 0

    for path, rel in iter_unity_files():
        try:
            size = path.stat().st_size
        except OSError:
            continue
        if size > MAX_BYTES:
            skipped += 1
            continue
        try:
            result = parse_file(rel, path, yaml, loader, guids)
        except Exception:
            skipped += 1
            continue
        if not result["objects"]:
            skipped += 1
            continue
        parsed += 1
        objects.update(result["objects"])
        files[rel] = {
            "objects": len(result["objects"]),
            "bytes": size,
            "classes": dict(result["counts"]),
        }

    classes = Counter(record["class"] for record in objects.values())
    refs = sum(len(record.get("refs") or []) for record in objects.values())
    index = {
        "generated": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        "root": ROOT.as_posix(),
        "counts": {
            "files": parsed,
            "skipped_files": skipped,
            "objects": len(objects),
            "gameobjects": classes.get("GameObject", 0),
            "monobehaviours": classes.get("MonoBehaviour", 0),
            "references": refs,
            "guids": len(guids),
        },
        "files": files,
        "guids": guids,
        "objects": objects,
    }
    if report:
        print(
            f"[unity-index] {parsed} file(s), {skipped} skipped | "
            f"{len(objects)} object(s), {classes.get('GameObject', 0)} GameObject, "
            f"{classes.get('MonoBehaviour', 0)} MonoBehaviour, {refs} reference(s)"
        )
    return index


def main() -> int:
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument("--dry-run", action="store_true", help="counts only, write nothing")
    args = parser.parse_args()

    index = build_index(report=True)
    if args.dry_run:
        return 0

    INDEX_DIR.mkdir(parents=True, exist_ok=True)
    INDEX.write_text(json.dumps(index, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")
    print(f"[unity-index] wrote {INDEX} ({INDEX.stat().st_size / 1024:.0f} KB)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
