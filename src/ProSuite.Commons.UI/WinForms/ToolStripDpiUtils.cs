using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms
{
	public static class ToolStripDpiUtils
	{
		// A second call (e.g. ScaleImagesToDpi, then SetImageSize) must scale from the original
		// image, not from the smaller copy of the first call.
		private static readonly ConditionalWeakTable<ToolStripItem, Image> _originalImages =
			new ConditionalWeakTable<ToolStripItem, Image>();

		/// <summary>
		/// Scales the designer's <see cref="ToolStrip.ImageScalingSize"/> (set for 96 DPI) of all
		/// tool strips and context menus below <paramref name="control"/> to the screen DPI.
		/// WinForms scales the rest of the layout but leaves this size as it is, so the icons
		/// shrink on high-DPI screens. Images larger than that size are pre-scaled with
		/// high-quality interpolation: the tool strip's own scaling drops or doubles 1 px
		/// details, which makes e.g. a symmetric border look thicker on one side.
		/// Call it once, after <c>InitializeComponent</c>.
		/// </summary>
		public static void ScaleImagesToDpi([NotNull] Control control)
		{
			float factor = control.DeviceDpi / 96f;

			ScaleImages(control,
			            size => new Size((int) (size.Width * factor),
			                             (int) (size.Height * factor)),
			            false, new HashSet<ToolStrip>());
		}

		/// <summary>
		/// Shows the images of all tool strips and context menus below <paramref name="control"/>
		/// at <paramref name="size"/> pixels (at 96 DPI) scaled to the screen DPI, also items whose
		/// designer setting is <see cref="ToolStripItemImageScaling.None"/>. For tool strips whose
		/// images are stored larger than shown. Call it once, after <c>InitializeComponent</c>.
		/// </summary>
		public static void SetImageSize([NotNull] Control control, int size)
		{
			int scaled = (int) Math.Round(size * control.DeviceDpi / 96f);

			ScaleImages(control, _ => new Size(scaled, scaled), true, new HashSet<ToolStrip>());
		}

		private static void ScaleImages([NotNull] Control control,
		                                [NotNull] Func<Size, Size> getSize, bool sizeToFit,
		                                [NotNull] HashSet<ToolStrip> scaled)
		{
			if (control is ToolStrip toolStrip)
			{
				Scale(toolStrip, getSize, sizeToFit, scaled);
			}

			if (control.ContextMenuStrip != null)
			{
				Scale(control.ContextMenuStrip, getSize, sizeToFit, scaled);
			}

			foreach (Control child in control.Controls)
			{
				ScaleImages(child, getSize, sizeToFit, scaled);
			}
		}

		private static void Scale([NotNull] ToolStrip toolStrip,
		                          [NotNull] Func<Size, Size> getSize, bool sizeToFit,
		                          [NotNull] HashSet<ToolStrip> scaled)
		{
			// A context menu can be shared by several controls.
			if (! scaled.Add(toolStrip))
			{
				return;
			}

			Size targetSize = getSize(toolStrip.ImageScalingSize);
			toolStrip.ImageScalingSize = targetSize;

			// Items keep the image size computed for the old scaling size (and draw the icon
			// tiny until the next resize) unless their image is set again.
			foreach (ToolStripItem item in toolStrip.Items)
			{
				if (sizeToFit)
				{
					item.ImageScaling = ToolStripItemImageScaling.SizeToFit;
				}

				if (item is ToolStripDropDownItem dropDownItem &&
				    dropDownItem.HasDropDownItems)
				{
					Scale(dropDownItem.DropDown, getSize, sizeToFit, scaled);
				}

				if (item.Image is null ||
				    item.ImageScaling != ToolStripItemImageScaling.SizeToFit)
				{
					continue;
				}

				Image image = _originalImages.GetValue(item, i => i.Image);

				item.Image = null;
				item.Image = GetImageForSize(image, targetSize);
			}
		}

		[NotNull]
		private static Image GetImageForSize([NotNull] Image image, Size size)
		{
			if (image.Width <= size.Width && image.Height <= size.Height)
			{
				return image;
			}

			return ImageDpiUtils.Scale(image, size);
		}
	}
}
