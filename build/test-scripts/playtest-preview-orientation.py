#!/usr/bin/env python3
"""X11 integration check for the packaged SDL preview: frame dimensions, bars and stable window size.

Requires an X11 desktop, xdotool, ImageMagick import, and Pillow. Pass the built
output directory of a PlayTest test project (PlayTestDemo.PlayTests in this repository by
default; --test-name selects another, such as JustBetweenUs.PlayTests). Opens only its own two preview windows,
sequentially; never injects desktop input. PNG evidence is saved under --artifacts.
"""
import argparse
import io
import os
from pathlib import Path
import selectors
import struct
import subprocess
import time

from PIL import Image


def wait_for(probe, description):
    deadline = time.monotonic() + 15
    while time.monotonic() < deadline:
        result = probe()
        if result:
            return result
        time.sleep(0.1)
    raise AssertionError(f"Timed out waiting for {description}")


def window_id(process):
    result = subprocess.run(["xdotool", "search", "--onlyvisible", "--pid", str(process.pid),
                             "--name", "CodeBrix PlayTest"], capture_output=True, text=True, timeout=5)
    return result.stdout.splitlines()[0] if result.returncode == 0 and result.stdout else None


def capture(window):
    result = subprocess.run(["import", "-window", window, "png:-"], capture_output=True, check=True, timeout=10)
    return Image.open(io.BytesIO(result.stdout)).convert("RGB")


def preview_command(output, preferred, test_name):
    width, height = preferred
    return ["dotnet", "exec", "--depsfile", str(output / (test_name + ".deps.json")),
            "--runtimeconfig", str(output / (test_name + ".runtimeconfig.json")),
            str(output / "CodeBrix.Platform.UI.Runtime.Skia.PlayTest.dll"),
            "--preview", str(width), str(height)]


def run(output, artifacts, preferred, test_name):
    width, height = preferred
    process = subprocess.Popen(preview_command(output, preferred, test_name), stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                               env={**os.environ, "SDL_VIDEODRIVER": "x11"})
    try:
        with selectors.DefaultSelector() as selector:
            selector.register(process.stdout, selectors.EVENT_READ)
            assert selector.select(20), "Preview did not report readiness"
        assert process.stdout.readline() == b"READY\n", "Preview failed to start"
        window = wait_for(lambda: window_id(process), "preview window")
        initial_size = capture(window).size
        for index, (frame_width, frame_height) in enumerate([(1920, 1080), (1080, 1920)] * 2):
            # Different solid colours prove each new frame was presented, including repeat transitions.
            colour = (40 + index * 30, 120, 200)
            red, green, blue = colour
            pixels = bytes((blue, green, red, 255)) * (frame_width * frame_height)
            process.stdin.write(struct.pack("<ii", frame_width, frame_height))
            process.stdin.write(pixels)
            process.stdin.flush()

            def presented():
                assert process.poll() is None, "Preview exited before the check finished"
                shot = capture(window)
                center = shot.getpixel((shot.width // 2, shot.height // 2))
                return shot if max(abs(a - b) for a, b in zip(center, colour)) <= 2 else None

            shot = wait_for(presented, f"frame {frame_width}x{frame_height}")
            assert shot.size == initial_size, f"Preview resized itself: {initial_size} -> {shot.size}"
            bounds = shot.getbbox()
            assert bounds is not None
            scale = min(shot.width / frame_width, shot.height / frame_height)
            expected_width, expected_height = frame_width * scale, frame_height * scale
            left, top, right, bottom = bounds
            assert abs((right - left) - expected_width) <= 2, bounds
            assert abs((bottom - top) - expected_height) <= 2, bounds
            assert abs(left - (shot.width - expected_width) / 2) <= 2, bounds
            assert abs(top - (shot.height - expected_height) / 2) <= 2, bounds
            shot.save(artifacts / f"preview-{width}x{height}-frame-{index}-{frame_width}x{frame_height}.png")
            print(f"PASS preferred={width}x{height} frame={frame_width}x{frame_height} "
                  f"window={shot.size} content={bounds}", flush=True)
        process.stdin.close()
        assert process.wait(timeout=15) == 0, process.stderr.read().decode()
    finally:
        if process.poll() is None:
            process.kill()
            process.wait(timeout=5)


def check_protocol_errors(output, test_name):
    for name, data, exit_code, error in [
        ("clean EOF", b"", 0, None),
        ("invalid dimensions", struct.pack("<ii", 100, 100), 1, b"InvalidDataException"),
        ("truncated header", b"\x80", 1, b"EndOfStreamException"),
        ("truncated pixels", struct.pack("<ii", 1920, 1080) + b"\x00", 1, b"EndOfStreamException"),
    ]:
        result = subprocess.run(preview_command(output, (1920, 1080), test_name), input=data, capture_output=True,
                                timeout=20, env={**os.environ, "SDL_VIDEODRIVER": "dummy"})
        assert b"READY\n" in result.stdout, result.stderr.decode()
        assert result.returncode == exit_code, (name, result.returncode, result.stderr.decode())
        if error:
            assert error in result.stderr, (name, result.stderr.decode())
        print(f"PASS protocol: {name}", flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--test-output", type=Path, required=True)
    parser.add_argument("--test-name", default="PlayTestDemo.PlayTests")
    parser.add_argument("--artifacts", type=Path, default=Path("TestResults/PlayTestPreview"))
    args = parser.parse_args()
    args.artifacts.mkdir(parents=True, exist_ok=True)
    for preferred in [(1920, 1080), (1080, 1920)]:
        run(args.test_output.resolve(), args.artifacts, preferred, args.test_name)
    check_protocol_errors(args.test_output.resolve(), args.test_name)


if __name__ == "__main__":
    main()
