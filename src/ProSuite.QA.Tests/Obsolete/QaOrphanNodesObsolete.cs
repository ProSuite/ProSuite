using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check if there are orphan nodes by consulting several line layers
	/// </summary>
	[Obsolete("Use QaNetOrphanNodes (QaNetOrphanNodes will be renamed to QaOrphanNodes)")
	]
	[CLSCompliant(false)]
	public class QaOrphanNodesObsolete : ContainerTest
	{
		private readonly int _nLayers;
		private readonly int _nPointLayers;
		private ISpatialFilter[] _filter;
		private QueryFilterHelper[] _helper;

		/// <summary>
		/// Check if any point in pointLayer is not From or To-Point of any line in lineLayers
		/// </summary>
		/// <param name="pointClasses">point layer</param>
		/// <param name="polylineClasses">polyLine layers</param>
		/// <remarks>All layers must have the same spatial reference</remarks>
		[Doc("QaOrphanNode_0")]
		public QaOrphanNodesObsolete(
			[Doc("QaOrphanNode_pointClasses")] IList<IFeatureClass> pointClasses,
			[Doc("QaOrphanNode_polylineClasses")] IList<IFeatureClass> polylineClasses)
			: base(CastToTables(pointClasses, polylineClasses))
		{
			_nPointLayers = pointClasses.Count;
			_nLayers = _nPointLayers + polylineClasses.Count;
		}

		/// <summary>
		/// Check if any point in pointLayer is not From or To-Point of any line in lineLayer
		/// </summary>
		/// <param name="pointClass">point layer</param>
		/// <param name="polylineClass">polyLine layer</param>
		/// <remarks>All layers must have the same spatial reference</remarks>
		[Doc("QaOrphanNodes_1")]
		public QaOrphanNodesObsolete(
			[Doc("QaOrphanNode_pointClass")] IFeatureClass pointClass,
			[Doc("QaOrphanNode_polylineClass")] IFeatureClass polylineClass)
			: base(new[] {(ITable) pointClass, (ITable) polylineClass})
		{
			_nPointLayers = 1;
			_nLayers = 2;
		}

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			int errorCount = 0;

			if (tableIndex >= _nPointLayers)
			{
				return errorCount;
			}

			IGeometry shape = ((IFeature) row).Shape;

			if (_filter == null)
			{
				InitFilter();
			}

			bool dangling = true;
			for (int i = _nPointLayers; i < _nLayers; i++)
			{
				_filter[i].Geometry = shape;
				// find all crossing lines complying with filter.WhereClause and throw error
				IEnumerable<IRow> pIntersects = Search(InvolvedTables[i], _filter[i], _helper[i]);
				if (pIntersects.GetEnumerator().MoveNext())
				{
					dangling = false;
					break;
				}
			}

			if (dangling)
			{
				const string description = "Node not connected";
				ReportError(description, shape, row);
				errorCount++;
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
			_filter = new ISpatialFilter[_nLayers];
			_helper = new QueryFilterHelper[_nLayers];
			// there is one table and hence one filter (see constructor)
			// Create copy of this filter and use it for quering crossing lines
			CopyFilters(out pFilters, out pHelpers);
			for (int i = 0; i < _nLayers; i++)
			{
				_filter[i] = pFilters[i];
				_helper[i] = pHelpers[i];
				_filter[i].SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;
			}
		}
	}
}
