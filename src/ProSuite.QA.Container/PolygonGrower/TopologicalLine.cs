using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.QA.Container.PolygonGrower
{
	public class TopologicalLine : ITopologicalLine
	{
		[CanBeNull] private IPoint _fromPoint;
		[CanBeNull] private IPoint _toPoint;

		private double _fromAngle;
		private bool _fromAngleKnown;

		private IRow _leftCentroid;
		private LineListPolygon _leftPoly;

		private int _maxCode;
		private IRow _rightCentroid;
		private LineListPolygon _rightPoly;
		private double _toAngle;
		private bool _toAngleKnown;
		private double _yMax;
		private double _resolution = -1;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="TopologicalLine"/> class.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="partIndex">part index of row.Shape, -1: full line</param>
		[CLSCompliant(false)]
		public TopologicalLine([NotNull] TableIndexRow row, int partIndex)
			: this(row, (IPolyline) ((IFeature) row.Row).Shape, partIndex)
		{
			Assert.ArgumentNotNull(row, nameof(row));
		}

		[CLSCompliant(false)]
		public TopologicalLine([NotNull] TableIndexRow row,
		                       [NotNull] IPolyline line,
		                       int partIndex)
		{
			Assert.ArgumentNotNull(row, nameof(row));
			Assert.ArgumentNotNull(line, nameof(line));

			Row = row;
			FullLine = line;
			PartIndex = partIndex;
		}

		#endregion

		[NotNull]
		public TableIndexRow Row { get; }

		public int PartIndex { get; }

		[CLSCompliant(false)]
		[NotNull]
		public IPolyline FullLine { get; }

		[CLSCompliant(false)]
		[NotNull]
		public ICurve Path
		{
			get
			{
				if (PartIndex < 0)
				{
					return FullLine;
				}

				var geoms = (IGeometryCollection) FullLine;
				if (geoms.GeometryCount <= 1)
				{
					return FullLine;
				}

				IGeometry part = geoms.Geometry[PartIndex];
				return (IPath) part;
			}
		}

		[CLSCompliant(false)]
		public IPoint FromPoint => _fromPoint ?? (_fromPoint = Path.FromPoint);

		[CLSCompliant(false)]
		public IPoint ToPoint => _toPoint ?? (_toPoint = Path.ToPoint);

		public double FromAngle
		{
			get
			{
				if (! _fromAngleKnown)
				{
					double angle;
					if (TopologicalLineUtils.CalculateFromAngle(
						(ISegmentCollection) Path, out angle)
					)
					{
						_fromAngle = angle;
						_fromAngleKnown = true;
					}
					else
					{
						_fromAngle = double.NaN;
						_fromAngleKnown = true;
						//// TODO handle "undefined angle -> orientation" in test (special error)
						//Assert.Fail("No distinct points found on {0}, unable to calculate from angle",
						//            GdbObjectUtils.ToString(Row.Row));
					}
				}

				return _fromAngle;
			}
		}

		public double ToAngle
		{
			get
			{
				if (! _toAngleKnown)
				{
					double angle;
					if (TopologicalLineUtils.CalculateToAngle((ISegmentCollection) Path, out angle))
					{
						_toAngle = angle;
						_toAngleKnown = true;
					}
					else
					{
						_toAngle = double.NaN;
						_toAngleKnown = true;

						// TODO handle "undefined angle -> orientation" in test (special error)
						//Assert.Fail("No distinct points found on {0}, unable to calculate to angle",
						//            GdbObjectUtils.ToString(Row.Row));
					}
				}

				return _toAngle;
			}
		}

		double ITopologicalLine.Length => Path.Length;

		IPolyline ITopologicalLine.GetLine()
		{
			if (PartIndex < 0)
			{
				return FullLine;
			}

			if (((IGeometryCollection) FullLine).GeometryCount <= 1)
			{
				return FullLine;
			}

			IPolyline line = QaGeometryUtils.CreatePolyline(Path);
			((ISegmentCollection) line).AddSegmentCollection(
				(ISegmentCollection) GeometryFactory.Clone(Path));
			return line;
		}

		ICurve ITopologicalLine.GetPath()
		{
			return Path;
		}

		public LineListPolygon LeftPoly
		{
			get { return _leftPoly; }
			internal set { _leftPoly = value; }
		}

		[CLSCompliant(false)]
		public IRow LeftCentroid
		{
			get { return _leftCentroid; }
			internal set { _leftCentroid = value; }
		}

		public LineListPolygon RightPoly
		{
			get { return _rightPoly; }
			internal set { _rightPoly = value; }
		}

		[CLSCompliant(false)]
		public IRow RightCentroid
		{
			get { return _rightCentroid; }
			internal set { _rightCentroid = value; }
		}

		[CLSCompliant(false)]
		public void QueryFromPoint([NotNull] IPoint queryPoint)
		{
			Path.QueryFromPoint(queryPoint);
		}

		[CLSCompliant(false)]
		public void QueryToPoint([NotNull] IPoint queryPoint)
		{
			Path.QueryToPoint(queryPoint);
		}

		/// <summary>
		/// return the y-coordinat of a point p where p.X == this.Extent.XMax
		/// </summary>
		/// <returns></returns>
		internal double YMax()
		{
			if (_maxCode == 0)
			{
				_maxCode = TopologicalLineUtils.CalculateOrientation(
					(ISegmentCollection) Path, Resolution, out _yMax);
			}

			return _yMax;
		}

		private double Resolution
		{
			get
			{
				if (_resolution < 0)
				{
					_resolution = GetResolution(Path);
				}

				return _resolution;
			}
		}

		public int Orientation
		{
			get
			{
				if (_maxCode == 0)
				{
					_maxCode = TopologicalLineUtils.CalculateOrientation(
						(ISegmentCollection) Path, Resolution, out _yMax);
				}

				return _maxCode;
			}
		}

		public void ClearPoly()
		{
			_rightPoly = null;
			_leftPoly = null;
		}

		private static double GetResolution([NotNull] ICurve polyline)
		{
			Assert.ArgumentNotNull(polyline, nameof(polyline));

			ISpatialReference spatialReference = polyline.SpatialReference;

			Assert.NotNull(spatialReference, "spatialReference");

			const bool standardUnits = true;
			return
				((ISpatialReferenceResolution) spatialReference).XYResolution[standardUnits] /
				2.0;
		}
	}
}