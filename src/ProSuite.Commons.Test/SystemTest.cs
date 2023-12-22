using System;
using System.Globalization;
using System.Runtime.Versioning;
using System.Threading;
using Microsoft.Win32;
using NUnit.Framework;
using ProSuite.Commons.Globalization;

namespace ProSuite.Commons.Test
{
	/// <summary>
	/// A container for unit tests (and experiments) that depend on nothing
	/// but .NET (and Windows). For lack of a better idea, it's called
	/// SystemTest (true to the maxim that it's a system if it doesn't have one).
	/// </summary>
#if !NET48
	[SupportedOSPlatform("windows")]
#endif
	[TestFixture]
	public class SystemTest
	{
		[Test]
		public void ReportCurrentCulture()
		{
			Console.WriteLine(@"CurrentCulture:");
			Console.WriteLine(
				CultureInfoUtils.GetCultureInfoDescription(CultureInfo.CurrentCulture));
			Console.WriteLine();
			Console.WriteLine(@"CurrentUICulture:");
			Console.WriteLine(
				CultureInfoUtils.GetCultureInfoDescription(CultureInfo.CurrentUICulture));
			Console.WriteLine();
			Console.WriteLine(@"InstalledUICulture:");
			Console.WriteLine(
				CultureInfoUtils.GetCultureInfoDescription(CultureInfo.InstalledUICulture));
			Console.WriteLine();
			Console.WriteLine(@"InvariantCulture:");
			Console.WriteLine(
				CultureInfoUtils.GetCultureInfoDescription(CultureInfo.InvariantCulture));
		}

		[Test]
		public void UseTempCultureInfo()
		{
			Console.WriteLine(@"CurrentCulture:");
			Console.WriteLine(
				CultureInfoUtils.GetCultureInfoDescription(CultureInfo.CurrentCulture));
			Console.WriteLine();

			const string tempCulture = "uk-UA";
			using (
				new CultureInfoUtils.TempCultureInfo(CultureInfo.GetCultureInfo(tempCulture)))
			{
				CultureInfo ci = Thread.CurrentThread.CurrentCulture;
				Console.WriteLine(@"TemporaryCulture:");
				Console.WriteLine(CultureInfoUtils.GetCultureInfoDescription(ci));
			}

			Console.WriteLine();
			Console.WriteLine(@"CurrentCulture:");
			Console.WriteLine(
				CultureInfoUtils.GetCultureInfoDescription(CultureInfo.CurrentCulture));
		}

		[Test]
		public void ReportEsriUILangID()
		{
			// ArcGIS at version 10 records the user interface language
			// in the registry at HKEY_CURRENT_USER\Software\ESRI\UILANGID
			// as a dword value. If this key is missing, the language is
			// "en-US".

			const string name = "UILANGID";
			const string path = "Software\\ESRI";

			Console.WriteLine(@"Registry lookup: HKEY_CURRENT_USER\{0}\{1}:",
			                  path, name);

			RegistryKey key = Registry.CurrentUser.OpenSubKey(path);

			if (key != null)
			{
				object value = key.GetValue(name);
				if (value != null)
				{
					Console.WriteLine(@"Value is: {0}", value);
				}
				else
				{
					Console.WriteLine(@"No such value: {0} (means English)", name);
				}
			}
			else
			{
				Console.WriteLine(@"No such path: {0} (pre version 10 ArcGIS?)", path);
			}
		}
	}
}
