﻿
/* 
 * PropertyPad.cs: The pad that holds the MD property grid. Can also 
 * hold custom grid widgets.
 * 
 * Author:
 *   Jose Medrano <josmed@microsoft.com>
 *
 * Copyright (C) 2018 Microsoft, Corp
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#if MAC

using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using System;
using MonoDevelop.Components;
using Xamarin.PropertyEditing;
using Xamarin.PropertyEditing.Mac;
using AppKit;
using CoreGraphics;

namespace MonoDevelop.DesignerSupport
{
	class MacPropertyGrid : NSStackView, IPropertyGrid
	{
		MacPropertyEditorPanel propertyEditorPanel;
		PropertyPadEditorProvider editorProvider;


		public event EventHandler Focused;

		public bool IsEditing => false;

		public event EventHandler PropertyGridChanged;

		public MacPropertyGrid () 
		{
			Orientation = NSUserInterfaceLayoutOrientation.Vertical;
			Alignment = NSLayoutAttribute.Leading;
			Spacing = 10;
			Distribution = NSStackViewDistribution.Fill;

			propertyEditorPanel = new MacPropertyEditorPanel (new MonoDevelopHostResourceProvider ()) {
				ShowHeader = false
			};
			AddArrangedSubview (propertyEditorPanel);
			//propertyEditorPanel.PropertiesChanged += PropertyEditorPanel_PropertiesChanged;
		}

		public override void SetFrameSize (CGSize newSize)
		{
			propertyEditorPanel.SetFrameSize (newSize);
			base.SetFrameSize (newSize);
		}

		void PropertyEditorPanel_PropertiesChanged (object sender, EventArgs e) => PropertyGridChanged?.Invoke (this, e);

		public void BlankPad ()
		{
			propertyEditorPanel.SelectedItems.Clear ();
			currentSelectedObject = null;
		}

		public void OnPadContentShown ()
		{
			if (editorProvider == null) {
				editorProvider = new PropertyPadEditorProvider ();
				propertyEditorPanel.TargetPlatform = new TargetPlatform (editorProvider) {
					AutoExpandAll = true
				};
				propertyEditorPanel.ArrangeMode = PropertyArrangeMode.Category;
			}
		}

		PropertyPadItem currentSelectedObject;

		public void SetCurrentObject (object lastComponent, object [] propertyProviders)
		{
			if (lastComponent != null) {
				var selection = new PropertyPadItem (lastComponent, propertyProviders);
				if (currentSelectedObject != selection) {
					propertyEditorPanel.SelectedItems.Clear ();
					propertyEditorPanel.SelectedItems.Add (selection);
					currentSelectedObject = selection;
				}
			}
		}

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);
		}

		public void Populate (bool saveEditSession)
		{
			//not implemented
		}

		public void SetToolbarProvider (Components.PropertyGrid.PropertyGrid.IToolbarProvider toolbarProvider)
		{
			//not implemented
		}
	}

	class MacPropertyEditorPanel : PropertyEditorPanel
	{
		public EventHandler Focused;

		public MacPropertyEditorPanel (MonoDevelopHostResourceProvider hostResources)
			: base (hostResources)
		{

		}

		public override bool BecomeFirstResponder ()
		{
			Focused?.Invoke (this, EventArgs.Empty);
			return base.BecomeFirstResponder ();
		}
	}
}

#endif