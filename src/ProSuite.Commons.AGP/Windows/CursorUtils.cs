using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Win32.SafeHandles;
using ProSuite.Commons.Diagnostics;
using ProSuite.Commons.Essentials.Assertions;
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
			return CreateCursor(baseImage, overlayImage, overlay2: null, overlay3: null, xHotspot,
			                    yHotspot);
		}

		public static Cursor CreateCursor(byte[] baseImage,
		                                  byte[] overlay1 = null,
		                                  byte[] overlay2 = null,
		                                  byte[] overlay3 = null,
		                                  int xHotspot = 0,
		                                  int yHotspot = 0)
		{
			Assert.ArgumentNotNull(baseImage, nameof(baseImage));

			try
			{
				var destinationRectangle = new Rectangle(0, 0, 32, 32);

				using var result = new Bitmap(32, 32);
				using (Graphics graphics = Graphics.FromImage(result))
				{
					graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

					var stream = new MemoryStream(baseImage);
					MemoryStream stream1 = null;
					MemoryStream stream2 = null;
					MemoryStream stream3 = null;

					try
					{
						if (overlay1 != null)
						{
							stream1 = new MemoryStream(overlay1);
						}

						if (overlay2 != null)
						{
							stream2 = new MemoryStream(overlay2);
						}

						if (overlay3 != null)
						{
							stream3 = new MemoryStream(overlay3);
						}

						using (Bitmap tmp = new Bitmap(stream))
						{
							// https://jira.swisstopo.ch/browse/GOTOP-450
							var bitmap = new Bitmap(tmp);
							graphics.DrawImage(bitmap, destinationRectangle);
						}

						if (stream1 != null)
						{
							using (Bitmap tmp = new Bitmap(stream1))
							{
								var bitmap = new Bitmap(tmp);
								graphics.DrawImage(bitmap, destinationRectangle);
							}
						}

						if (stream2 != null)
						{
							using (Bitmap tmp = new Bitmap(stream2))
							{
								var bitmap = new Bitmap(tmp);
								graphics.DrawImage(bitmap, destinationRectangle);
							}
						}

						if (stream3 != null)
						{
							using (Bitmap tmp = new Bitmap(stream3))
							{
								var bitmap = new Bitmap(tmp);
								graphics.DrawImage(bitmap, destinationRectangle);
							}
						}

						using (Bitmap clone =
						       result.Clone(destinationRectangle, result.PixelFormat))
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
					finally
					{
						stream.Dispose();
						stream1?.Dispose();
						stream2?.Dispose();
						stream3?.Dispose();
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
