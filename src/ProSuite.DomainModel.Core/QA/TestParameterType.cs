using System;

namespace ProSuite.DomainModel.Core.QA
{
	[Flags]
	public enum TestParameterType
	{
		Unknown = 0,
		CustomScalar = 1,
		String = 2,
		Integer = 4,
		Double = 8,
		DateTime = 16,
		Boolean = 32,
		Dataset = 64,

		NonObjectDataset = 128,
		NonVectorDataset = 256,
		PointDataset = 512,
		MultipointDataset = 1024,
		PolylineDataset = 2048,
		PolygonDataset = 4096,
		MultipatchDataset = 8192,

		VectorDataset = MultipatchDataset | PolygonDataset | PolylineDataset |
		                MultipointDataset | PointDataset,
		ObjectDataset = VectorDataset | NonVectorDataset,
		TableDataset = ObjectDataset | NonObjectDataset,

		TerrainDataset = 16384,
		TopologyDataset = 32768,
		GeometricNetworkDataset = 65536,

		RasterMosaicDataset = 131072,
		RasterDataset = 262144
	}
}
