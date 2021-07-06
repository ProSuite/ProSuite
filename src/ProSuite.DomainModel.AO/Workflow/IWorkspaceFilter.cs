using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.Workflow
{
	public interface IWorkspaceFilter
	{
		bool Ignore([NotNull] IWorkspace workspace,
		            [NotNull] out string reason);
	}
}
