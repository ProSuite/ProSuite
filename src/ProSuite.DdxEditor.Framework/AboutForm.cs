using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Globalization;

namespace ProSuite.DdxEditor.Framework
{
	internal partial class AboutForm : Form
	{
		public AboutForm([NotNull] string clientName)
		{
			Assert.ArgumentNotNullOrEmpty(clientName, nameof(clientName));

			InitializeComponent();

			_labelHeader.Text = clientName;
			Text = string.Format("About {0}", clientName);
		}

		private void AboutBoxView_Load(object sender, EventArgs e)
		{
			using (Process process = Process.GetCurrentProcess())
			{
				_textBoxInfo.Text = GetDescription(process);
			}
		}

		[NotNull]
		private static string GetDescription([NotNull] Process process)
		{
			ProcessModule mainExe = process.MainModule;
			FileVersionInfo mainExeVersion = mainExe?.FileVersionInfo;
			string version = mainExeVersion?.FileVersion ?? "n/a";

			var sb = new StringBuilder();

			sb.AppendFormat("Version: {0}", version);

			sb.AppendLine();
			sb.AppendLine();
			sb.AppendFormat("Operating System:    {0}",
			                Environment.OSVersion.VersionString);
			sb.AppendLine();
			sb.AppendFormat(".NET Runtime:        {0}", Environment.Version);
			sb.AppendLine();
			sb.AppendLine();
			sb.AppendFormat("Machine Name:        {0}", Environment.MachineName);
			sb.AppendLine();
			sb.AppendFormat("Processors:          {0}", Environment.ProcessorCount);
			sb.AppendLine();
			sb.AppendLine();
			sb.AppendFormat("User logged in as:   {0}\\{1}", Environment.UserDomainName,
			                Environment.UserName);
			sb.AppendLine();

			if (! Equals(Environment.UserName, EnvironmentUtils.UserDisplayName))
			{
				sb.AppendFormat("User name:           {0}", EnvironmentUtils.UserDisplayName);
				sb.AppendLine();
			}

			sb.AppendLine();
			sb.AppendLine("Regional Settings:");
			sb.AppendLine();
			CultureInfo culture = CultureInfo.CurrentCulture;
			sb.AppendFormat("CurrentCulture:      {0}", culture.EnglishName);
			sb.AppendLine();
			sb.AppendFormat(CultureInfoUtils.GetCultureInfoDescription(culture));
			sb.AppendLine();
			sb.AppendLine();
			culture = CultureInfo.CurrentUICulture;
			sb.AppendFormat("CurrentUICulture:    {0}", culture.EnglishName);
			sb.AppendLine();
			sb.AppendFormat(CultureInfoUtils.GetCultureInfoDescription(culture));
			sb.AppendLine();
			sb.AppendLine();

			sb.AppendFormat("Current Directory:   {0}", Environment.CurrentDirectory);
			sb.AppendLine();
			sb.AppendLine();

			long virtualBytes;
			long privateBytes;
			long workingSet;
			ProcessUtils.GetMemorySize(process, out virtualBytes, out privateBytes,
			                           out workingSet);

			const int mb = 1024 * 1024;
			sb.AppendFormat("Virtual Bytes:       {0:N0} Mb", virtualBytes / mb);
			sb.AppendLine();
			sb.AppendFormat("Private Bytes:       {0:N0} Mb", privateBytes / mb);
			sb.AppendLine();
			sb.AppendFormat("Working Set:         {0:N0} Mb", workingSet / mb);

			sb.AppendLine();
			sb.AppendLine();
			sb.AppendLine();

			sb.AppendLine("Machine environment variables:");
			sb.AppendLine();

			foreach (KeyValuePair<string, string> pair in
			         EnvironmentUtils.GetEnvironmentVariables(EnvironmentVariableTarget.Machine))
			{
				sb.AppendFormat("- {0}: {1}", pair.Key, pair.Value);
				sb.AppendLine();
			}

			sb.AppendLine();
			sb.AppendLine();
			sb.AppendLine("User environment variables:");
			sb.AppendLine();

			foreach (KeyValuePair<string, string> pair in
			         EnvironmentUtils.GetEnvironmentVariables(EnvironmentVariableTarget.User))
			{
				sb.AppendFormat("- {0}: {1}", pair.Key, pair.Value);
				sb.AppendLine();
			}

			return sb.ToString();
		}

		private void _buttonClose_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void _buttonCopy_Click(object sender, EventArgs e)
		{
			Clipboard.SetText(_textBoxInfo.Text);

			_toolStripStatusLabel.Text = "Description copied";
		}
	}
}
