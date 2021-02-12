using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check if neighboring areas with line in between lack certain differences
	/// </summary>
	/// History: 26.10: GKAT initial coding
	[CLSCompliant(false)]
	[Obsolete("Unfinished. Consolidate with qaNeighborAreas, or delete")]
	public class QaLineNeighborAreas : ContainerTest
	{
		private readonly ISpatialFilter[] _filter;
		private readonly QueryFilterHelper[] _helper;

		/// <summary>
		/// Check if neighboring areas with line in between lack certain differences
		/// </summary>
		/// <param name="areaLayer">layer with the two areas</param>
		/// <param name="lineLayer">layer with the line</param>
		public QaLineNeighborAreas(ITable areaLayer, ITable lineLayer)
			: base(new[] {areaLayer, lineLayer, areaLayer})
		{
			_filter = new ISpatialFilter[3];
			_helper = new QueryFilterHelper[3];
			_filter[0] = null;
		}

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			int errorCount = 0;
			if (tableIndex != 0)
			{
				return errorCount;
			}

			if (_filter[0] == null)
			{
				InitFilter();
			}

			IGeometry shape = ((IFeature) row).Shape;
			_filter[1].Geometry = shape;
			_filter[2].Geometry = shape;
			_helper[2].MinimumOID = -1;
			_helper[1].MinimumOID = -1;
			// find all crossing lines complying with filter.WhereClause and throw error

			IEnumerable<IRow> pAreas = Search(InvolvedTables[2], _filter[2], _helper[2]);

			IEnumerable<IRow> pLines = Search(InvolvedTables[1], _filter[1], _helper[1]);
			foreach (IRow area in pAreas)
			{
				var polygon = (IRelationalOperator) ((IFeature) area).Shape;
				foreach (IRow line in pLines)
				{
					if (polygon.Touches(((IFeature) line).Shape))
					{
						//TODO
						const string description = "TODO";
						ReportError(description, _filter[1].Geometry, row, area, line);
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

			_filter[2] = pFilters[2];
			_filter[2].SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;
			_helper[2] = pHelpers[2];
			_filter[1] = pFilters[1];
			_filter[1].SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;
			_helper[1] = pHelpers[1];
		}
	}
}
