using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Media;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Drawing;
using ProSuite.Commons.UI.WinForms;

namespace ProSuite.Commons.AGP.Framework;

/// <summary>
/// Makes WinForms windows shown in ArcGIS Pro look like Pro's own: the dark theme when Pro
/// uses it, and the image of the command that opened the window as its icon.
/// </summary>
public static class FormStyleUtils
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private const int _logicalIconSize = 16;

	public static bool IsDarkTheme =>
		FrameworkApplication.ApplicationTheme == ApplicationTheme.Dark;

	/// <param name="form">The window, before it is shown.</param>
	/// <param name="damlId">The DAML ID of the command whose image becomes the window icon.
	/// Tool windows (<see cref="FormBorderStyle.FixedToolWindow"/>,
	/// <see cref="FormBorderStyle.SizableToolWindow"/>) have no icon in their title bar.</param>
	public static void ApplyProStyle([NotNull] Form form, [CanBeNull] string damlId = null)
	{
		if (IsDarkTheme)
		{
			WinFormsThemeUtils.ApplyDarkTheme(form);
		}

		if (damlId == null)
		{
			return;
		}

		Icon icon = CreateIcon(damlId, form.LogicalToDeviceUnits(_logicalIconSize));

		if (icon == null)
		{
			return;
		}

		form.Icon = icon;
		form.ShowIcon = true;
		form.Disposed += (sender, args) => icon.Dispose();
	}

	/// <summary>
	/// The image of the command <paramref name="damlId"/>, for a WPF window's icon.
	/// </summary>
	[CanBeNull]
	public static ImageSource GetCommandImage([NotNull] string damlId)
	{
		return FrameworkApplication.GetPlugInWrapper(damlId)?.SmallImage as ImageSource;
	}

	/// <remarks>
	/// Like the command buttons in tool strips: a failure to rasterize the image (e.g. in a
	/// remote desktop session) leaves the window without its icon but fully functional.
	/// </remarks>
	[CanBeNull]
	private static Icon CreateIcon([NotNull] string damlId, int pixelSize)
	{
		ImageSource imageSource = GetCommandImage(damlId);

		if (imageSource == null)
		{
			return null;
		}

		try
		{
			using (Bitmap bitmap = BitmapUtils.CreateBitmap(imageSource, pixelSize))
			{
				if (bitmap == null)
				{
					return null;
				}

				IntPtr handle = bitmap.GetHicon();

				try
				{
					// Clone: an icon from a handle does not own it.
					using (Icon icon = Icon.FromHandle(handle))
					{
						return (Icon) icon.Clone();
					}
				}
				finally
				{
					DestroyIcon(handle);
				}
			}
		}
		catch (Exception ex)
		{
			_msg.Warn($"Error creating the window icon of {damlId}. " +
			          "The window is shown without it.", ex);
			return null;
		}
	}

	[DllImport("user32.dll")]
	private static extern bool DestroyIcon(IntPtr handle);
}
