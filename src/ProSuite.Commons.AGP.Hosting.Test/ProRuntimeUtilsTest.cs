using System;
using System.Threading;
using Microsoft.Win32;
using NUnit.Framework;

namespace ProSuite.Commons.AGP.Hosting.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class ProRuntimeUtilsTest
	{
		[Test]
		public void CanCheckIsProInstalled()
		{
			bool isProInstalled = ProRuntimeUtils.IsProInstalled(out Version proVersion);

			RegistryKey versionSubKey =
				Registry.LocalMachine.OpenSubKey(@"SOFTWARE\ESRI\ArcGISPro", false);
			bool versionSubKeyExists = versionSubKey != null;

			Assert.AreEqual(versionSubKeyExists, isProInstalled);

			bool isServerInstalled = ProRuntimeUtils.IsServerInstalled(out Version serverVersion);

			Assert.IsTrue(isProInstalled || isServerInstalled);

			Console.WriteLine($"ArcGIS Pro Version: {proVersion}");
			Console.WriteLine($"ArcGIS Server Version: {serverVersion}");
		}

		[Test]
		public void CanGetInstallDirs()
		{
			string installDirPro = ProRuntimeUtils.GetProInstallDir();
			string installDirServer = ProRuntimeUtils.GetServerInstallDir();

			Assert.IsTrue(installDirPro != null || installDirServer != null);

			Console.WriteLine($"ArcGIS Pro InstallDir: {installDirPro}");
			Console.WriteLine($"ArcGIS Server InstallDir: {installDirServer}");
		}
	}
}
