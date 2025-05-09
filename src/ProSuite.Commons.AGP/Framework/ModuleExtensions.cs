using System;
using System.Globalization;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Framework;

public static class ModuleExtensions
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	/// <summary>
	/// Get the named project setting, converting it to a Boolean;
	/// return null if there is no such setting (or the conversion
	/// to Boolean fails). All parameters may be null.
	/// </summary>
	public static bool? GetFlag(this ModuleSettingsReader settings, string name)
	{
		if (settings == null || name == null) return null;

		var setting = settings.Get(name);
		if (setting is null) return null;
		if (setting is bool flag) return flag;

		try
		{
			var text = Convert.ToString(setting);
			if (string.Equals(text, "true", StringComparison.OrdinalIgnoreCase) ||
			    string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase) ||
			    string.Equals(text, "on", StringComparison.OrdinalIgnoreCase)) return true;
			if (string.Equals(text, "false", StringComparison.OrdinalIgnoreCase) ||
			    string.Equals(text, "no", StringComparison.OrdinalIgnoreCase) ||
			    string.Equals(text, "off", StringComparison.OrdinalIgnoreCase)) return false;
			return null;
		}
		catch (Exception ex)
		{
			_msg.Error(
				$"Cannot convert project setting {name} to {nameof(Boolean)}: {ex.Message}; assuming null");
			return null;
		}
	}

	public static void Add(this ModuleSettingsWriter settings, string name, bool flag)
	{
		if (settings is null)
			throw new ArgumentNullException(nameof(settings));
		if (name is null)
			throw new ArgumentNullException(nameof(name));

		var text = flag.ToString(CultureInfo.InvariantCulture);
		settings.Add(name, text);
	}

	public static double? GetDouble(this ModuleSettingsReader settings, string name)
	{
		if (settings is null || name is null) return null;

		var setting = settings.Get(name);
		if (setting is null) return null;
		if (setting is double value) return value;

		try
		{
			var text = Convert.ToString(setting);

			bool canParse = double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture,
			                                out value);

			return canParse ? value : null;
		}
		catch (Exception ex)
		{
			_msg.Error($"Cannot convert project setting {name} to {nameof(Double)}: {ex.Message}");
			return null;
		}
	}

	public static void Add(this ModuleSettingsWriter settings, string name, double value)
	{
		if (settings is null)
			throw new ArgumentNullException(nameof(settings));
		if (name is null)
			throw new ArgumentNullException(nameof(name));

		var text = value.ToString(CultureInfo.InvariantCulture);
		settings.Add(name, text);
	}

	/// <summary>
	/// Get the named project setting, converting it to a String;
	/// return null if there is no such setting (or the conversion
	/// to String fails). All parameters may be null.
	/// </summary>
	public static string GetString(this ModuleSettingsReader settings, string name)
	{
		if (settings == null || name == null) return null;

		var setting = settings.Get(name);
		if (setting == null) return null;
		if (setting is string text) return text;

		try
		{
			return Convert.ToString(setting);
		}
		catch (Exception ex)
		{
			_msg.Error(
				$"Cannot convert project setting {name} to {nameof(String)}: {ex.Message}; assuming null");
			return null;
		}
	}

	//private static Uri GetUri(ModuleSettingsReader settings, string name)
	//{
	//	if (settings is null || name is null) return null;

	//	var setting = settings.Get(name);
	//	if (setting is null) return null;
	//	if (setting is Uri uri) return uri;

	//	try
	//	{
	//		var text = Convert.ToString(setting);
	//		return text is null ? null : new Uri(text);
	//	}
	//	catch
	//	{
	//		_msg.Error(
	//			$"Cannot convert project setting {name} to {nameof(Uri)}; assuming null");
	//		return null;
	//	}
	//}
}
