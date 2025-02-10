using Microsoft.Win32.SafeHandles;
using ProSuite.Commons.Essentials.Assertions;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Windows
{
	public static class CursorUtils
	{
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

			var result = new Bitmap(32, 32);
			var destinationRectangle = new Rectangle(0, 0, 32, 32);

			using (var graphics = Graphics.FromImage(result))
			{
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

				graphics.DrawImage(CreateImage(baseImage), destinationRectangle);

				if (overlay1 != null)
				{
					graphics.DrawImage(CreateImage(overlay1), destinationRectangle);
				}

				if (overlay2 != null)
				{
					graphics.DrawImage(CreateImage(overlay2), destinationRectangle);
				}

				if (overlay3 != null)
				{
					graphics.DrawImage(CreateImage(overlay3), destinationRectangle);
				}
			}

			var icon = new IconInfo();
			GetIconInfo(result.GetHicon(), ref icon);
			icon.xHotspot = xHotspot;
			icon.yHotspot = yHotspot;
			icon.fIcon = false;

			nint ptr = CreateIconIndirect(ref icon);

			return CursorInteropHelper.Create(new SafeIconHandle(ptr, true));
		}

		private static Image CreateImage(byte[] resource)
		{
			using var stream = new MemoryStream(resource);
			return Image.FromStream(stream);
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
