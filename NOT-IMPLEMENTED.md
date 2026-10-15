# Not Implemented

You most likely arrived here from a `NotImplementedException` (or a build warning)
telling you that a specific type or member **is not implemented** in CodeBrix.Platform.

## What this means

CodeBrix.Platform provides a large, WinUI/UWP-compatible API surface so that the same
application code can run across the supported desktop targets. To keep that surface
complete and source-compatible, **every** WinUI/UWP type and member exists in the API —
but a subset of them are not yet backed by a working implementation. When your code
reaches one of those, you get a "not implemented" message that points at this page.

The exception/warning message names the exact member, for example:

> The member `string SomeType.SomeMember` is not implemented.

## What to do about it

- **Check the message for the member name.** That tells you precisely which API is missing
  an implementation — not that something is broken in your own code.
- **Look for an alternative API** that is implemented and accomplishes the same thing.
- **Guard the call at runtime** if the member is only needed on some platforms or in some
  scenarios.
- **Open an issue** on the CodeBrix.Platform repository if an unimplemented member is
  blocking you, so it can be prioritized:
  <https://github.com/ellisnet/CodeBrix.Platform/issues>

## Why an API can be present but not implemented

CodeBrix.Platform deliberately keeps the full WinUI/UWP shape (types, properties, methods,
events) so that code, XAML, and third-party libraries compile unchanged. Members that have
no meaningful behaviour on the supported targets — or that simply have not been ported yet —
throw a "not implemented" exception rather than silently doing the wrong thing.

## Implemented, but not yet verified on every platform

Some features are implemented for every head but have so far only been exercised on some of them.
They are expected to work; if one does not on your platform, please open an issue.

- **WebGpu add-in (`WebGpuRenderView`), DirectMetal render path (macOS).** The macOS head's direct path
  compiles and is guarded at run time, but has not yet been run on a Mac. Two points in particular are
  unverified: Metal's hazard tracking between Skia's queue sampling a frame texture and WebGPU's own queue
  writing it, and releasing the frame textures off the main thread when the `Manual` driver tears the view
  down on a worker thread. With `UseDirectGpuRenderingMode()` off (the default) the view uses its
  `Readback` path, which does not depend on either.
- **WebGpu add-in, DirectVulkan render path on the Wayland head.** The same Vulkan code as the X11 head's
  direct path (which has run live), including the wait for the Wayland renderer's first frame, but not
  yet run in a live Wayland session.
- **WebGpu add-in on Windows (Win32 and WPF heads).** The `Readback` path on the view's own Direct3D 12
  device; there is no direct path on these heads.
- **WebGpu PlayTest provider (`CodeBrix.Platform.PlayTest.WebGpu`) on Windows (Direct3D 12) and macOS
  (Metal).** Only the Linux (Vulkan) route has run.

---

This document is referenced from "not implemented" messages throughout CodeBrix.Platform.
