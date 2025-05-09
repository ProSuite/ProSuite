using System;
using System.Windows;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.UI.Persistence.WPF
{
	// Mostly identical to old WinForms FormStateManager, but:
	// - screen coordinates (position, size) are double (not int)
	// - Topmost (not TopMost; spelling as in WPF)
	// - IsMaximized (instead of FormWindowState) to avoid the WPF dependency

	/// <summary>
	/// Save and restore window position, size, and optionally
	/// custom user state. Persist state in an XML file in the
	/// user's AppData directory. If the form's class name
	/// </summary>
	/// <typeparam name="T">The type of form state</typeparam>
	/// <remarks>Same interface as the old WinForms solution
	/// for easy migration from WinForms to WPF; in your usings,
	/// place EITHER ...Persistence.WinForms OR ...Persistence.WPF,
	/// but not both.</remarks>
	public class FormStateManager<T> where T : FormState
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly string _formID;
		private readonly string _fileName;

		public FormStateManager([NotNull] Window form, string callingContextID = null)
		{
			Form = form ?? throw new ArgumentNullException(nameof(form));

			_formID = form.GetType().Name;
			_fileName = FormStatePersistence.GetFileName(_formID, callingContextID);
		}

		/// <summary>
		/// Save form (window/dialog) state.
		/// </summary>
		/// <returns>True if form state was saved;
		/// false if an error occurred</returns>
		/// <remarks>All exceptions are ignored to make sure that
		/// this function never breaks the application.</remarks>
		public bool SaveState()
		{
			if (Form.WindowState == WindowState.Minimized)
			{
				return false; // don't save state if minimized
			}

			try
			{
				T formState = GetCurrentState();

				FormStatePersistence.WriteToXml(_fileName, formState, typeof(T));
			}
			catch (Exception ex)
			{
				_msg.Warn("Error saving window state", ex);

				return false;
			}

			return true;
		}

		/// <summary>
		/// Restore the saved form state (if such exists) for the form.
		/// </summary>
		/// <returns>True if saved form state was found and has been
		/// successfully applied; false if no form state exists for
		/// the form or an error occurred</returns>
		/// <remarks>All exceptions are ignored to make sure that
		/// this function never breaks the application.</remarks>
		public bool RestoreState(FormStateRestoreOption option = FormStateRestoreOption.Normal)
		{
			T formState;

			try
			{
				formState = (T) FormStatePersistence.ReadFromXml(_fileName, typeof(T));
			}
			catch (Exception ex)
			{
				_msg.Warn("Error restoring saved window state", ex);

				return false;
			}

			if (formState is null)
			{
				return false;
			}

			// get the current form state for rollback in case of exception
			T origFormState = GetCurrentState();

			try
			{
				ApplyFormState(formState, option);
			}
			catch (Exception ex)
			{
				// roll back and ignore the exception
				try
				{
					ApplyFormState(origFormState);
				}
				catch
				{
					_msg.Warn("Unable to roll back form state", ex);
				}
				finally
				{
					_msg.Warn("Error applying stored form state", ex);
				}

				return false;
			}

			return true;
		}

		#region Non-public members

		[NotNull]
		protected Window Form { get; }

		[NotNull]
		private T GetCurrentState()
		{
			var formState = (T) Activator.CreateInstance(typeof(T));

			Assert.NotNull(formState);

			formState.Width = Form.Width;
			formState.Height = Form.Height;
			formState.Left = Form.Left;
			formState.Top = Form.Top;
			formState.IsMaximized = Form.WindowState == WindowState.Maximized;
			formState.Topmost = Form.Topmost;

			GetUserFormState(formState);

			return formState;
		}

		private void GetUserFormState([NotNull] T formState)
		{
			// if the form implements IFormStateAware<T>, give it a
			// chance to store internal form state

			if (Form is IFormStateAware<T> target)
			{
				target.SaveState(formState);
			}
		}

		private void ApplyFormState([NotNull] T formState,
		                            FormStateRestoreOption option = FormStateRestoreOption.Normal)
		{
			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.Debug($"Applying form state for {_formID}");
			}

			using (_msg.IncrementIndentation())
			{
				switch (option)
				{
					case FormStateRestoreOption.OnlyLocation:
						ApplyLocation(formState);
						ApplyTopMost(formState);
						// don't apply size
						// don't apply window state
						ApplyUserFormState(formState);
						break;

					case FormStateRestoreOption.KeepLocation:
						// don't apply location
						ApplyTopMost(formState);
						ApplySize(formState);
						ApplyWindowState(formState);
						ApplyUserFormState(formState);
						break;

					case FormStateRestoreOption.Normal:
						ApplyLocation(formState);
						ApplyTopMost(formState);
						ApplySize(formState);
						ApplyWindowState(formState);
						ApplyUserFormState(formState);
						break;

					default:
						throw new ArgumentOutOfRangeException(
							nameof(option), option, "Unknown restore option");
				}
			}
		}

		private void ApplyLocation([NotNull] T formState)
		{
			if (!formState.HasLocation)
			{
				return;
			}

			// TODO Should get the actual screen/working area given
			// the form state's top left corner, but WPF cannot do that
			// (WinForms could and can, but then we'd have to convert coords...)

			//const int minVisibleOffset = 80;
			//var upperLeft = new Point(formState.Left, formState.Top);
			//Rectangle workingArea = Screen.GetWorkingArea(upperLeft);
			//int maxLeft = workingArea.Right - minVisibleOffset;
			//int maxTop = workingArea.Height - minVisibleOffset;
			//int minTop = workingArea.Top;
			//int minLeft = workingArea.Left - minVisibleOffset;

			var vsLeft = SystemParameters.VirtualScreenLeft;
			var vsTop = SystemParameters.VirtualScreenTop;
			var vsWidth = SystemParameters.VirtualScreenWidth;
			var vsHeight = SystemParameters.VirtualScreenHeight;

			const double minVisibleOffset = 200;

			var minLeft = vsLeft;
			var minTop = vsTop;
			var maxLeft = vsLeft + vsWidth - minVisibleOffset;
			var maxTop = vsTop + vsHeight - minVisibleOffset;

			double left = Clamp(formState.Left, minLeft, maxLeft);
			double top = Clamp(formState.Top, minTop, maxTop);

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Virtual screen: left {0}, top {1}, width {2}, height {3}",
				                 vsLeft, vsTop, vsWidth, vsHeight);
				_msg.DebugFormat("Restore location: {0} {1}", left, top);
			}

			Form.WindowStartupLocation = WindowStartupLocation.Manual; // TODO needed?
			Form.Left = left;
			Form.Top = top;
		}

		private void ApplySize([NotNull] T formState)
		{
			if (!formState.HasSize)
			{
				return;
			}

			//int maxWidth = Screen.PrimaryScreen.WorkingArea.Width;
			//int maxHeight = Screen.PrimaryScreen.WorkingArea.Height;

			// For simplicity (and today), limit size to
			// the size of the primary monitor's work area:

			double maxWidth = SystemParameters.WorkArea.Width;
			double maxHeight = SystemParameters.WorkArea.Height;

			double width = Math.Min(formState.Width, maxWidth);
			double height = Math.Min(formState.Height, maxHeight);

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.Debug($"Restore size: {width} {height}");
			}

			Form.Width = width;
			Form.Height = height;
		}

		private void ApplyWindowState([NotNull] T formState)
		{
			// don't restore minimized state
			if (formState.IsMaximized)
			{
				Form.WindowState = WindowState.Maximized;
			}
		}

		private void ApplyTopMost([NotNull] T formState)
		{
			Form.Topmost = formState.Topmost;
		}

		private void ApplyUserFormState([NotNull] T formState)
		{
			if (Form is IFormStateAware<T> target)
			{
				target.RestoreState(formState);
			}
		}

		private static double Clamp(double value, double min, double max)
		{
			if (value < min) return min;
			if (value > max) return max;
			return value;
		}

		#endregion
	}
}
