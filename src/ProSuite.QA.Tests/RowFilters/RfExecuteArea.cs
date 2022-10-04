using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.RowFilters
{
	[UsedImplicitly]
	public class RfExecuteArea : RowFilter
	{
		private IList<ISpatialFilter> _spatialFilters;
		private IList<QueryFilterHelper> _filterHelpers;

		public RfExecuteArea(IReadOnlyFeatureClass areaFc)
			: base(CastToTables(areaFc)) { }

		private void EnsureFilters()
		{
			if (_spatialFilters == null)
			{
				CopyFilters(out _spatialFilters, out _filterHelpers);
				_spatialFilters[0].SpatialRel = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
			}
		}

		public override bool VerifyExecute(IReadOnlyRow row)
		{
			if (! (row is IReadOnlyFeature f))
			{
				return true;
			}

			EnsureFilters();

			IReadOnlyTable table = InvolvedTables[0];
			ISpatialFilter filter = _spatialFilters[0];
			QueryFilterHelper helper = _filterHelpers[0];
			IGeometry searchGeom = f.Shape;
			filter.Geometry = searchGeom;
			foreach (var searched in Search(table, filter, helper))
			{
				if (((IRelationalOperator) searchGeom).Within(((IReadOnlyFeature) searched).Shape))
				{
					return true;
				}
			}

			return false;
		}
	}
}
