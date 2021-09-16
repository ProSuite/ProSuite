using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.UI.Persistence.WinForms
{
	/// <summary>
	/// Manager for saving and restoring window geometry settings. The settings are
	/// written to an xml file with a name including the form type and an optional
	/// calling context identifier. 
	/// </summary>
	public class WindowSettingsManager
	{
		private readonly Form _form;
		private readonly string _fileName;
		private readonly Type _settingsType;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		#region Static factory methods

		public static WindowSettingsManager GetManager(Form form, Type settingsType)
		{
			return GetManager(form, null, settingsType);
		}

		/// <summary>
		/// Creates a WindowSettingsManager object for a form in a uniquely 
		/// identified calling context and using a given WindowSettings subtype for
		/// storing the serializable settings.
		/// </summary>
		/// <param name="form">form for which the manager should save and restore
		/// settings</param>
		/// <param name="callingContextID">string identifying the calling context of
		/// the form, to distinguish settings for different usage situations
		/// of a given form type</param>
		/// <param name="settingsType">type of settings class that stores the 
		/// settings in xml-serializable form. Must be a subclass of WindowSettings
		/// </param>
		/// <returns>WindowSettingsManager object</returns>
		public static WindowSettingsManager GetManager(Form form,
		                                               string callingContextID = null,
		                                               Type settingsType = null)
		{
			Type baseSettingsType = typeof(WindowSettings);

			if (settingsType == null)
			{
				// use default settings type
				settingsType = baseSettingsType;
			}
			else
			{
				// validate settings type parameter
				if (! settingsType.IsSubclassOf(baseSettingsType))
				{
					throw new ArgumentException(
						string.Format(
							"Invalid settings type: {0}; expected subclass of {1}",
							settingsType.Name,
							baseSettingsType.Name),
						nameof(settingsType));
				}
			}

			return new WindowSettingsManager(form, callingContextID, settingsType);
		}

		#endregion

		#region Constructors

		private WindowSettingsManager(Form form, string callingContextID,
		                              Type settingsType)
		{
			_form = form;
			_settingsType = settingsType;
			_fileName = GetFileName(form, callingContextID);
		}

		#endregion

		/// <summary>
		/// Saves the window settings for the form. 
		/// </summary>
		/// <remarks>
		/// All exceptions are ignored to make sure that this miscellaneous function
		/// does never break the application. 
		/// </remarks>
		/// <returns>True if settings were successfully stored, false if an error
		/// occurred</returns>
		public bool SaveSettings()
		{
			if (_form.WindowState == FormWindowState.Minimized)
			{
				// don't save state if minimized
				return false;
			}
			else
			{
				try
				{
					// get the current settings for the form
					WindowSettings settings = GetCurrentSettings();

					// write settings
					FormStatePersistence.WriteToXml(_fileName, settings, _settingsType);
				}
				catch (Exception ex)
				{
					_msg.Warn("Error saving window settings", ex);

					// ignore errors
					return false;
				}

				return true;
			}
		}

		/// <summary>
		/// Restores the saved geometry settings (if existing) for the form and 
		/// returns a value that indicates if the settings have been successfully
		/// restored.
		/// </summary>
		/// <remarks>
		/// All exceptions are ignored to make sure that this miscellaneous function
		/// does never break the application. However, it is guaranteed that all
		/// changes are fully rolled back.
		/// </remarks>
		/// <returns>True if stored settings have been encountered and successfully
		/// applied, false if no stored settings existed for the form or an error
		/// occurred</returns>
		public bool RestoreSettings()
		{
			return RestoreSettings(FormStateRestoreOption.Normal);
		}

		/// <summary>
		/// Restores the saved geometry settings (if existing) for the form and 
		/// returns a value that indicates if the settings have been successfully
		/// restored.
		/// </summary>
		/// <param name="restoreOption">options for the restore.</param>
		/// <remarks>
		/// All exceptions are ignored to make sure that this miscellaneous function
		/// does never break the application. However, it is guaranteed that all
		/// changes are fully rolled back.
		/// </remarks>
		/// <returns>True if stored settings have been encountered and successfully
		/// applied, false if no stored settings existed for the form or an error
		/// occurred</returns>
		public bool RestoreSettings(FormStateRestoreOption restoreOption)
		{
			// read settings from file
			WindowSettings settings;
			try
			{
				settings = (WindowSettings) FormStatePersistence.ReadFromXml(
					_fileName, _settingsType);
			}
			catch (Exception e)
			{
				_msg.Warn("Error reading stored settings", e);

				// ignore the exception
				return false;
			}

			if (settings == null)
			{
				return false;
			}
			else
			{
				// get the current settings for rollback in case of exception
				WindowSettings origSettings = GetCurrentSettings();

				try
				{
					ApplySettings(settings, restoreOption);
				}
				catch (Exception e)
				{
					// roll back and ignore the exception
					try
					{
						ApplySettings(origSettings);
					}
					catch
					{
						_msg.Warn("Unable to roll back window settings", e);
					}
					finally
					{
						_msg.Warn("Error applying stored window settings", e);
					}

					return false;
				}
			}

			return true;
		}

		#region Non-public members

		private void ApplySettings(WindowSettings settings)
		{
			ApplySettings(settings, FormStateRestoreOption.Normal);
		}

		private void ApplySettings(WindowSettings settings,
		                           FormStateRestoreOption restoreOption)
		{
			switch (restoreOption)
			{
				case FormStateRestoreOption.OnlyLocation:
					ApplyLocation(settings);
					ApplyTopMost(settings);
					break;

				case FormStateRestoreOption.Normal:
					ApplyLocation(settings);
					ApplyTopMost(settings);
					ApplySize(settings);
					ApplyWindowState(settings);
					ApplyInternalSettings(settings);
					break;

				default:
					throw new ArgumentException(
						string.Format("Unknown restore option: {0}", restoreOption));
			}
		}

		private WindowSettings GetCurrentSettings()
		{
			var settings = (WindowSettings) Activator.CreateInstance(
				_settingsType);

			// store standard settings
			settings.Width = _form.Size.Width;
			settings.Height = _form.Size.Height;
			settings.Left = _form.Location.X;
			settings.Top = _form.Location.Y;
			settings.WindowState = _form.WindowState;
			settings.TopMost = _form.TopMost;

			// get internal form settings
			GetInternalSettings(settings);

			return settings;
		}

		private void GetInternalSettings(WindowSettings settings)
		{
			// if the form implements IWindowSettingsTarget, give it a
			// chance to store internal settings
			if (_form is IWindowSettingsTarget target)
			{
				target.SaveSettings(settings);
			}
		}

		private void ApplyInternalSettings(WindowSettings settings)
		{
			// if the form implements IWindowSettingsTarget, give it a
			// chance to restore internal settings
			if (_form is IWindowSettingsTarget target)
			{
				target.RestoreSettings(settings);
			}
		}

		private void ApplyLocation(WindowSettings settings)
		{
			if (! settings.HasLocation)
			{
				return;
			}

			const int minVisibleOffset = 80;

			var upperLeft = new Point(settings.Left, settings.Top);
			Rectangle workingArea = Screen.GetWorkingArea(upperLeft);

			int maxLeft = workingArea.Right - minVisibleOffset;
			int maxTop = workingArea.Height - minVisibleOffset;
			int minTop = workingArea.Top;
			int minLeft = workingArea.Left - minVisibleOffset;

			// try to restore location. Make sure that location is within current
			// screen dimensions
			int left = settings.Left > maxLeft
				           ? maxLeft
				           : settings.Left < minLeft
					           ? minLeft
					           : settings.Left;

			int top = settings.Top > maxTop
				          ? maxTop
				          : settings.Top < minTop
					          ? minTop
					          : settings.Top;

			_form.StartPosition = FormStartPosition.Manual;
			_form.Location = new Point(left, top);
		}

		private void ApplySize(WindowSettings settings)
		{
			if (! settings.HasSize)
			{
				return;
			}

			int maxWidth = Screen.PrimaryScreen.WorkingArea.Width;
			int maxHeight = Screen.PrimaryScreen.WorkingArea.Height;

			// try to restore location. Make sure that location is within current
			// screen dimensions
			int width = settings.Width > maxWidth
				            ? maxWidth
				            : settings.Width;
			int height = settings.Height > maxHeight
				             ? maxHeight
				             : settings.Height;

			_form.Size = new Size(width, height);
		}

		private void ApplyWindowState(WindowSettings settings)
		{
			// don't restore minimized state
			if (settings.WindowState != FormWindowState.Minimized)
			{
				_form.WindowState = settings.WindowState;
			}
		}

		private void ApplyTopMost(WindowSettings settings)
		{
			_form.TopMost = settings.TopMost;
		}

		private static string GetFileName(Form form, string callingContextID)
		{
			var format = "ws_{0}{1}.xml";
			return string.Format(format, form.GetType().FullName,
			                     callingContextID == null
				                     ? string.Empty
				                     : "_" + callingContextID);
		}

		#endregion
	}
}
