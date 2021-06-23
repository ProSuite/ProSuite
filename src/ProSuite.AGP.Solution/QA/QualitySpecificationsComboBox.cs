using ProSuite.AGP.QA.ProPlugins;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AGP.Workflow;

namespace ProSuite.AGP.Solution.QA
{
	[UsedImplicitly]
	internal sealed class QualitySpecificationsComboBox : QualitySpecificationsComboBoxBase
	{
		protected override IMapBasedSessionContext SessionContext =>
			ProSuiteToolsModule.Current.SessionContext;
	}
}
