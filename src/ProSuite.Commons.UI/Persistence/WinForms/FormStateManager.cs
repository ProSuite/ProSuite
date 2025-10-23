using System;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.UI.Persistence.WinForms
{
	public class FormStateManager<T> where T : FormState
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly string _fileName;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="FormStateManager&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="form">The form.</param>
		/// <param name="callingContextID">The calling context ID.</param>
		public FormStateManager([NotNull] Form form,
		                        [CanBeNull] string callingContextID = null)
		{
			Assert.ArgumentNotNull(form, nameof(form));

			Form = form;
			_fileName = FormStatePersistence.GetFileName(form.GetType().Name, callingContextID);
		}

		#endregion

		/// <summary>
		/// Saves the form state. 
		/// </summary>
		/// <remarks>
		/// All exceptions are ignored to make sure that this miscellaneous function
		/// does never break the application. 
		/// </remarks>
		/// <returns>True if form state was successfully stored, false if an error
		/// occurred</returns>
		public bool SaveState()
		{
			if (Form.WindowState == FormWindowState.Minimized)
			{
				// don't save state if minimized
				return false;
			}

			try
			{
				// get the current form state
				T formState = GetCurrentState();

				// write form state
				FormStatePersistence.WriteToXml(_fileName, formState, typeof(T));
			}
			catch (Exception ex)
			{
				_msg.Warn("Error saving form state", ex);

				// ignore errors
				return false;
			}

			return true;
		}

		/// <summary>
		/// Restores the saved form state (if existing) for the form and 
		/// returns a value that indicates if the form state has been successfully
		/// restored.
		/// </summary>
		/// <param name="restoreOption">options for the restore.</param>
		/// <remarks>
		/// All exceptions are ignored to make sure that this miscellaneous function
		/// does never break the application. However, it is guaranteed that all
		/// changes are fully rolled back.
		/// </remarks>
		/// <returns>True if stored form state has been encountered and successfully
		/// applied, false if no stored form state existed for the form or an error
		/// occurred</returns>
		public bool RestoreState(
			FormStateRestoreOption restoreOption = FormStateRestoreOption.Normal)
		{
			// read state from file
			T formState;
			try
			{
				formState = (T) FormStatePersistence.ReadFromXml(_fileName, typeof(T));
			}
			catch (Exception e)
			{
				_msg.Warn(
					"Error restoring saved window state. Window state is reset to defaults.", e);

				// ignore the exception
				return false;
			}

			if (formState == null)
			{
				return false;
			}

			// get the current form state for rollback in case of exception
			T origFormState = GetCurrentState();

			try
			{
				ApplyFormState(formState, restoreOption);
			}
			catch (Exception e)
			{
				// roll back and ignore the exception
				try
				{
					ApplyFormState(origFormState);
				}
				catch
				{
					_msg.Warn("Unable to roll back form state", e);
				}
				finally
				{
					_msg.Warn("Error applying stored form state", e);
				}

				return false;
			}

			return true;
		}

		#region Non-public members

		[NotNull]
		protected Form Form { get; }

		private void ApplyFormState([NotNull] T formState)
		{
			ApplyFormState(formState, FormStateRestoreOption.Normal);
		}

		private void ApplyFormState([NotNull] T formState,
		                            FormStateRestoreOption restoreOption)
		{
			_msg.VerboseDebug(() => $"Applying form state for {Form.Name}");

			using (_msg.IncrementIndentation())
			{
				switch (restoreOption)
				{
					case FormStateRestoreOption.OnlyLocation:
						ApplyLocation(formState);
						ApplyTopMost(formState);
						// don't apply size
						// don't apply window state
						ApplyInternalFormState(formState);
						break;

					case FormStateRestoreOption.KeepLocation:
						// don't apply location
						ApplyTopMost(formState);
						ApplySize(formState);
						ApplyWindowState(formState);
						ApplyInternalFormState(formState);
						break;

					case FormStateRestoreOption.Normal:
						ApplyLocation(formState);
						ApplyTopMost(formState);
						ApplySize(formState);
						ApplyWindowState(formState);
						ApplyInternalFormState(formState);
						break;

					default:
						throw new ArgumentException(
							string.Format("Unknown restore option: {0}", restoreOption));
				}
			}
		}

		[NotNull]
		private T GetCurrentState()
		{
			var formState = (T) Activator.CreateInstance(typeof(T));

			Assert.NotNull(formState);

			// get standard form state
			formState.Width = Form.Size.Width;
			formState.Height = Form.Size.Height;
			formState.Left = Form.Location.X;
			formState.Top = Form.Location.Y;
			formState.WindowState = Form.WindowState;
			formState.TopMost = Form.TopMost;

			// get internal form state
			GetInternalState(formState);

			return formState;
		}

		private void GetInternalState([NotNull] T formState)
		{
			// if the form implements IFormStateAware<T>, give it a
			// chance to store internal form state

			if (Form is IFormStateAware<T> target)
			{
				target.GetState(formState);
			}
		}

		private void ApplyInternalFormState([NotNull] T formState)
		{
			// if the form implements IFormStateAware<T>, give it a
			// chance to restore internal form state
			var target = Form as IFormStateAware<T>;

			target?.RestoreState(formState);
		}

		private void ApplyLocation([NotNull] T formState)
		{
			if (! TryGetLocation(formState, out int left, out int top))
			{
				return;
			}

			// apply
			Form.StartPosition = FormStartPosition.Manual;
			Form.Location = new Point(left, top);
		}

		private static bool TryGetLocation(T formState, out int left, out int top)
		{
			if (! formState.HasLocation)
			{
				left = top = 0;
				return false;
			}

			const int minVisibleOffset = 80;

			var upperLeft = new Point(formState.Left, formState.Top);
			Rectangle workingArea = Screen.GetWorkingArea(upperLeft);

			int maxLeft = workingArea.Right - minVisibleOffset;
			int maxTop = workingArea.Height - minVisibleOffset;
			int minTop = workingArea.Top;
			int minLeft = workingArea.Left - minVisibleOffset;

			// try to restore location. Make sure that location is within current
			// screen dimensions
			left = formState.Left > maxLeft
				       ? maxLeft
				       : formState.Left < minLeft
					       ? minLeft
					       : formState.Left;

			top = formState.Top > maxTop
				      ? maxTop
				      : formState.Top < minTop
					      ? minTop
					      : formState.Top;

			// log 
			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Working area at {0}, {1}: {2}",
				                 formState.Left, formState.Top, workingArea);
				_msg.DebugFormat("Restore location: {0} {1}", left, top);
			}

			return true;
		}

		private void ApplySize([NotNull] T formState)
		{
			if (! formState.HasSize)
			{
				return;
			}

			int maxWidth = Screen.PrimaryScreen.WorkingArea.Width;
			int maxHeight = Screen.PrimaryScreen.WorkingArea.Height;

			// try to restore location. Make sure that location is within current
			// screen dimensions
			int width = formState.Width > maxWidth
				            ? maxWidth
				            : formState.Width;
			int height = formState.Height > maxHeight
				             ? maxHeight
				             : formState.Height;

			// log
			_msg.VerboseDebug(() => $"Restore size: {width} {height}");

			// apply
			Form.Size = new Size(width, height);
		}

		private void ApplyWindowState([NotNull] T formState)
		{
			// don't restore minimized state
			if (formState.WindowState != FormWindowState.Minimized)
			{
				Form.WindowState = formState.WindowState;
			}
		}

		private void ApplyTopMost([NotNull] T formState)
		{
			Form.TopMost = formState.TopMost;
		}

		#endregion
	}
}
