using System;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using NUnit.Framework;
using ProSuite.Commons.UI.WinForms;
using IWin32Window = System.Windows.Forms.IWin32Window;

namespace ProSuite.Commons.UI.Test.WinForms
{
	/// <summary>
	/// A message box is attached to the main window only if the current thread created that
	/// window. In ArcGIS Pro the user interface thread is not managed thread 1, so a check of the
	/// thread number left the message box without owner, and Pro lost the focus.
	/// </summary>
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class WinFormUtilsTest
	{
		[Test]
		public void IsCreatedOnCurrentThread_WinFormsForm()
		{
			using (var form = new Form())
			{
				CreateHandle(form);

				AssertCreatedOnThisThreadOnly(form);
			}
		}

		[Test]
		public void IsCreatedOnCurrentThread_WinFormsFormWithoutHandle_IsFalse()
		{
			using (var form = new Form())
			{
				Assert.False(WinFormUtils.IsCreatedOnCurrentThread(form));
				Assert.False(form.IsHandleCreated);
			}
		}

		[Test]
		public void IsCreatedOnCurrentThread_WpfWindow()
		{
			var window = new Window();
			try
			{
				AssertCreatedOnThisThreadOnly(WinFormUtils.GetWin32Window(window));
			}
			finally
			{
				window.Close();
			}
		}

		[Test]
		public void IsCreatedOnCurrentThread_WindowHandle()
		{
			using (var form = new Form())
			{
				AssertCreatedOnThisThreadOnly(new WindowHandle(CreateHandle(form)));
			}
		}

		/// <summary>
		/// Reading the handle creates it. CreateControl does not, as long as the form is invisible.
		/// </summary>
		private static IntPtr CreateHandle(Control control)
		{
			return control.Handle;
		}

		private static void AssertCreatedOnThisThreadOnly(IWin32Window window)
		{
			Assert.True(WinFormUtils.IsCreatedOnCurrentThread(window));

			bool resultOnOtherThread = true;
			Exception exceptionOnOtherThread = null;

			var otherThread = new Thread(() =>
			{
				try
				{
					resultOnOtherThread = WinFormUtils.IsCreatedOnCurrentThread(window);
				}
				catch (Exception e)
				{
					exceptionOnOtherThread = e;
				}
			});
			otherThread.SetApartmentState(ApartmentState.STA);
			otherThread.Start();
			otherThread.Join();

			Assert.Null(exceptionOnOtherThread);
			Assert.False(resultOnOtherThread);
		}

		/// <summary>
		/// A window known only by its handle, as for a window of another user interface library.
		/// </summary>
		private class WindowHandle : IWin32Window
		{
			public WindowHandle(IntPtr handle)
			{
				Handle = handle;
			}

			public IntPtr Handle { get; }
		}
	}
}
