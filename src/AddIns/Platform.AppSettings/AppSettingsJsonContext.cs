//
// AppSettingsJsonContext.cs
//
// Copyright (c) 2026 Jeremy Ellis and contributors
// SPDX-License-Identifier: Apache-2.0
//

using System.Text.Json.Serialization;

namespace CodeBrix.Platform.AppSettings;

/// <summary>
/// Source-generated System.Text.Json metadata for the values the settings store itself reads and writes (the
/// auto-backup retention count), so the store never needs reflection-based serialization of its own - a trimmed or
/// native AOT application that uses only the JsonTypeInfo&lt;T&gt; members serializes nothing by reflection.
/// </summary>
[JsonSerializable(typeof(int))]
internal sealed partial class AppSettingsJsonContext : JsonSerializerContext
{
}
