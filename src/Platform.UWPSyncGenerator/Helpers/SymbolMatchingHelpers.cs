using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace CodeBrix.Platform.UWPSyncGenerator.Helpers; //Was previously: Uno.UWPSyncGenerator.Helpers

internal static class SymbolMatchingHelpers
{
	public static bool AreMatching(ISymbol uapSymbol, ISymbol codebrixSymbol)
	{
		//if (uapSymbol?.Name == "SizeInt32" ||
		//	unoSymbol?.Name == "Size" && unoSymbol?.ContainingType?.Name == "AppWindow")
		//{
		//	global::System.Diagnostics.Debugger.Break();
		//}
		if (uapSymbol is IEventSymbol uapEvent)
		{
			var result = codebrixSymbol is IEventSymbol codebrixEvent && AreEventsMatching(uapEvent, codebrixEvent);
			return result;
		}
		else if (uapSymbol is IFieldSymbol uapField)
		{
			var result = codebrixSymbol is IFieldSymbol codebrixField && AreFieldsMatching(uapField, codebrixField);
			return result;
		}
		else if (uapSymbol is INamedTypeSymbol uapNamedType)
		{
			var result = codebrixSymbol is INamedTypeSymbol codebrixNamedType &&
				(
					(AreMatchingCommon(uapNamedType, codebrixNamedType) && uapNamedType.Name == codebrixNamedType.Name) ||
					// This happens for not implemented symbols that are annotated as nullable.
					// Since the compiler can't find the type, it considers it as value type, hence wraps it in Nullable<T>.
					codebrixNamedType.Name == "Nullable"
				);
			return result;
		}
		else if (uapSymbol is IPropertySymbol uapProperty)
		{
			var result = codebrixSymbol is IPropertySymbol codebrixProperty && ArePropertiesMatching(uapProperty, codebrixProperty);
			return result;
		}
		else if (uapSymbol is IMethodSymbol uapMethod)
		{
			var result = codebrixSymbol is IMethodSymbol codebrixMethod && AreMethodsMatching(uapMethod, codebrixMethod);
			return result;
		}
		else if (uapSymbol is ITypeParameterSymbol uapTypeParameter)
		{
			var result = codebrixSymbol is ITypeParameterSymbol codebrixTypeParameter && AreTypeParametersMatching(uapTypeParameter, codebrixTypeParameter);
			return result;
		}
		else if (uapSymbol is IArrayTypeSymbol uapArrayTypeSymbol)
		{
			var result = codebrixSymbol is IArrayTypeSymbol codebrixArrayTypeSymbol && AreMatching(uapArrayTypeSymbol.ElementType, codebrixArrayTypeSymbol.ElementType);
			return result;
		}
		else
		{
			throw new ArgumentException($"Unexpected symbol '{uapSymbol?.Kind.ToString() ?? "<null>"}'");
		}
	}

	private static bool AreMatchingCommon(ISymbol uapSymbol, ISymbol codebrixSymbol)
	{
		if (codebrixSymbol.Kind == SymbolKind.ErrorType)
		{
			// Some events are marked not implemented, but used in implemented signatures.
			// For example, `HoldingEventHandler` and `UIElement.Holding`.
			// When we're matching `UIElement.Holding`, we get here with `HoldingEventHandler`
			// being an error symbol.
			// TODO: Move such symbols to non-generated files and remove this condition.
			return true;
		}

		if (uapSymbol.Name == "DependencyObject" || uapSymbol.ContainingSymbol.Name == "DependencyObject")
		{
			// We define DependencyObject as an interface, diverging from UWP.
			// This causes checks like IsAbstract to diverge for the type itself
			// as well as its members. Ignore them.
			return true;
		}

		if (uapSymbol.Name == "IGeometrySource2D" ||
			uapSymbol.Name == "SizeInt32" ||
			uapSymbol.Name == "PointInt32")
		{
			// For some reason, these are marked with Kind=ErrorType.
			// This means the matching then fails.
			return true;
		}

		if (uapSymbol.Name == "Transform" && uapSymbol.Kind == SymbolKind.NamedType)
		{
			// In Uno, it's abstract to force all derived classes to implement a specific method.
			// In UWP/WinUI, it's not abstract but it doesn't have any accessible constructors.
			// The divergence here shouldn't be problematic/noticeable.
			return true;
		}
		// Skipping accessibility check for now. It causes two issues:
		// 1. For some reason, Roslyn is returning private accessibility for some public properties (Specifically, for some interface implementations).
		// 2. For types declared without explicit accessibility, it's going to be considered internal (however, the generated file later will have the correct accessibility)
		var result = /*uapSymbol.DeclaredAccessibility == unoSymbol.DeclaredAccessibility &&*/
			uapSymbol.IsAbstract == codebrixSymbol.IsAbstract &&
			uapSymbol.IsOverride == codebrixSymbol.IsOverride &&
			uapSymbol.Name == codebrixSymbol.Name &&
			// Temporary skip named type: Until we match seal-ness and static-ness with UWP.
			(uapSymbol.IsSealed == codebrixSymbol.IsSealed || uapSymbol.Kind == SymbolKind.NamedType) &&
			(uapSymbol.IsStatic == codebrixSymbol.IsStatic) &&
			uapSymbol.IsVirtual == codebrixSymbol.IsVirtual;
		return result;
	}

	private static bool AreEventsMatching(IEventSymbol uapEvent, IEventSymbol codebrixEvent)
	{
		if (uapEvent.ContainingType.Name == "Window")
		{
			// TODO: Match API with WinUI.
			return true;
		}

		var result = AreMatchingCommon(uapEvent, codebrixEvent) && AreMatching(uapEvent.Type, codebrixEvent.Type);
		return result;
	}

	private static bool AreFieldsMatching(IFieldSymbol uapField, IFieldSymbol codebrixField) =>
		AreMatchingCommon(uapField, codebrixField) &&
		uapField.IsVolatile == codebrixField.IsVolatile &&
		(uapField.ConstantValue?.Equals(codebrixField.ConstantValue) ?? codebrixField.ConstantValue == null) &&
		uapField.IsConst == codebrixField.IsConst &&
		uapField.IsReadOnly == codebrixField.IsReadOnly &&
		AreMatching(uapField.Type, codebrixField.Type);

	private static bool ArePropertiesMatching(IPropertySymbol uapProperty, IPropertySymbol codebrixProperty)
	{
		if (uapProperty.Name == "WindowActivationState" && uapProperty.ContainingType.Name == "WindowActivatedEventArgs")
		{
			// TODO: Match API with WinUI.
			return true;
		}

		if (uapProperty.Name == "Name" && uapProperty.ContainingType.Name == "FrameworkElement")
		{
			// TODO: Name shouldn't be virtual.
			return true;
		}

		if (!AreMatchingCommon(uapProperty, codebrixProperty))
		{
			return false;
		}
		// TODO:
		//if (uapProperty.IsReadOnly != unoProperty.IsReadOnly)
		//{
		//	return false;
		//}
		//if (uapProperty.IsWriteOnly != unoProperty.IsWriteOnly)
		//{
		//	return false;
		//}
		if (uapProperty.IsIndexer != codebrixProperty.IsIndexer)
		{
			return false;
		}
		if (!AreParametersMatching(uapProperty.Parameters, codebrixProperty.Parameters))
		{
			return false;
		}

		// Property type of ContentTemplateRoot is diverging from UWP.
		// In Uno we use the native view for each platform Android.Views.View, UIKit.UIView, AppKit.NSView
		if (!AreMatching(uapProperty.Type, codebrixProperty.Type) && uapProperty.Name != "ContentTemplateRoot" &&
			// object vs UIElement
			uapProperty.Name != "Content" &&
			// IEasingFunction vs EasingFunctionBase
			uapProperty.Name != "EasingFunction" &&
			// string vs object
			uapProperty.Name != "ElementName")
		{
			return false;
		}

		return true;
	}

	private static bool AreMethodsMatching(IMethodSymbol uapMethod, IMethodSymbol codebrixMethod)
	{
		if (uapMethod == null)
		{
			return codebrixMethod == null;
		}
		else if (codebrixMethod == null)
		{
			return false;
		}

		if (uapMethod.Name == "ToString" && uapMethod.Parameters.IsEmpty)
		{
			return true;
		}

		if (uapMethod.Name is "LoadContent" or "Measure" or "Arrange")
		{
			return true;
		}

		return AreMatchingCommon(uapMethod, codebrixMethod) &&
			(uapMethod.AssociatedSymbol != null || AreParametersMatching(uapMethod.Parameters, codebrixMethod.Parameters)) &&
			uapMethod.Arity == codebrixMethod.Arity &&
			uapMethod.IsExtensionMethod == codebrixMethod.IsExtensionMethod &&
			uapMethod.IsReadOnly == codebrixMethod.IsReadOnly &&
			uapMethod.IsVararg == codebrixMethod.IsVararg &&
			uapMethod.MethodKind == codebrixMethod.MethodKind &&
			AreMatching(uapMethod.ReturnType, codebrixMethod.ReturnType) &&
			uapMethod.TypeArguments == codebrixMethod.TypeArguments;
	}

	private static bool AreTypeParametersMatching(ITypeParameterSymbol uapTypeParameter, ITypeParameterSymbol codebrixTypeParameter)
	{
		return AreMatchingCommon(uapTypeParameter, codebrixTypeParameter) &&
			uapTypeParameter.Ordinal == codebrixTypeParameter.Ordinal &&
			uapTypeParameter.Variance == codebrixTypeParameter.Variance &&
			uapTypeParameter.TypeParameterKind == codebrixTypeParameter.TypeParameterKind &&
			uapTypeParameter.HasReferenceTypeConstraint == codebrixTypeParameter.HasReferenceTypeConstraint &&
			uapTypeParameter.HasValueTypeConstraint == codebrixTypeParameter.HasValueTypeConstraint &&
			uapTypeParameter.HasUnmanagedTypeConstraint == codebrixTypeParameter.HasUnmanagedTypeConstraint &&
			uapTypeParameter.HasNotNullConstraint == codebrixTypeParameter.HasNotNullConstraint &&
			uapTypeParameter.HasConstructorConstraint == codebrixTypeParameter.HasConstructorConstraint;

	}

	private static bool AreParametersMatching(ImmutableArray<IParameterSymbol> uapParameters, ImmutableArray<IParameterSymbol> codebrixParameters)
	{
		if (uapParameters.Length != codebrixParameters.Length)
		{
			return false;
		}

		for (int i = 0; i < uapParameters.Length; i++)
		{
			if (!AreParametersMatching(uapParameters[i], codebrixParameters[i]))
			{
				return false;
			}
		}

		return true;
	}

	private static bool AreParametersMatching(IParameterSymbol uapParameters, IParameterSymbol codebrixParameters) =>
		uapParameters.IsOptional == codebrixParameters.IsOptional &&
		uapParameters.Name == codebrixParameters.Name &&
		uapParameters.IsParams == codebrixParameters.IsParams &&
		uapParameters.RefKind == codebrixParameters.RefKind &&
		AreMatching(uapParameters.Type, codebrixParameters.Type);
}
