param(
    [Parameter(Mandatory = $true)][string]$TestOutput,
    [string]$Artifacts = 'TestResults/PlayTestPreview-Windows'
)

# Windows counterpart of playtest-preview-orientation.py. Captures only this
# script's own preview client area; never sends keyboard or pointer input.
$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'Run this check on Windows with PowerShell 7.' }
$TestOutput = (Resolve-Path -LiteralPath $TestOutput).Path
$null = New-Item -ItemType Directory -Path $Artifacts -Force
$Artifacts = (Resolve-Path -LiteralPath $Artifacts).Path
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class PreviewWindow {
    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct Point { public int X, Y; }
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr window, ref Point point);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    [DllImport("dwmapi.dll")] public static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
    public static byte[] Pixels(int width, int height, byte red, byte green, byte blue) {
        var bytes = new byte[width * height * 4];
        for (int i = 0; i < bytes.Length; i += 4) {
            bytes[i] = blue; bytes[i+1] = green; bytes[i+2] = red; bytes[i+3] = 255;
        }
        return bytes;
    }
    public static int[] ContentBounds(byte[] pixels, int width, int height, int stride) {
        int left = width, top = height, right = 0, bottom = 0;
        // DWM's thin active-window outline can overlap the captured client edge.
        // Ignore those two pixels, then extend content that reaches the inner edge.
        for (int y = 2; y < height-2; y++) for (int x = 2; x < width-2; x++) {
            int i = y * stride + x * 4;
            if (pixels[i] <= 2 && pixels[i+1] <= 2 && pixels[i+2] <= 2) continue;
            left = Math.Min(left, x); top = Math.Min(top, y);
            right = Math.Max(right, x+1); bottom = Math.Max(bottom, y+1);
        }
        if (left == 2) left = 0;
        if (top == 2) top = 0;
        if (right == width-2) right = width;
        if (bottom == height-2) bottom = height;
        return new[] { left, top, right, bottom };
    }
}
'@
$previousDpi = [PreviewWindow]::SetThreadDpiAwarenessContext([IntPtr]::new(-4))

function Start-Preview([int]$Width, [int]$Height, [string]$Driver = 'windows') {
    $info = [System.Diagnostics.ProcessStartInfo]::new('dotnet')
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardInput = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $info.Environment['SDL_VIDEODRIVER'] = $Driver
    $null = $info.Environment.Remove('SDL_RENDER_DRIVER')
    foreach ($arg in @('exec', '--depsfile', "$TestOutput/JustBetweenUs.PlayTests.deps.json",
        '--runtimeconfig', "$TestOutput/JustBetweenUs.PlayTests.runtimeconfig.json",
        "$TestOutput/CodeBrix.Platform.UI.Runtime.Skia.PlayTest.dll", '--preview', "$Width", "$Height")) {
        $info.ArgumentList.Add($arg)
    }
    $process = [System.Diagnostics.Process]::Start($info)
    try {
        $ready = $process.StandardOutput.ReadLineAsync()
        if (-not $ready.Wait(20000)) { throw 'Preview readiness timed out.' }
        if ($ready.Result -ne 'READY') { throw "Preview failed: $($process.StandardError.ReadToEnd())" }
        return $process
    } catch {
        if (-not $process.HasExited) { $process.Kill($true); $process.WaitForExit() }
        $process.Dispose()
        throw
    }
}

function Get-PreviewFrame([IntPtr]$Window) {
    $rect = [PreviewWindow+Rect]::new()
    $origin = [PreviewWindow+Point]::new()
    if (-not [PreviewWindow]::GetClientRect($Window, [ref]$rect) -or
        -not [PreviewWindow]::ClientToScreen($Window, [ref]$origin)) { throw 'Cannot locate preview client area.' }
    $bitmap = [System.Drawing.Bitmap]::new($rect.Right, $rect.Bottom)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try { $graphics.CopyFromScreen($origin.X, $origin.Y, 0, 0, $bitmap.Size) }
    finally { $graphics.Dispose() }
    return $bitmap
}

try {
    foreach ($preferred in @(@(1920, 1080), @(1080, 1920))) {
        $process = Start-Preview $preferred[0] $preferred[1]
        try {
            $deadline = [DateTime]::UtcNow.AddSeconds(15)
            do {
                $process.Refresh()
                if ($process.MainWindowHandle -ne [IntPtr]::Zero) { break }
                if ([DateTime]::UtcNow -gt $deadline) { throw 'No preview window appeared.' }
                Start-Sleep -Milliseconds 50
            } while ($true)
            $window = $process.MainWindowHandle
            # Rounded Windows 11 corners can expose desktop pixels inside the client
            # rectangle. Square only our probe window for the pixel-boundary check.
            $cornerPreference = 1
            $null = [PreviewWindow]::DwmSetWindowAttribute($window, 33, [ref]$cornerPreference, 4)
            $null = [PreviewWindow]::SetForegroundWindow($window)
            $initial = Get-PreviewFrame $window
            $initialSize = $initial.Size
            $initial.Dispose()
            $index = 0
            foreach ($frame in @(@(1920, 1080), @(1080, 1920), @(1920, 1080), @(1080, 1920))) {
                $red = 40 + $index * 30
                $pixels = [PreviewWindow]::Pixels($frame[0], $frame[1], $red, 120, 200)
                $stream = $process.StandardInput.BaseStream
                $header = [BitConverter]::GetBytes([int]$frame[0]) + [BitConverter]::GetBytes([int]$frame[1])
                $stream.Write($header, 0, $header.Length)
                $stream.Write($pixels, 0, $pixels.Length)
                $stream.Flush()
                $deadline = [DateTime]::UtcNow.AddSeconds(15)
                do {
                    if ($process.HasExited) { throw 'Preview exited before presenting the frame.' }
                    $shot = Get-PreviewFrame $window
                    $center = $shot.GetPixel([int]($shot.Width / 2), [int]($shot.Height / 2))
                    if ([Math]::Abs($center.R - $red) -le 2 -and
                        [Math]::Abs($center.G - 120) -le 2 -and [Math]::Abs($center.B - 200) -le 2) { break }
                    $shot.Dispose()
                    if ([DateTime]::UtcNow -gt $deadline) { throw 'Frame was not presented in the native preview.' }
                    Start-Sleep -Milliseconds 50
                } while ($true)
                try {
                    $shot.Save("$Artifacts/preview-$($preferred[0])x$($preferred[1])-frame-$index.png", [System.Drawing.Imaging.ImageFormat]::Png)
                    if ($shot.Size -ne $initialSize) { throw "Preview resized: $initialSize -> $($shot.Size)" }
                    $data = $shot.LockBits([System.Drawing.Rectangle]::new(0, 0, $shot.Width, $shot.Height),
                        [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
                    try {
                        $bytes = [byte[]]::new($data.Stride * $shot.Height)
                        [Runtime.InteropServices.Marshal]::Copy($data.Scan0, $bytes, 0, $bytes.Length)
                        $bounds = [PreviewWindow]::ContentBounds($bytes, $shot.Width, $shot.Height, $data.Stride)
                    } finally { $shot.UnlockBits($data) }
                    $scale = [Math]::Min($shot.Width / $frame[0], $shot.Height / $frame[1])
                    $width = $frame[0] * $scale
                    $height = $frame[1] * $scale
                    if ([Math]::Abs(($bounds[2] - $bounds[0]) - $width) -gt 2 -or
                        [Math]::Abs(($bounds[3] - $bounds[1]) - $height) -gt 2 -or
                        [Math]::Abs($bounds[0] - ($shot.Width - $width) / 2) -gt 2 -or
                        [Math]::Abs($bounds[1] - ($shot.Height - $height) / 2) -gt 2) {
                        throw "Incorrect letterboxing: $bounds"
                    }
                    Write-Output "PASS preferred=$($preferred -join 'x') frame=$($frame -join 'x') window=$($shot.Size) content=$bounds"
                } finally { $shot.Dispose() }
                $index++
            }
            $process.StandardInput.Close()
            if (-not $process.WaitForExit(15000) -or $process.ExitCode -ne 0) { throw 'Preview did not exit cleanly.' }
        } finally {
            if (-not $process.HasExited) { $process.Kill($true); $process.WaitForExit() }
            $process.Dispose()
        }
    }
    foreach ($case in @(
        @{ Name = 'clean EOF'; Bytes = [byte[]]@(); Code = 0; Error = '' },
        @{ Name = 'invalid dimensions'; Bytes = [BitConverter]::GetBytes(100) + [BitConverter]::GetBytes(100); Code = 1; Error = 'InvalidDataException' },
        @{ Name = 'truncated header'; Bytes = [byte[]]@(128); Code = 1; Error = 'EndOfStreamException' },
        @{ Name = 'truncated pixels'; Bytes = [BitConverter]::GetBytes(1920) + [BitConverter]::GetBytes(1080) + [byte[]]@(0); Code = 1; Error = 'EndOfStreamException' }
    )) {
        $process = Start-Preview 1920 1080 'dummy'
        try {
            $process.StandardInput.BaseStream.Write([byte[]]$case.Bytes, 0, $case.Bytes.Length)
            $process.StandardInput.Close()
            if (-not $process.WaitForExit(15000)) { throw "$($case.Name): preview did not exit." }
            $errors = $process.StandardError.ReadToEnd()
            if ($process.ExitCode -ne $case.Code -or ($case.Error -and -not $errors.Contains($case.Error))) {
                throw "$($case.Name): unexpected exit $($process.ExitCode): $errors"
            }
            Write-Output "PASS protocol: $($case.Name)"
        } finally {
            if (-not $process.HasExited) { $process.Kill($true); $process.WaitForExit() }
            $process.Dispose()
        }
    }
} finally {
    $null = [PreviewWindow]::SetThreadDpiAwarenessContext($previousDpi)
}
