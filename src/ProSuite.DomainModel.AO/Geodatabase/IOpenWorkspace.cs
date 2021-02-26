using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.Geodatabase
{
	public interface IOpenWorkspace
	{
		[NotNull]
		[PublicAPI]
		IFeatureWorkspace OpenWorkspace(int hWnd = 0);
	}
}
