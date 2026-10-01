#!/usr/bin/env python3
"""Exercise the automatic recorder, including intentionally failing test code.

Build build/test-scripts/PlayTestRecordingProbe/PlayTestRecordingProbe.csproj in
Release first. This checker succeeds only when the intentional failure is correctly
reported and every expected PNG/index entry is valid. No desktop capture is used.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import struct
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
DLL = ROOT / "build/test-scripts/PlayTestRecordingProbe/bin/Release/net10.0/PlayTestRecordingProbe.dll"


def run(arguments, *, expected, environment=None):
    result = subprocess.run(["dotnet", "exec", str(DLL), *arguments], cwd=ROOT,
                            env=environment, text=True, stdout=subprocess.PIPE,
                            stderr=subprocess.STDOUT, timeout=120)
    if result.returncode != expected:
        raise AssertionError(f"Exit {result.returncode}, expected {expected}:\n{result.stdout}")
    return result.stdout


def check_index(folder):
    index = json.loads((folder / "screenshot-index.json").read_text())
    assert index["schemaVersion"] == 1 and index["status"] == "completed"
    assert index["application"]["systemTheme"] == "Dark"
    assert index["application"]["preferredOrientation"] == "Portrait"
    tests = index["tests"]
    # Statically skipped methods have no executing test body/lifecycle callback.
    assert len(tests) == 3, [(t["method"], t["outcome"]) for t in tests]
    assert sorted(t["outcome"] for t in tests) == ["failed", "passed", "passed"]
    rows = [t for t in tests if t["caseNumber"] is not None]
    assert sorted(t["caseNumber"] for t in rows) == [1, 2]
    assert {t["arguments"][0]["value"] for t in rows} == {"first row", "second row"}
    indexed = set()
    for test in tests:
        assert test["directory"].startswith("SendButton/Clicking/ClickTests/")
        assert test["startedAt"] <= test["bodyStartedAt"] <= test["finishedAt"]
        assert test["durationMilliseconds"] > 0 and not test["recordingErrors"]
        shots = test["screenshots"]
        assert shots[0]["kind"] == "start" and shots[-1]["kind"] == "final"
        assert Path(shots[0]["path"]).name == "screenshot-start.png"
        assert Path(shots[-1]["path"]).name == "screenshot-final.png"
        for ordinal, shot in enumerate(shots[1:-1], 1):
            assert shot["step"] == ordinal
            assert Path(shot["path"]).name == f"screenshot-{ordinal}.png"
        for shot in shots:
            path = folder / shot["path"]
            assert path.resolve().is_relative_to(folder.resolve())
            data = path.read_bytes()
            assert data[:8] == b"\x89PNG\r\n\x1a\n"
            assert struct.unpack(">II", data[16:24]) == (shot["width"], shot["height"])
            assert shot["path"] not in indexed
            indexed.add(shot["path"])
            if shot["kind"] == "step":
                source = shot["source"]
                assert source and source["line"] > 0
                assert Path(source["file"]).name == "RecordingTests.cs"
        first = (folder / shots[0]["path"]).read_bytes()
        checked = (folder / shots[1]["path"]).read_bytes()
        assert hashlib.sha256(first).digest() != hashlib.sha256(checked).digest()
        assert (shots[0]["width"], shots[0]["height"]) == (1080, 1920)
        if test["caseNumber"] is not None:
            assert test["directory"].endswith(f"test-case-{test['caseNumber']}")
            assert (shots[-1]["width"], shots[-1]["height"]) == (1920, 1080)
            assert any(s["operation"] == "Evaluate" and s["rootRequestedTheme"] == "Light" for s in shots)
        else:
            assert "Intentional recording-harness failure" in test["errors"]
    assert {p.relative_to(folder).as_posix() for p in folder.rglob("*.png")} == indexed
    return len(indexed)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, help="New parent directory for validation artifacts")
    args = parser.parse_args()
    output = args.output.resolve() if args.output else Path(tempfile.mkdtemp(prefix="playtest-recording-check-"))
    if args.output:
        output.mkdir(parents=True, exist_ok=False)
    recorded = output / "recorded run with spaces"
    recorded.mkdir()
    environment = os.environ.copy()
    environment.update(CODEBRIX_PLAYTEST_THEME="light", CODEBRIX_PLAYTEST_ORIENTATION="landscape")
    log = run(["--headless", "--theme=DaRk", "--orientation=PoRtRaIt", f"--screenshotfolder={recorded}"], expected=2, environment=environment)
    (output / "test.log").write_text(log)
    assert "Intentional recording-harness failure" in log and "skipped: 1" in log
    count = check_index(recorded)

    empty = output / "empty"
    empty.mkdir()
    missing = output / "does-not-exist"
    occupied = output / "occupied"
    occupied.mkdir()
    marker = occupied / ".hidden"
    marker.write_text("preserve user content")
    child = output / "contains-directory"
    (child / "empty-child").mkdir(parents=True)
    checks = [
        (["--theme=system"], "--theme must be"),
        (["--theme=1"], "--theme must be"),
        (["--orientation=sideways"], "--orientation must be"),
        (["--orientation=0"], "--orientation must be"),
        ([f"--screenshotfolder={missing}"], "existing folder"),
        ([f"--screenshotfolder={occupied}"], "must be empty"),
        ([f"--screenshotfolder={child}"], "must be empty"),
        ([f"--screenshotfolder={marker}"], "existing folder"),
        ([f"--screenshotfolder={recorded}"], "must be empty"),
    ]
    for arguments, diagnostic in checks:
        assert diagnostic in run(arguments, expected=5)
    assert marker.read_text() == "preserve user content" and not missing.exists()
    run(["--list-tests", f"--screenshotfolder={empty}"], expected=0)
    assert not list(empty.iterdir()), "Discovery must not claim a screenshot directory"
    # A disabled recorder must create no index in the working directory.
    run(["--headless", "--filter-method", "*Clicking_changes_pixels"], expected=0)
    summary = {"screenshots": count, "executedCases": 3, "expectedFailures": 1,
               "validationChecks": len(checks), "output": str(output)}
    (output / "summary.json").write_text(json.dumps(summary, indent=2) + "\n")
    print(json.dumps(summary, indent=2))


if __name__ == "__main__":
    main()
