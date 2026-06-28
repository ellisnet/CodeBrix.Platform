#nullable enable
using System;
using System.Linq;
using CodeBrix.Platform.Extensions;

namespace CodeBrix.Platform.UI.SourceGenerators.XamlGenerator; //Was previously: Uno.UI.SourceGenerators.XamlGenerator

/// <summary>
/// The Initialize method of an event handler databind using x:Bind
/// </summary>
/// <param name="MethodName">Name of the initialize method</param>
/// <param name="Build">Body of the method</param>
internal record XBindEventInitializerDefinition(string MethodName, Action<string, IIndentedStringBuilder> Build);
