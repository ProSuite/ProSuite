using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.GeoDb;

namespace ProSuite.QA.Container.TestContainer
{
	public class UncachedDataContainer : IDataContainer
	{
		private readonly IEnvelope _envelope;

		public UncachedDataContainer(IEnvelope envelope)
		{
			_envelope = envelope;

			WksGeometryUtils.GetWksEnvelope(_envelope);
		}

		public WKSEnvelope CurrentTileExtent =>
			WksGeometryUtils.GetWksEnvelope(Assert.NotNull(_envelope));

		public IEnvelope GetLoadedExtent(IReadOnlyTable table)
		{
			return _envelope ?? ((IReadOnlyFeatureClass) table)?.Extent;
		}

		public double GetSearchTolerance(IReadOnlyTable table)
		{
			throw new NotImplementedException();
		}

		public ISimpleSurface GetSimpleSurface(RasterReference rasterReference, IEnvelope envelope,
		                                       double? defaultValueForUnassignedZs = null,
		                                       UnassignedZValueHandling? unassignedZValueHandling =
			                                       null)
		{
			return rasterReference.CreateSurface(envelope, defaultValueForUnassignedZs,
			                                     unassignedZValueHandling);
		}

		public IEnumerable<IReadOnlyRow> Search(IReadOnlyTable table,
		                                        ITableFilter queryFilter,
		                                        QueryFilterHelper filterHelper)
		{
			return table.EnumRows(queryFilter, false);
		}

		public IUniqueIdProvider GetUniqueIdProvider(IReadOnlyTable table) => null;

		public IEnumerable<Tile> EnumInvolvedTiles(IGeometry geometry)
		{
			throw new NotImplementedException();
		}
	}
}
