#!/usr/bin/env python3
"""Check the native Cocoa preview's frames, stable size, letterboxing, and pipe errors.

Requires macOS 14+, Apple's command-line tools and Screen Recording permission
for the invoking terminal/agent. Captures only the previews started by this script.
No Python imaging packages are needed. All evidence is saved under --artifacts.
"""
import argparse
import json
import os
from pathlib import Path
import selectors
import struct
import subprocess
import sys
import time


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--test-output", type=Path, required=True)
    parser.add_argument("--test-name", default="PlayTestDemo.PlayTests")
    parser.add_argument("--artifacts", type=Path, default=Path("TestResults/PlayTestPreview-macOS"))
    args = parser.parse_args()
    if sys.platform != "darwin":
        parser.error("Run this check on macOS.")
    output, artifacts = args.test_output.resolve(), args.artifacts.resolve()
    artifacts.mkdir(parents=True, exist_ok=True)
    helper = artifacts / "capture-preview"
    subprocess.run(["xcrun", "clang", "-fobjc-arc", "-mmacosx-version-min=14.0",
                    "-framework", "AppKit", "-framework", "ScreenCaptureKit",
                    str(Path(__file__).with_name("playtest-preview-capture-macos.m")),
                    "-o", str(helper)], check=True)

    def command(width, height):
        return ["dotnet", "exec", "--depsfile", str(output / (args.test_name + ".deps.json")),
                "--runtimeconfig", str(output / (args.test_name + ".runtimeconfig.json")),
                str(output / "CodeBrix.Platform.UI.Runtime.Skia.PlayTest.dll"), "--preview", str(width), str(height)]

    results = []
    for width, height in [(1920, 1080), (1080, 1920)]:
        with (artifacts / f"preview-{width}.stderr").open("wb") as errors:
            process = subprocess.Popen(command(width, height), stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                                       stderr=errors, env={**os.environ, "SDL_VIDEODRIVER": "cocoa"})
            try:
                with selectors.DefaultSelector() as selector:
                    selector.register(process.stdout, selectors.EVENT_READ)
                    assert selector.select(20), "Preview did not report readiness"
                assert process.stdout.readline() == b"READY\n", "Preview failed to start"
                initial_size = None
                for index, (fw, fh) in enumerate([(1920, 1080), (1080, 1920)] * 2):
                    colour = (40 + index * 30, 120, 200)
                    pixels = bytes((colour[2], colour[1], colour[0], 255)) * (fw * fh)
                    process.stdin.write(struct.pack("<ii", fw, fh) + pixels)
                    process.stdin.flush()
                    image = artifacts / f"preview-{width}x{height}-frame-{index}-{fw}x{fh}.png"
                    deadline = time.monotonic() + 20
                    while True:
                        assert process.poll() is None, "Preview exited unexpectedly"
                        shot = subprocess.run([str(helper), str(process.pid), str(width//2), str(height//2), str(image)],
                                              capture_output=True, text=True, timeout=20)
                        if shot.returncode:
                            raise RuntimeError(shot.stderr.strip())
                        sample = json.loads(shot.stdout)
                        if max(abs(a-b) for a, b in zip(sample["center"], colour)) <= 4:
                            break
                        assert time.monotonic() < deadline, f"Frame was not presented: {sample}"
                        time.sleep(.1)
                    initial_size = initial_size or sample["size"]
                    assert sample["size"] == initial_size, "Preview resized itself"
                    scale = min(width/2/fw, height/2/fh)
                    expected = ((width/2-fw*scale)/2, (height/2-fh*scale)/2, fw*scale, fh*scale)
                    left, top, right, bottom = sample["bounds"]
                    actual = (left, top, right-left, bottom-top)
                    assert all(abs(a-b) <= 2 for a, b in zip(actual, expected)), (actual, expected)
                    results.append(dict(preferred=[width,height], frame=[fw,fh], image=str(image), **sample))
                    print(f"PASS preferred={width}x{height} frame={fw}x{fh} content={sample['bounds']}", flush=True)
                process.stdin.close()
                assert process.wait(timeout=15) == 0
            finally:
                if process.poll() is None:
                    process.kill(); process.wait(timeout=5)
    for name, data, code, error in [
        ("clean EOF", b"", 0, None),
        ("invalid dimensions", struct.pack("<ii",100,100), 1, b"InvalidDataException"),
        ("truncated header", b"\x80", 1, b"EndOfStreamException"),
        ("truncated pixels", struct.pack("<ii",1920,1080)+b"\x00", 1, b"EndOfStreamException"),
    ]:
        run = subprocess.run(command(1920,1080), input=data, capture_output=True, timeout=20,
                             env={**os.environ,"SDL_VIDEODRIVER":"dummy"})
        assert b"READY\n" in run.stdout and run.returncode == code, (name,run.stderr)
        assert error is None or error in run.stderr, (name,run.stderr)
        results.append(dict(protocol=name, passed=True))
        print(f"PASS protocol: {name}", flush=True)
    (artifacts / "summary.json").write_text(json.dumps(results,indent=2))


if __name__ == "__main__":
    main()
