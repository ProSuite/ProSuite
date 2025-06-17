using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.UI.Persistence.WPF
{
	/// <summary>
	/// The same as the <see cref="FormStateManager{T}"/> but
	/// for UI elements that are not complete windows, such as
	/// dock panes in ArcGIS Pro. The UI element must implement
	/// <see cref="IFormStateAware{T}"/> and the caller must
	/// provide a “formID” string, e.g. a dock pane's DAML ID.
	/// </summary>
	public class UserStateManager<T> where T : FormState
	{
		private readonly IFormStateAware<T> _form;
		private readonly string _formID;
		private readonly string _fileName;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public UserStateManager(IFormStateAware<T> form, string formID, string callingContextID = null)
		{
			if (string.IsNullOrEmpty(formID))
				throw new ArgumentNullException(nameof(formID));

			_form = form ?? throw new ArgumentNullException(nameof(form));
			_formID = formID;
			_fileName = FormStatePersistence.GetFileName(formID, callingContextID);
		}

		/// <summary>
		/// Save form state to the current user's profile.
		/// </summary>
		/// <returns>True if form state was saved;
		/// false if an error occurred</returns>
		/// <remarks>Exceptions are ignored to make sure that
		/// this function never breaks the application.</remarks>
		public bool SaveState()
		{
			try
			{
				T formState = GetCurrentState();

				FormStatePersistence.WriteToXml(_fileName, formState, typeof(T));

				return true;
			}
			catch (Exception ex)
			{
				_msg.Warn("Error saving form state", ex);

				return false;
			}
		}

		/// <summary>
		/// Restore the saved form state (if such exists).
		/// </summary>
		/// <returns>True if saved form state was found and has been
		/// successfully applied; false if no form state exists for
		/// the form or an error occurred</returns>
		/// <remarks>Exceptions are ignored to make sure that
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
				_msg.Warn("Error restoring saved form state", ex);
				return false;
			}

			if (formState is null)
			{
				return false; // no saved form state
			}

			try
			{
				ApplyFormState(formState, option);
			}
			catch (Exception ex)
			{
				_msg.Warn("Error applying stored form state", ex);

				return false;
			}

			return true;
		}

		#region Non-public methods

		[NotNull]
		private T GetCurrentState()
		{
			var formState = (T) Activator.CreateInstance(typeof(T));

			Assert.NotNull(formState);

			formState.Topmost = false;
			formState.IsMaximized = false;
			formState.Top = formState.Left = double.NaN;
			formState.Width = formState.Height = double.NaN;

			_form.SaveState(formState);

			return formState;
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
					case FormStateRestoreOption.KeepLocation:
					case FormStateRestoreOption.Normal:
						_form.RestoreState(formState);
						break;

					default:
						throw new ArgumentOutOfRangeException(
							nameof(option), option, "Unknown restore option");
				}
			}
		}

		#endregion
	}
}
