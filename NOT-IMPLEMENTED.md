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

---

This document is referenced from "not implemented" messages throughout CodeBrix.Platform.
