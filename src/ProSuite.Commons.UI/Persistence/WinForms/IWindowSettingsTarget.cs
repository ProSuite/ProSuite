namespace ProSuite.Commons.UI.Persistence.WinForms
{
	/// <summary>
	/// Defines the interface for forms that want to include *internal*
	/// settings (e.g. splitter positions) in the persistable settings
	/// managed by the WindowSettingsManager
	/// </summary>
	public interface IWindowSettingsTarget
	{
		void RestoreSettings(WindowSettings settings);

		void SaveSettings(WindowSettings settings);
	}
}
