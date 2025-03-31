using System;
using Microsoft.Win32.SafeHandles;
using ProSuite.Commons.Essentials.Assertions;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using ProSuite.Commons.Diagnostics;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Windows
{
	public static class CursorUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static Cursor GetCursor([NotNull] byte[] cursorBytes)
		{
			Assert.ArgumentNotNull(cursorBytes, nameof(cursorBytes));

			return new Cursor(new MemoryStream(cursorBytes));
		}

		public static Cursor CreateCursor(byte[] baseImage,
										  byte[] overlayImage,
										  int xHotspot = 0,
										  int yHotspot = 0)
		{
			return CreateCursor(baseImage, overlayImage, overlay2: null, overlay3: null, xHotspot, yHotspot);
		}

		public static Cursor CreateCursor(byte[] baseImage,
										  byte[] overlay1 = null,
										  byte[] overlay2 = null,
										  byte[] overlay3 = null,
										  int xHotspot = 0,
										  int yHotspot = 0)
		{
			Assert.ArgumentNotNull(baseImage, nameof(baseImage));

			// https://stackoverflow.com/questions/14866603/a-generic-error-occurred-in-gdi-when-attempting-to-use-image-save
			// https://stackoverflow.com/questions/72010973/a-generic-error-occurred-in-gdi-while-saving-image-to-memorystream
			// https://stackoverflow.com/questions/5813633/a-generic-error-occurs-at-gdi-at-bitmap-save-after-using-savefiledialog

			try
			{
				var destinationRectangle = new Rectangle(0, 0, 32, 32);

				using var result = new Bitmap(32, 32);
				using (Graphics graphics = Graphics.FromImage(result))
				{
					graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

					using (var stream = new MemoryStream(baseImage))
					using (Image image = Image.FromStream(stream))
					{
						graphics.DrawImage(image, destinationRectangle);
					}

					if (overlay1 != null)
					{
						using (var stream = new MemoryStream(overlay1))
						using (Image image = Image.FromStream(stream))
						{
							graphics.DrawImage(image, destinationRectangle);
						}
					}

					if (overlay2 != null)
					{
						using (var stream = new MemoryStream(overlay2))
						using (Image image = Image.FromStream(stream))
						{
							graphics.DrawImage(image, destinationRectangle);
						}
					}

					if (overlay3 != null)
					{
						using (var stream = new MemoryStream(overlay3))
						using (Image image = Image.FromStream(stream))
						{
							graphics.DrawImage(image, destinationRectangle);
						}
					}

					using (Bitmap clone = result.Clone(destinationRectangle, result.PixelFormat))
					{
						var icon = new IconInfo();
						GetIconInfo(clone.GetHicon(), ref icon);
						icon.xHotspot = xHotspot;
						icon.yHotspot = yHotspot;
						icon.fIcon = false;

						nint ptr = CreateIconIndirect(ref icon);

						return CursorInteropHelper.Create(new SafeIconHandle(ptr, true));
					}
				}
			}
			catch (Exception ex)
			{
				var info = new MemoryUsageInfo();
				_msg.Debug($"PB: {info.PrivateBytes:N0}. {ex.Message}", ex);

				return Cursors.Cross;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct IconInfo
		{
			public bool fIcon;
			public int xHotspot;
			public int yHotspot;
			public readonly nint hbmMask;
			public readonly nint hbmColor;
		}

		[DllImport("user32.dll")]
		private static extern nint CreateIconIndirect(ref IconInfo icon);

		[DllImport("user32.dll")]
		private static extern bool GetIconInfo(nint handle, ref IconInfo pIconInfo);

		private class SafeIconHandle : SafeHandleZeroOrMinusOneIsInvalid
		{
			public SafeIconHandle(nint hIcon, bool ownsHandle) : base(ownsHandle)
			{
				SetHandle(hIcon);
			}

			[DllImport("user32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			private static extern bool DestroyIcon([In] nint hIcon);

			protected override bool ReleaseHandle()
			{
				return DestroyIcon(handle);
			}
		}
	}
}
