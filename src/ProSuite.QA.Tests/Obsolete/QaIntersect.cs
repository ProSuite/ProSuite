using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Look for an Intersection between two lines that
	/// are on the same layer
	/// </summary>
	[Obsolete("use QaLineIntersect")]
	public class QaIntersect : ContainerTest
	{
		private ISpatialFilter _filter;
		private QueryFilterHelper _helper;

		public QaIntersect(IFeatureClass featureClass)
			: base((ITable) featureClass) { }

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			if (_filter == null)
			{
				InitFilter();
			}

			int errorCount = 0;

			// configure filter to find crossing "row"
			var pLine = (IPolyline) ((IFeature) row).Shape;
			_filter.Geometry = pLine;

			// optimize query if tests runs "directed"
			if (IgnoreUndirected)
			{
				_helper.MinimumOID = row.OID;
			}
			else
			{
				_helper.MinimumOID = -1;
			}

			// find all crossing lines complying with filter.WhereClause and throw error
			IEnumerable<IRow> pIntersects = Search(InvolvedTables[0], _filter, _helper);
			foreach (IRow pRow in pIntersects)
			{
				// TODO: find Intersection co-ordinates
				if (pRow.OID == row.OID)
				{
					var self = (ITopologicalOperator2) ((IFeature) row).Shape;
					self.IsKnownSimple_2 = false;
					if (self.IsSimple == false)
					{
						self = (ITopologicalOperator2) ((IFeature) row).ShapeCopy;
						((IMAware) self).MAware = false;
						self.IsKnownSimple_2 = false;
						self.Simplify();
						var part = (IPath) ((IGeometryCollection) self).get_Geometry(0);
						IGeometry geometry = part.FromPoint;

						const string description = "Self-Intersection";
						ReportError(description, geometry, row);
						errorCount++;
					}
				}
				else
				{
					IGeometry geometry = ((ITopologicalOperator) pLine).Intersect(
						((IFeature) pRow).Shape,
						esriGeometryDimension.esriGeometry0Dimension);
					if (! geometry.IsEmpty)
					{
						const string description = "Intersection";
						ReportError(description, geometry, row, pRow);
						errorCount++;
					}
				}
			}

			return errorCount;
		}

		/// <summary>
		/// create a filter that gets the lines crossing the current row,
		/// with the same attribute constraints as the table
		/// </summary>
		private void InitFilter()
		{
			IList<ISpatialFilter> pFilters;
			IList<QueryFilterHelper> pHelpers;

			// there is one table and hence one filter (see constructor)
			// Create copy of this filter and use it for quering crossing lines
			CopyFilters(out pFilters, out pHelpers);
			_filter = pFilters[0];
			_filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelCrosses;
			_helper = pHelpers[0];
		}
	}
}
