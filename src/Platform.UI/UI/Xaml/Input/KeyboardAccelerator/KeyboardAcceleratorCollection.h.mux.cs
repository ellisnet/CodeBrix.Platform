// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\core\inc\CKeyboardAcceleratorCollection.h, 

using CodeBrix.Platform.UI.Xaml;
using CodeBrix.Platform.UI.Xaml.Core;
using CodeBrix.Platform.UI.Extensions;

namespace Microsoft.UI.Xaml.Input;

internal class KeyboardAcceleratorCollection : DependencyObjectCollection<KeyboardAccelerator>
{
#if HAS_CODEBRIX // TODO: Uno specific - workaround for the lack of support for Enter/Leave on DOs.
	private ParentVisualTreeListener _parentVisualTreeListener;

	public KeyboardAcceleratorCollection(DependencyObject parent) : base(parent, true)
	{
		_parentVisualTreeListener = new ParentVisualTreeListener(this);
		_parentVisualTreeListener.ParentLoaded += (s, e) => Enter(null, new EnterParams(true));
		_parentVisualTreeListener.ParentUnloaded += (s, e) => Leave(null, new LeaveParams(true));

		// The listener reports a parent that is ALREADY loaded from its own constructor, before the handlers above
		// exist: a collection created for a live element (KeyboardAccelerators first read from code once the element
		// is loaded) would otherwise never enter the content root's live accelerators, and its accelerators would
		// never be invoked. WinUI enters a collection that is set on a live element at once.
		if (this.FindFirstParent<FrameworkElement>() is { IsLoaded: true })
		{
			Enter(null, new EnterParams(true));
		}
	}
#endif

	private void Enter(DependencyObject pNamescopeOwner, EnterParams enterParams)
	{
		//base.Enter(pNamescopeOwner, enterParams);

		if (enterParams.IsLive)// || enterParams.IsForKeyboardAccelerator)
		{
			ContentRoot pContentRoot = VisualTree.GetContentRootForElement(this);
			if (pContentRoot != null)
			{
				pContentRoot.AddToLiveKeyboardAccelerators(this);
			}
		}
	}

	private void Leave(DependencyObject pNamescopeOwner, LeaveParams leaveParams)
	{
		//base.Leave(pNamescopeOwner, leaveParams);

		if (leaveParams.IsLive)// || leaveParams.IsForKeyboardAccelerator)
		{
			ContentRoot pContentRoot = VisualTree.GetContentRootForElement(this);
			if (pContentRoot != null)
			{
				pContentRoot.RemoveFromLiveKeyboardAccelerators(this);
			}
		}
	}
}
