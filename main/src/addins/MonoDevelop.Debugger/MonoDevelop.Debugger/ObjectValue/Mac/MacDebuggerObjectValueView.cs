//
// MacDebuggerObjectValueView.cs
//
// Author:
//       Jeffrey Stedfast <jestedfa@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corp.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;

using AppKit;
using Foundation;
using CoreGraphics;

using Xwt.Drawing;

using MonoDevelop.Core;

namespace MonoDevelop.Debugger
{
	/// <summary>
	/// The NSTableViewCell used for the "Value" column.
	/// </summary>
	class MacDebuggerObjectValueView : MacDebuggerObjectCellViewBase
	{
		class EditableTextField : NSTextField
		{
			readonly MacDebuggerObjectValueView valueView;
			string oldValue, newValue;
			bool editing;

			public EditableTextField (MacDebuggerObjectValueView valueView)
			{
				this.valueView = valueView;
			}

			public override void DidBeginEditing (NSNotification notification)
			{
				base.DidBeginEditing (notification);
				valueView.TreeView.OnStartEditing ();
				oldValue = newValue = StringValue.Trim ();
				editing = true;
			}

			public override void DidChange (NSNotification notification)
			{
				newValue = StringValue.Trim ();
				base.DidChange (notification);
			}

			public override void DidEndEditing (NSNotification notification)
			{
				base.DidEndEditing (notification);

				if (!editing)
					return;

				editing = false;

				valueView.TreeView.OnEndEditing ();

				if (newValue != oldValue && valueView.TreeView.GetEditValue (valueView.Node, newValue))
					valueView.Refresh ();

				oldValue = newValue = null;
			}

			protected override void Dispose (bool disposing)
			{
				if (disposing)
					valueView.Dispose ();

				base.Dispose (disposing);
			}
		}

		readonly List<NSLayoutConstraint> constraints = new List<NSLayoutConstraint> ();
		NSImageView statusIcon;
		bool statusIconVisible;
		NSView colorPreview;
		bool colorPreviewVisible;
		NSButton valueButton;
		bool valueButtonVisible;
		NSButton viewerButton;
		bool viewerButtonVisible;
		bool disposed;

		public MacDebuggerObjectValueView (MacObjectValueTreeView treeView) : base (treeView, "value")
		{
			statusIcon = new NSImageView {
				TranslatesAutoresizingMaskIntoConstraints = false
			};

			colorPreview = new NSView (new CGRect (0, 0, 16, 16)) {
				TranslatesAutoresizingMaskIntoConstraints = false
			};

			valueButton = new NSButton {
				TranslatesAutoresizingMaskIntoConstraints = false,
				Title = GettextCatalog.GetString (""),
				BezelStyle = NSBezelStyle.Inline
			};
			valueButton.Cell.UsesSingleLineMode = true;
			valueButton.Font = NSFont.FromDescription (valueButton.Font.FontDescriptor, valueButton.Font.PointSize - 3);
			valueButton.Activated += OnValueButtonActivated;

			int imageSize = treeView.CompactView ? CompactImageSize : ImageSize;
			viewerButton = new NSButton {
				Image = GetImage (Gtk.Stock.Edit, imageSize, imageSize),
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			viewerButton.BezelStyle = NSBezelStyle.Inline;
			viewerButton.Bordered = false;
			viewerButton.Activated += OnViewerButtonActivated;

			TextField = new EditableTextField (this) {
				AutoresizingMask = NSViewResizingMask.WidthSizable,
				TranslatesAutoresizingMaskIntoConstraints = false,
				BackgroundColor = NSColor.Clear,
				Bordered = false,
				Editable = false
			};
			TextField.Cell.UsesSingleLineMode = true;
			TextField.Cell.Wraps = false;

			AddSubview (TextField);
		}

		public MacDebuggerObjectValueView (IntPtr handle) : base (handle)
		{
		}

		protected override void UpdateContents ()
		{
			if (Node == null)
				return;

			foreach (var constraint in constraints) {
				constraint.Active = false;
				constraint.Dispose ();
			}
			constraints.Clear ();

			var editable = TreeView.GetCanEditNode (Node);
			var textColor = NSColor.ControlText;
			string evaluateStatusIcon = null;
			string valueButtonText = null;
			var showViewerButton = false;
			Color? previewColor = null;
			string strval;

			if (Node.IsUnknown) {
				if (TreeView.DebuggerService.Frame != null) {
					strval = GettextCatalog.GetString ("The name '{0}' does not exist in the current context.", Node.Name);
				} else {
					strval = string.Empty;
				}
				evaluateStatusIcon = Ide.Gui.Stock.Warning;
			} else if (Node.IsError || Node.IsNotSupported) {
				evaluateStatusIcon = Ide.Gui.Stock.Warning;
				strval = Node.Value;
				int i = strval.IndexOf ('\n');
				if (i != -1)
					strval = strval.Substring (0, i);
				textColor = NSColor.FromCGColor (GetCGColor (Styles.ObjectValueTreeValueErrorText));
			} else if (Node.IsImplicitNotSupported) {
				strval = "";//val.Value; with new "Show Value" button we don't want to display message "Implicit evaluation is disabled"
				textColor = NSColor.FromCGColor (GetCGColor (Styles.ObjectValueTreeValueDisabledText));
				if (Node.CanRefresh)
					valueButtonText = GettextCatalog.GetString ("Show Value");
			} else if (Node.IsEvaluating) {
				strval = GettextCatalog.GetString ("Evaluating\u2026");

				evaluateStatusIcon = "md-spinner-16";

				textColor = NSColor.FromCGColor (GetCGColor (Styles.ObjectValueTreeValueDisabledText));
			} else if (Node.IsEnumerable) {
				if (Node is ShowMoreValuesObjectValueNode) {
					valueButtonText = GettextCatalog.GetString ("Show More");
				} else {
					valueButtonText = GettextCatalog.GetString ("Show Values");
				}
				strval = "";
			} else if (Node is AddNewExpressionObjectValueNode) {
				strval = string.Empty;
				editable = false;
			} else {
				strval = TreeView.Controller.GetDisplayValueWithVisualisers (Node, out showViewerButton);

				if (TreeView.Controller.GetNodeHasChangedSinceLastCheckpoint (Node))
					textColor = NSColor.FromCGColor (GetCGColor (Styles.ObjectValueTreeValueModifiedText));

				var val = Node.GetDebuggerObjectValue ();
				if (val != null && !val.IsNull && DebuggingService.HasGetConverter<Color> (val)) {
					try {
						previewColor = DebuggingService.GetGetConverter<Color> (val).GetValue (val);
					} catch {
						previewColor = null;
					}
				}
			}

			strval = strval.Replace ("\r\n", " ").Replace ("\n", " ");

			var views = new List<NSView> ();

			OptimalWidth = MarginSize;

			// First item: Status Icon
			if (evaluateStatusIcon != null) {
				statusIcon.Image = GetImage (evaluateStatusIcon, Gtk.IconSize.Menu);

				if (!statusIconVisible) {
					AddSubview (statusIcon);
					statusIconVisible = true;
				}

				constraints.Add (statusIcon.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor));
				constraints.Add (statusIcon.WidthAnchor.ConstraintEqualToConstant (ImageSize));
				constraints.Add (statusIcon.HeightAnchor.ConstraintEqualToConstant (ImageSize));
				views.Add (statusIcon);

				OptimalWidth += statusIcon.Image.Size.Width;
				OptimalWidth += RowCellSpacing;
			} else if (statusIconVisible) {
				statusIcon.RemoveFromSuperview ();
				statusIconVisible = false;
			}

			// Second Item: Color Preview
			if (previewColor != null) {
				colorPreview.Layer.BackgroundColor = GetCGColor (previewColor.Value);

				if (!colorPreviewVisible) {
					AddSubview (colorPreview);
					colorPreviewVisible = true;
				}

				constraints.Add (colorPreview.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor));
				constraints.Add (colorPreview.WidthAnchor.ConstraintEqualToConstant (ImageSize));
				constraints.Add (colorPreview.HeightAnchor.ConstraintEqualToConstant (ImageSize));
				views.Add (colorPreview);

				OptimalWidth += colorPreview.Frame.Width;
				OptimalWidth += RowCellSpacing;
			} else if (colorPreviewVisible) {
				colorPreview.RemoveFromSuperview ();
				colorPreviewVisible = false;
			}

			// Third Item: Value Button
			if (valueButtonText != null && !((MacObjectValueNode) ObjectValue).HideValueButton) {
				valueButton.AttributedTitle = GetAttributedString (valueButtonText);
				valueButton.SizeToFit ();

				if (!valueButtonVisible) {
					AddSubview (valueButton);
					valueButtonVisible = true;
				}

				constraints.Add (valueButton.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor));
				views.Add (valueButton);

				OptimalWidth += valueButton.Frame.Width;
				OptimalWidth += RowCellSpacing;
			} else if (valueButtonVisible) {
				valueButton.RemoveFromSuperview ();
				valueButtonVisible = false;
			}

			// Fourth Item: Viewer Button
			if (showViewerButton) {
				if (!viewerButtonVisible) {
					AddSubview (viewerButton);
					viewerButtonVisible = true;
				}

				constraints.Add (viewerButton.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor));
				constraints.Add (viewerButton.WidthAnchor.ConstraintEqualToConstant (viewerButton.Image.Size.Width));
				constraints.Add (viewerButton.HeightAnchor.ConstraintEqualToConstant (viewerButton.Image.Size.Height));
				views.Add (viewerButton);

				OptimalWidth += viewerButton.Frame.Width;
				OptimalWidth += RowCellSpacing;
			} else if (viewerButtonVisible) {
				viewerButton.RemoveFromSuperview ();
				viewerButtonVisible = false;
			}

			// Fifth Item: Text Value
			TextField.AttributedStringValue = GetAttributedString (strval);
			TextField.TextColor = textColor;
			TextField.Editable = editable;

			constraints.Add (TextField.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor));
			views.Add (TextField);

			// lay out our views...
			var leadingAnchor = LeadingAnchor;

			for (int i = 0; i < views.Count; i++) {
				var view = views[i];

				constraints.Add (view.LeadingAnchor.ConstraintEqualToAnchor (leadingAnchor, i == 0 ? MarginSize : RowCellSpacing));
				leadingAnchor = view.TrailingAnchor;
			}

			constraints.Add (TextField.TrailingAnchor.ConstraintEqualToAnchor (TrailingAnchor, -MarginSize));

			foreach (var constraint in constraints)
				constraint.Active = true;

			TextField.SizeToFit ();

			OptimalWidth += TextField.Frame.Width;
			OptimalWidth += MarginSize;
		}

		void OnValueButtonActivated (object sender, EventArgs e)
		{
			if (Node.IsEnumerable) {
				if (Node is ShowMoreValuesObjectValueNode moreNode) {
					TreeView.LoadMoreChildren (moreNode.EnumerableNode);
				} else {
					// use ExpandItem to expand so we see the loading message, expanding the node will trigger a fetch of the children
					TreeView.ExpandItem (ObjectValue, false);
				}
			} else {
				// this is likely to support IsImplicitNotSupported
				TreeView.Refresh (Node);
			}

			((MacObjectValueNode) ObjectValue).HideValueButton = true;
			Refresh ();
		}

		void OnViewerButtonActivated (object sender, EventArgs e)
		{
			if (!TreeView.DebuggerService.CanQueryDebugger)
				return;

			if (TreeView.ShowVisualizer (Node))
				Refresh ();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing && !disposed) {
				viewerButton.Activated -= OnViewerButtonActivated;
				valueButton.Activated -= OnValueButtonActivated;
				foreach (var constraint in constraints)
					constraint.Dispose ();
				constraints.Clear ();
				disposed = true;
			}

			base.Dispose (disposing);
		}
	}
}
