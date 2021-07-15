using System.Collections.Generic;

namespace ProSuite.DomainModel.Core.DataModel
{
	public interface ISimpleTerrainDataset : ISpatialDataset, IDatasetCollection
	{
		int TerrainId { get; }

		double PointDensity { get; }

		IList<TerrainSourceDataset> Sources { get; }
	}
}
