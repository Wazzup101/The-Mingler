"""Verify pinned release bytes without extracting or running any downloaded file."""
import argparse
import hashlib
import json
from pathlib import Path

MANIFEST = Path(__file__).with_name("approved-linux-beta1.json")


def verify(directory, manifest):
    directory = Path(directory)
    expected = manifest["assets"]
    if set(p.name for p in directory.iterdir()) != set(expected):
        raise ValueError("Release assets differ from the reviewed manifest")
    for name, record in expected.items():
        path = directory / name
        if Path(name).name != name or path.is_symlink() or not path.is_file():
            raise ValueError("Unexpected asset type or name")
        if path.stat().st_size != record["bytes"]:
            raise ValueError(f"Size mismatch: {name}")
        with path.open("rb") as stream:
            digest = hashlib.file_digest(stream, "sha256").hexdigest()
        if digest != record["sha256"]:
            raise ValueError(f"Checksum mismatch: {name}")
    return {"schemaVersion": 1, "repository": manifest["repository"],
            "releaseTag": manifest["releaseTag"],
            "claim": "Reviewed release assets match the pinned publisher-approved bytes",
            "builtInThisWorkflow": False, "nativeDeviceTestingClaimed": False, "assets": expected}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", type=Path)
    parser.add_argument("--predicate", type=Path)
    args = parser.parse_args()
    result = verify(args.directory, json.loads(MANIFEST.read_text(encoding="utf-8")))
    if args.predicate:
        with args.predicate.open("x", encoding="utf-8", newline="\n") as output:
            output.write(json.dumps(result, indent=2) + "\n")
    print("PASS: all five reviewed release assets match; nothing was executed")
