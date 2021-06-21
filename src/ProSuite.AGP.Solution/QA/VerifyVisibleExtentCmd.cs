using System.Windows;
using ArcGIS.Desktop.Framework.Controls;
using ProSuite.AGP.QA.ProPlugins;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.UI.QA.VerificationProgress;

namespace ProSuite.AGP.Solution.QA
{
	internal class VerifyVisibleExtentCmd : VerifyVisibleExtentCmdBase
	{
		protected override IQualityVerificationEnvironment QualityVerificationEnvironment =>
			ProSuiteToolsModule.Current.QualityVerificationEnvironment;

		protected override Window CreateProgressWindow(
			VerificationProgressViewModel progressViewModel)
		{
			ProWindow window = new VerificationProgressWindow(progressViewModel);

			window.Owner = System.Windows.Application.Current.MainWindow;

			return window;
		}

		protected override string HtmlReportName => Constants.HtmlReportName;
	}
}
