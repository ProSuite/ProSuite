using System;
using System.Collections.Generic;
using System.ComponentModel;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.Commons.AO.Geometry;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
	[Obsolete("Use QaMinNodeDistance")]
	public class QaMinPointDistance : ContainerTest
	{
		private IEnvelope _box;
		private double _coincident2;
		private IList<ISpatialFilter> _filter;
		private IList<QueryFilterHelper> _helper;
		private bool _is3D;
		private double _searchDistanceSquared;

		/// <summary>
		/// Finds all pair of points in 'layer' closer than 'near'
		/// </summary>
		/// <param name="pointClass">point feature class</param>
		/// <param name="near">minimum point distance in (x,y)-units</param>
		/// <param name="is3D">include z-Component</param>
		[Description("Finds all pair of points in 'layer' closer than 'near'")]
		public QaMinPointDistance(
			[Description("Point feature class")] IFeatureClass pointClass,
			[Description("minimum point distance in (x,y)-units")]
			double near,
			[Description("include z-coordinate for checking")]
			bool is3D)
			:
			base((ITable) pointClass)
		{
			Init(near, is3D);
		}

		/// <summary>
		/// Finds all pair of points 'layers' closer than 'near'
		/// or smaller than the tolerance of the Spatial Reference of the first layer
		/// </summary>
		/// <param name="featureClasses">point feature classes</param>
		/// <param name="near">minimum point distance in (x,y)-units</param>
		/// <param name="is3D">include z-Component</param>
		/// <remarks>Remark: the feature classes in 'layers' must have the same spatial reference</remarks>
		[Description(
			"Finds all pair of points 'layers' closer than 'near'\n" +
			"Remark: the feature classes in 'layers' must have the same spatial reference"
		)]
		public QaMinPointDistance(
			[Description("point feature classes")] IList<IFeatureClass> featureClasses,
			[Description("minimum point distance in (x,y)-units")]
			double near,
			[Description("include z-coordinate for checking")]
			bool is3D)
			: base(CastToTables(featureClasses))
		{
			Init(near, is3D);
		}

		private void Init(double searchDistance, bool is3D)
		{
			SearchDistance = searchDistance;
			_searchDistanceSquared = searchDistance * searchDistance;
			_filter = null;
			_is3D = is3D;

			double coincident = GeometryUtils.GetXyTolerance((IFeatureClass) InvolvedTables[0]);
			_coincident2 = coincident * coincident;
		}

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			// preparing
			int iNError = 0;
			if (_filter == null)
			{
				InitFilter();
			}

			var p0 = (IPoint) ((IFeature) row).Shape;

			// iterating over all needed tables
			int iTable = -1;
			bool bSkip = IgnoreUndirected;

			foreach (IFeatureClass fcNeighbor in InvolvedTables)
			{
				iTable++;
				_helper[iTable].MinimumOID = -1;
				if (row.Table == fcNeighbor)
				{
					bSkip = false;
					if (IgnoreUndirected)
					{
						_helper[iTable].MinimumOID = row.OID;
					}
				}

				if (bSkip)
				{
					continue;
				}

				iNError += ExecutePoint(p0, fcNeighbor, iTable, (IFeature) row);
			}

			return iNError;
		}

		private int ExecutePoint(IPoint point, IFeatureClass neighbor, int iTable,
		                         IFeature row0)
		{
			int iNError = 0;
			ISpatialFilter filter = _filter[iTable];
			_box.PutCoords(point.X - SearchDistance, point.Y - SearchDistance,
			               point.X + SearchDistance, point.Y + SearchDistance);
			filter.Geometry = _box;

			foreach (IFeature rowNeighbor in
				Search((ITable) neighbor, _filter[iTable], _helper[iTable]))
			{
				var p0 = (IPoint) rowNeighbor.Shape;

				iNError += CheckDistance(point, p0, row0, rowNeighbor);
			}

			return iNError;
		}

		private void InitFilter()
		{
			CopyFilters(out _filter, out _helper);
			foreach (ISpatialFilter filter in _filter)
			{
				filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
			}

			_box = new EnvelopeClass();
		}

		private int CheckDistance(IPoint p0, IPoint p1, IFeature row0, IFeature row1)
		{
			if (row0 == row1)
			{
				return 0;
			}

			double d = p0.X - p1.X;
			double d2 = d * d;
			d = p0.Y - p1.Y;
			d2 += d * d;
			if (_is3D)
			{
				d = p0.Z - p1.Z;
				d2 += d * d;
			}

			if (d2 < _searchDistanceSquared &&
			    (row0.OID != row1.OID || row0.Table != row1.Table))
			{
				IGeometry geom;
				if (d2 > _coincident2)
				{
					object o = Type.Missing;
					IPointCollection errorLine = new PolylineClass();
					errorLine.AddPoint(p0, ref o, ref o);
					errorLine.AddPoint(p1, ref o, ref o);

					geom = (IGeometry) errorLine;
				}
				else
				{
					geom = row0.ShapeCopy;
				}

				double dist = Math.Sqrt(d2);
				string description = string.Format("Point distance {0}",
				                                   FormatLengthComparison(dist, "<",
				                                                          SearchDistance,
				                                                          p0.SpatialReference));
				ReportError(description, geom, row0, row1);

				return 1;
			}

			return 0;
		}
	}
}
