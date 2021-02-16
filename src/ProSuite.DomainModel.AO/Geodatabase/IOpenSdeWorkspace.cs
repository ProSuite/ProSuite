using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.Geodatabase
{
	public interface IOpenSdeWorkspace : IOpenWorkspace
	{
		[NotNull]
		[PublicAPI]
		IFeatureWorkspace OpenWorkspace([CanBeNull] string versionName, int hWnd = 0);
	}
}
