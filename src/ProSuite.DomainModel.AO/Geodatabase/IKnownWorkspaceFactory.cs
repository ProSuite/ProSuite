using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DomainModel.AO.Geodatabase
{
	public interface IKnownWorkspaceFactory
	{
		bool CanOpen([NotNull] ConnectionProvider connectionProvider);

		[NotNull]
		IFeatureWorkspace OpenWorkspace([NotNull] ConnectionProvider connectionProvider,
		                                int hWnd = 0);
	}
}
