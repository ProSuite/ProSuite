using System.Collections.Generic;

namespace ProSuite.DomainModel.Core.DataModel
{
	public interface ISimpleTerrainDataset : ISpatialDataset, IDatasetCollection
	{
		double PointDensity { get; }

		IReadOnlyList<TerrainSourceDataset> Sources { get; }
	}
}
