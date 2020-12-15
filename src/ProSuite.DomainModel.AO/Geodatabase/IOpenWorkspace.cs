using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.Geodatabase
{
	[CLSCompliant(false)]
	public interface IOpenWorkspace
	{
		[NotNull]
		[PublicAPI]
		IFeatureWorkspace OpenWorkspace(int hWnd = 0);
	}
}
