using ESRI.ArcGIS.Geometry;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public interface IGeometryTypeConfigurator
	{
		T GetGeometryType<T>() where T : GeometryType;

		GeometryTypeShape GetGeometryType(esriGeometryType esriGeometryType);
	}
}
