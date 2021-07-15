using ESRI.ArcGIS.Geodatabase;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public interface ITerrainSoure
	{
		IVectorDataset Dataset { get; }
		esriTinSurfaceType Type { get; }
	}
}
