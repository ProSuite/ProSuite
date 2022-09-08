using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.IssueFilters
{
	[UsedImplicitly]
	public class IfIntersecting : IssueFilter
	{
		private IList<QueryFilterHelper> _filterHelpers;
		private IList<ISpatialFilter> _spatialFilters;

		[DocIf(nameof(DocIfStrings.IfIntersecting_0))]
		public IfIntersecting(
			[DocIf(nameof(DocIfStrings.IfIntersects_featureClass))]
			IReadOnlyFeatureClass featureClass)
			: base(new[] {featureClass}) { }

		public override bool Check(QaErrorEventArgs error)
		{
			IGeometry errorGeometry = error.QaError.Geometry;

			if (errorGeometry == null || errorGeometry.IsEmpty)
			{
				return false;
			}

			EnsureFilters();

			IReadOnlyTable table = InvolvedTables[0];
			ISpatialFilter filter = _spatialFilters[0];
			QueryFilterHelper helper = _filterHelpers[0];

			filter.Geometry = errorGeometry;
			foreach (var searched in Search(table, filter, helper))
			{
				if (! ((IRelationalOperator) errorGeometry).Disjoint(
					    ((IReadOnlyFeature) searched).Shape))
				{
					return true;
				}
			}

			return false;
		}

		private void EnsureFilters()
		{
			if (_spatialFilters == null)
			{
				CopyFilters(out _spatialFilters, out _filterHelpers);
				_spatialFilters[0].SpatialRel = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
			}
		}
	}
}
