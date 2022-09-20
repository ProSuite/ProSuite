using ProSuite.AGP.QA.ProPlugins;
using ProSuite.Commons.AGP;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AGP.Workflow;

namespace ProSuite.AGP.Solution.QA
{
	[UsedImplicitly]
	internal class VerifySelectionCmd : VerifySelectionCmdBase
	{
		protected override IMapBasedSessionContext SessionContext =>
			ProSuiteToolsModule.Current.SessionContext;

		protected override IProSuiteFacade ProSuiteImpl => ProSuiteUtils.Facade;
	}
}
