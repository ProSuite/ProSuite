using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public class InMemoryDataset : BackingDataset
	{
		private readonly ITable _schema;
		private IEnvelope _extent;

		public InMemoryDataset(ITable schema,
		                       IList<IRow> allRows)
		{
			_schema = schema;
			AllRows = allRows;
		}

		public IList<IRow> AllRows { get; }

		public override IEnvelope Extent
		{
			get
			{
				if (_extent == null)
				{
					_extent = GetExtent();
				}

				return _extent;
			}
		}

		public override IRow GetRow(int id)
		{
			return AllRows.First(r => r.OID == id);

			// TODO: Throw com exception with error code e.ErrorCode = (int)fdoError.FDO_E_ROW_NOT_FOUND
		}

		public override int GetRowCount(IQueryFilter filter)
		{
			return Search(filter, true).Count();
		}

		public override IEnumerable<IRow> Search(IQueryFilter filter, bool recycling)
		{
			var filterHelper = FilterHelper.Create(_schema, filter.WhereClause);

			ISpatialFilter spatialFilter = filter as ISpatialFilter;

			bool hasSpatialFilter = spatialFilter?.Geometry != null;

			return AllRows.Where(r => filterHelper.Check(r) &&
			                          (! hasSpatialFilter || CheckSpatial(r, spatialFilter)));
		}

		private IEnvelope GetExtent()
		{
			if (! (_schema is IFeatureClass featureClass))
			{
				return null;
			}

			var result = new EnvelopeClass
			             {
				             SpatialReference = DatasetUtils.GetSpatialReference(featureClass)
			             };

			foreach (IRow row in AllRows)
			{
				IFeature feature = (IFeature) row;
				result.Union(feature.Extent);
			}

			return result;
		}

		private bool CheckSpatial(IRow row, ISpatialFilter spatialFilter)
		{
			// TODO: Do it properly, see TileCache
			IFeature feature = row as IFeature;

			if (feature == null)
			{
				return false;
			}

			IGeometry searchGeometry = spatialFilter.Geometry;

			return GeometryUtils.Intersects(searchGeometry, feature.Shape);
		}
	}
}
