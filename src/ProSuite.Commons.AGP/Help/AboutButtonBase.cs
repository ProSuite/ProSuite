using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AGP.Help;

public abstract class AboutButtonBase : ButtonCommandBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private readonly string _caption;

	protected AboutButtonBase([NotNull] string caption)
	{
		if (string.IsNullOrWhiteSpace(caption))
			throw new ArgumentNullException(nameof(caption));

		_caption = caption;
	}

	protected override Task<bool> OnClickCore()
	{
		var configFileSearcher = GetConfigFileSearcher();

		var items = new List<AboutItem>();
		CollectInformation(items, configFileSearcher);

		var message = AboutItem.GetPlainText(items);

		_msg.Info(message);

		Gateway.ShowDialog<AboutWindow>(_caption, items);

		return Task.FromResult(true);
	}

	[CanBeNull]
	protected abstract string GetConfigDirEnvVar();

	[CanBeNull]
	protected abstract IConfigFileSearcher GetConfigFileSearcher();

	private void CollectInformation([NotNull] ICollection<AboutItem> items,
	                                [CanBeNull] IConfigFileSearcher configFileSearcher)
	{
		if (items is null) throw new ArgumentNullException(nameof(items));

		string currentSection = AboutItem.AddinSection;

		try
		{
			var assembly = Assembly.GetExecutingAssembly();
			var version = assembly.GetName().Version;

			Add(items, currentSection, "Addin Version", Convert.ToString(version), "reflected");

			var proVersion = Assembly.GetEntryAssembly()?.GetName().Version;

			Add(items, currentSection, "ArcGIS Pro Version", Convert.ToString(proVersion),
			    "reflected");
		}
		catch (Exception ex)
		{
			Add(items, currentSection, "Error", ex.Message, "version retrieval failed");
		}

		currentSection = AboutItem.ConfigSection;

		try
		{
			string configDirEnvVar = GetConfigDirEnvVar();

			if (configDirEnvVar == null)
			{
				Add(items, currentSection, "ConfigDirEnvVar", string.Empty,
				    "config dir env var undefined");
			}
			else
			{
				string configDir = Environment.GetEnvironmentVariable(configDirEnvVar);

				Add(items, currentSection, $"{configDirEnvVar}",
				    configDir ?? "environment variable is not defined", "env var");
			}

			var searchPaths = configFileSearcher?.GetSearchPaths().ToList();
			if (searchPaths is not null)
			{
				int count = searchPaths.Count;
				var remarks = new[] { "tried first", "tried second", "etc." };
				Add(items, currentSection, "Config Search Path",
				    $"{count} entr{(count == 1 ? "y" : "ies")}");
				for (int i = 0; i < count; i++)
				{
					string path = searchPaths[i];
					var remark = i < remarks.Length ? remarks[i] : null;
					Add(items, currentSection, "-", path, remark);
				}
			}
		}
		catch (Exception ex)
		{
			Add(items, currentSection, "Error", ex.Message, "error getting config info");
		}

		currentSection = AboutItem.ProcessSection;

		try
		{
			Add(items, currentSection, "Process ID", Convert.ToString(Environment.ProcessId));
			Add(items, currentSection, "Command Line", Environment.CommandLine);
			Add(items, currentSection, "Machine Name", Environment.MachineName);
			Add(items, currentSection, "User Name", Environment.UserName);
			Add(items, currentSection, "Current Directory", Environment.CurrentDirectory);
			Add(items, currentSection, "System Directory", Environment.SystemDirectory);
		}
		catch (Exception ex)
		{
			Add(items, currentSection, "Error", ex.Message);
		}

		currentSection = AboutItem.RuntimeSection;

		try
		{
			var framework = RuntimeInformation.FrameworkDescription; // e.g. .NET 6.0.27
			var runtimeIdentifier = RuntimeInformation.RuntimeIdentifier; // e.g. win10-x64
			var processArch = RuntimeInformation.ProcessArchitecture; // X86 or X64 or Arm ...
			var osName = RuntimeInformation.OSDescription; // e.g. Microsoft Windows 10.0.19045
			var osArch = RuntimeInformation.OSArchitecture; // X86 or X64 or Arm or ...

			Add(items, currentSection, "Framework", framework);
			Add(items, currentSection, "Runtime Identifier", runtimeIdentifier);
			Add(items, currentSection, "Process Architecture", Convert.ToString(processArch));
			Add(items, currentSection, "Operating System", osName);
			Add(items, currentSection, "OS Architecture", Convert.ToString(osArch));
		}
		catch (Exception ex)
		{
			Add(items, currentSection, "Error", ex.Message);
		}

		//try
		//{
		//	GetAllAddinInfos(items);
		//}
		//catch (Exception ex)
		//{
		//	Add(items, "All Addins", "Error", ex.Message, "getting all Addin infos failed");
		//}
	}

	private static void GetAllAddinInfos(ICollection<AboutItem> items)
	{
		if (items is null) return;

		var infos = FrameworkApplication.GetAddInInfos();

		foreach (var info in infos)
		{
			string currentSection = $"Addin {info.ID}";

			Add(items, currentSection, "Name", info.Name);
			Add(items, currentSection, "Description", info.Description);
			Add(items, currentSection, "ImagePath", info.ImagePath);
			Add(items, currentSection, "Author", info.Author);
			Add(items, currentSection, "Company", info.Company);
			Add(items, currentSection, "Date", info.Date);
			Add(items, currentSection, "Version", info.Version);
			Add(items, currentSection, "FullPath", info.FullPath);
			Add(items, currentSection, "DigitalSignature", info.DigitalSignature);
			Add(items, currentSection, "IsCompatible", info.IsCompatible.ToString());
			Add(items, currentSection, "IsDeleted", info.IsDeleted.ToString());
			Add(items, currentSection, "TargetVersion", info.TargetVersion);
			Add(items, currentSection, "ErrorMsg", info.ErrorMsg);
			Add(items, currentSection, "ID", info.ID);
		}
	}

	private static void Add(ICollection<AboutItem> result, string section,
	                        string key, string value, string remark = null)
	{
		if (result is null) return;
		if (string.IsNullOrEmpty(key)) return;
		result.Add(new AboutItem(section, key, value, remark));
	}
}

public class AboutItem
{
	public string Section { get; }
	public string Key { get; }
	public string Value { get; }
	public string Remark { get; }

	public static readonly string AddinSection = "Addin";
	public static readonly string ConfigSection = "Config";
	public static readonly string ProcessSection = "Process";
	public static readonly string RuntimeSection = "Runtime";

	public AboutItem(string section, string key, string value, string remark = null)
	{
		Section = section; // can be null
		Key = key ?? throw new ArgumentNullException(nameof(key));
		Value = value ?? string.Empty;
		Remark = remark;
	}

	public static int GetSectionOrder(string section)
	{
		if (string.IsNullOrEmpty(section))
			return 0;
		if (string.Equals(section, AddinSection, StringComparison.OrdinalIgnoreCase))
			return 1;
		if (string.Equals(section, ConfigSection, StringComparison.OrdinalIgnoreCase))
			return 2;
		if (string.Equals(section, ProcessSection, StringComparison.OrdinalIgnoreCase))
			return 3;
		if (string.Equals(section, RuntimeSection, StringComparison.OrdinalIgnoreCase))
			return 4;

		return 999;
	}

	public static string GetPlainText(IEnumerable<AboutItem> items)
	{
		if (items is null)
		{
			return string.Empty;
		}

		string lastSection = null;
		var buffer = new StringBuilder();

		foreach (var group in items.GroupBy(item => item.Section)
		                           .OrderBy(g => GetSectionOrder(g.Key)))
		{
			if (group.Key != lastSection)
			{
				buffer.AppendLine().AppendLine(group.Key);
				lastSection = group.Key;
			}

			foreach (var item in group)
			{
				buffer.Append($"{item.Key}: {item.Value}");

				if (! string.IsNullOrEmpty(item.Remark))
				{
					buffer.Append($" ({item.Remark})");
				}

				buffer.AppendLine();
			}
		}

		return buffer.Trim().ToString();
	}
}
