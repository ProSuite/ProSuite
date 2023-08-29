using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Test.Transformer
{
	internal class IgnoreErrorArea : IssueFilter
	{
		private IList<IFeatureClassFilter> _spatialFilters;
		private IList<QueryFilterHelper> _filterHelpers;

		public IgnoreErrorArea(IReadOnlyFeatureClass areaFc)
			: base(CastToTables(areaFc)) { }

		private void EnsureFilters()
		{
			if (_spatialFilters == null)
			{
				CopyFilters(out _spatialFilters, out _filterHelpers);
			}
		}

		public override bool Check(QaErrorEventArgs args)
		{
			EnsureFilters();
			QaError error = args.QaError;
			IReadOnlyTable table = InvolvedTables[0];
			IFeatureClassFilter filter = _spatialFilters[0];
			QueryFilterHelper helper = _filterHelpers[0];
			filter.FilterGeometry = error.Geometry;
			foreach (var row in Search(table, filter, helper))
			{
				IGeometry ignoreGeom = ((IFeature) row).Shape;
				if (((IRelationalOperator2) ignoreGeom).ContainsEx(
					    error.Geometry, esriSpatialRelationExEnum.esriSpatialRelationExBoundary))
				{
					return true;
				}
			}

			return false;
		}
	}
}
