#nullable enable

using System;
using Microsoft.CodeAnalysis;

namespace CodeBrix.Platform.UI.SourceGenerators.XamlGenerator; //Was previously: Uno.UI.SourceGenerators.XamlGenerator

internal record BackingFieldDefinition(string GlobalizedTypeName, string Name, Accessibility Accessibility);
