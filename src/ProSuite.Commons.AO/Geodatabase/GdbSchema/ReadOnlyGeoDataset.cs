using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public class ReadOnlyGeoDataset : IReadOnlyGeoDataset, IEquatable<ReadOnlyGeoDataset>
	{
		private IGeoDataset Dataset { get; }

		public ReadOnlyGeoDataset([NotNull]IGeoDataset dataset)
		{
			Dataset = dataset;
		}
		ISpatialReference IReadOnlyGeoDataset.SpatialReference => Dataset.SpatialReference;
		IEnvelope IReadOnlyGeoDataset.Extent => Dataset.Extent;

		bool IEquatable<ReadOnlyGeoDataset>.Equals(ReadOnlyGeoDataset other)
		{
			return other?.Dataset == Dataset;
		}
	}
}
