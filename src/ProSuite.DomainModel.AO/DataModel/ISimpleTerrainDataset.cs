using System;
using System.Collections.Generic;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public interface ISimpleTerrainDataset : ISpatialDataset, IFeatureDatasetElement
	{
		int TerrainDefId { get; }
		IList<ITerrainSoure> Sources { get; }
	}
}
