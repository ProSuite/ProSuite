using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.Workflow.WorkspaceFilters
{
	public interface IWorkspaceMatchCriterion
	{
		bool IsSatisfied([NotNull] IWorkspace workspace,
		                 [NotNull] out string reason);
	}
}
