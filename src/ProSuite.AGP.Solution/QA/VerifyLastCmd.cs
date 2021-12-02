using System.Windows;
using ArcGIS.Desktop.Framework.Controls;
using ProSuite.AGP.QA.ProPlugins;
using ProSuite.Commons.AGP;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.UI.QA.VerificationProgress;

namespace ProSuite.AGP.Solution.QA
{
	[UsedImplicitly]
	internal class VerifyLastCmd : VerifyLastCmdBase
	{
		protected override IMapBasedSessionContext SessionContext =>
			ProSuiteToolsModule.Current.SessionContext;

		protected override IProSuiteFacade ProSuiteImpl => ProSuiteUtils.Facade;

		protected override Window CreateProgressWindow(
			VerificationProgressViewModel progressViewModel)
		{
			ProWindow window = new VerificationProgressWindow(progressViewModel);

			window.Owner = System.Windows.Application.Current.MainWindow;

			return window;
		}
	}
}
