using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	public class Line3D : IEquatable<Line3D>, IBoundedXY
	{
		public Pnt3D StartPoint { get; private set; }
		public Pnt3D EndPoint { get; private set; }

		public Pnt3D StartPointCopy => StartPoint.ClonePnt3D();
		public Pnt3D EndPointCopy => EndPoint.ClonePnt3D();

		public int Dimension => 3;

		public IBox Extent => _extent ?? (_extent = new Box(new Pnt3D(XMin, YMin, ZMin),
		                                                    new Pnt3D(XMax, YMax, ZMax)));

		public IGmtry Border => null; // Requires point collection

		private Vector _directionVector;

		private double _length2DSquared = -1;
		private double _length2D = -1;
		private IBox _extent;

		public double XMin { get; private set; }
		public double YMin { get; private set; }
		public double ZMin { get; private set; }

		public double XMax { get; private set; }
		public double YMax { get; private set; }
		public double ZMax { get; private set; }

		#region Efficient access of the direction vector's x,y,z components

		private double _deltaX = double.NaN;
		private double _deltaY = double.NaN;
		private double _deltaZ = double.NaN;

		public double DeltaX
		{
			get
			{
				if (double.IsNaN(_deltaX))
				{
					_deltaX = EndPoint.X - StartPoint.X;
				}

				return _deltaX;
			}
		}

		public double DeltaY
		{
			get
			{
				if (double.IsNaN(_deltaY))
				{
					_deltaY = EndPoint.Y - StartPoint.Y;
				}

				return _deltaY;
			}
		}

		public double DeltaZ
		{
			get
			{
				if (double.IsNaN(_deltaZ))
				{
					_deltaZ = EndPoint.Z - StartPoint.Z;
				}

				return _deltaZ;
			}
		}

		#endregion

		public Line3D(Pnt3D startPoint, Pnt3D endPoint)
		{
			StartPoint = startPoint;
			EndPoint = endPoint;

			// Avoid / defer the (expensive) _extent creation
			UpdateBounds(startPoint, endPoint);
		}

		/// <summary>
		/// The 3D length if the line has non-NaN Z values, otherwise the 2D length (length of the projection 
		/// into the XY plane).
		/// </summary>
		public double Length3D
		{
			get
			{
				double lengthSquared = DirectionVector.LengthSquared;

				if (! double.IsNaN(lengthSquared))
				{
					return Math.Sqrt(lengthSquared);
				}

				return Math.Sqrt(DirectionVector.Length2DSquared);
			}
		}

		public double Length2DSquared
		{
			get
			{
				// Avoid direction vector for performance reasons
				if (_length2DSquared < 0)
				{
					_length2DSquared = DeltaX * DeltaX +
					                   DeltaY * DeltaY;
				}

				return _length2DSquared;
			}
		}

		public double Length2D
		{
			get
			{
				if (_length2D < 0)
				{
					_length2D = Math.Sqrt(Length2DSquared);
				}

				return _length2D;
			}
		}

		public Vector DirectionVector
		{
			get
			{
				// Instantiation of the coordinates array is expensive, delay creation, but create only once
				if (_directionVector == null)
				{
					_directionVector = new Vector(new[]
					                              {
						                              EndPoint.X - StartPoint.X,
						                              EndPoint.Y - StartPoint.Y,
						                              EndPoint.Z - StartPoint.Z
					                              });
				}

				return _directionVector;
			}
		}

		public bool IsDefined => ! double.IsNaN(DirectionVector.LengthSquared) &&
		                         DirectionVector.LengthSquared > 0;

		[CanBeNull]
		public static Line3D ConstructInBox([NotNull] Pnt3D p0,
		                                    [NotNull] Vector direction,
		                                    [NotNull] IBox withinBox)
		{
			double xMin = withinBox.Min.X;
			double yMin = withinBox.Min.Y;
			double zMin = withinBox.Min[2];
			double xMax = withinBox.Max.X;
			double yMax = withinBox.Max.Y;
			double zMax = withinBox.Max[2];

			return ConstructInBox(p0, direction, xMin, xMax, yMin, yMax, zMin, zMax);
		}

		[CanBeNull]
		public static Line3D ConstructInBox([NotNull] Pnt3D p0,
		                                    [NotNull] Vector direction,
		                                    double xMin, double xMax,
		                                    double yMin, double yMax,
		                                    double zMin, double zMax)
		{
			Assert.ArgumentNotNaN(xMin, nameof(xMin));
			Assert.ArgumentNotNaN(xMax, nameof(xMax));
			Assert.ArgumentNotNaN(yMin, nameof(yMin));
			Assert.ArgumentNotNaN(yMax, nameof(yMax));
			Assert.ArgumentNotNaN(zMin, nameof(zMin));
			Assert.ArgumentNotNaN(zMax, nameof(zMax));

			if (MathUtils.AreEqual(direction.LengthSquared, 0) ||
			    double.IsNaN(p0.X) || double.IsNaN(p0.Y) || double.IsNaN(p0.Z))
			{
				return null;
			}

			// Get s in the largest dimension of the vector.
			double sMax = double.NaN;
			double sMin = double.NaN;

			double directionX = direction[0];
			double directionY = direction[1];
			double directionZ = direction[2];

			if (Math.Abs(directionX) >= Math.Abs(directionY) &&
			    Math.Abs(directionX) >= Math.Abs(directionZ))
			{
				sMax = (xMax - p0.X) / directionX;
				sMin = (xMin - p0.X) / directionX;
			}

			if (Math.Abs(directionY) >= Math.Abs(directionX) &&
			    Math.Abs(directionY) >= Math.Abs(directionZ))
			{
				sMax = (yMax - p0.Y) / directionY;
				sMin = (yMin - p0.Y) / directionY;
			}

			if (Math.Abs(directionZ) >= Math.Abs(directionX) &&
			    Math.Abs(directionZ) >= Math.Abs(directionY))
			{
				sMax = (zMax - p0.Z) / directionZ;
				sMin = (zMin - p0.Z) / directionZ;
			}

			Assert.False(double.IsNaN(sMax), "sMax could not be determined by box.");
			Assert.False(double.IsNaN(sMax), "sMin could not be determined by box.");

			var fromPoint = new Pnt3D(
				p0.X + sMin * directionX,
				p0.Y + sMin * directionY,
				p0.Z + sMin * directionZ);

			var toPoint = new Pnt3D(
				p0.X + sMax * directionX,
				p0.Y + sMax * directionY,
				p0.Z + sMax * directionZ);

			return new Line3D(fromPoint, toPoint);
		}

		public void ReverseOrientation()
		{
			_directionVector = null;

			Pnt3D tmp = StartPoint;
			StartPoint = EndPoint;
			EndPoint = tmp;

			UpdateDeltas();
		}

		public Pnt3D GetPointAlong(double distance, bool asRatio)
		{
			if (! asRatio)
			{
				distance = distance / DirectionVector.Length;
			}

			Pnt result = StartPoint + distance * DirectionVector;

			return new Pnt3D(result[0], result[1], result[2]);
		}

		public double GetDistanceAlong(Pnt3D point, bool asRatio = false)
		{
			Pnt3D relativeToStart = point - StartPoint;

			double distanceRatio = DirectionVector.GetFactor(relativeToStart);

			return asRatio ? distanceRatio : Length3D * distanceRatio;
		}

		/// <summary>
		/// The perpendicular distance of the infinite line defined by the Start/End points of this
		/// line to the specified point.
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public double GetDistancePerpendicular(Pnt3D point)
		{
			var sp = new Line3D(StartPoint, point);

			Vector vectorProduct = GeomUtils.CrossProduct(DirectionVector,
			                                              sp.DirectionVector);

			return Math.Abs(vectorProduct.Length / DirectionVector.Length);
		}

		/// <summary>
		/// The perpendicular distance of the infinite line defined by the Start/End points of this
		/// line to the specified point.
		/// </summary>
		/// <param name="point"></param>
		/// <param name="inXY"></param>
		/// <param name="distanceAlongRatio">The distance-along-ratio of the closest point on the line.</param>
		/// <param name="pointOnLine"></param>
		/// <returns></returns>
		public double GetDistancePerpendicular(Pnt3D point, bool inXY,
		                                       out double distanceAlongRatio,
		                                       out Pnt3D pointOnLine)
		{
			// http://geomalgorithms.com/a02-_lines.html#Distance-to-Infinite-Line

			Pnt3D w = point - StartPoint;

			double c1, c2;

			if (inXY)
			{
				c1 = GeomUtils.DotProduct(w.X, w.Y, 0, DirectionVector.X,
				                          DirectionVector.Y, 0);
				c2 = DirectionVector.Length2DSquared;
			}
			else
			{
				c1 = GeomUtils.DotProduct(w, DirectionVector);
				c2 = DirectionVector.LengthSquared;
			}

			if (c2 < double.Epsilon)
			{
				// 0-length line: Distance to StartPoint
				distanceAlongRatio = 0;
				pointOnLine = (Pnt3D) StartPoint.Clone();
				return StartPoint.GetDistance(point, inXY);
			}

			distanceAlongRatio = c1 / c2;

			pointOnLine = (Pnt3D) (StartPoint + distanceAlongRatio * DirectionVector);

			return pointOnLine.GetDistance(point, inXY);
		}

		public double GetDistancePerpendicular(Pnt3D point, bool inXY)
		{
			// Could theoretically be faster with using the cross-product variant with Z=0
			return GetDistancePerpendicular(point, inXY, out double _, out Pnt3D _);
		}

		public bool ExtentIntersects([NotNull] IBox box,
		                             double tolerance = double.Epsilon,
		                             bool inXY = false)
		{
			int dimensions = inXY ? 2 : 3;

			for (var i = 0; i < dimensions; i++)
			{
				if (! ExtentIntersects1D(i, box.Min[i], box.Max[i], tolerance))
				{
					return false;
				}
			}

			return true;
		}

		public bool ExtentIntersectsXY(IBoundedXY other, double tolerance)
		{
			return ExtentIntersectsXY(other.XMin, other.YMin, other.XMax, other.YMax,
			                          tolerance);
		}

		public bool ExtentIntersectsXY(
			double xMin, double yMin, double xMax, double yMax,
			double tolerance)
		{
			return ! GeomRelationUtils.AreBoundsDisjoint(XMin, YMin, XMax, YMax,
			                                             xMin, yMin, xMax, yMax,
			                                             tolerance);
		}

		public bool IntersectsPointXY([NotNull] ICoordinates point, double tolerance)
		{
			if (! ExtentIntersectsXY(point, tolerance))
			{
				return false;
			}

			double distanceAlong;
			double distanceFrom =
				GetDistanceXYPerpendicularSigned(point, out distanceAlong);

			if (Math.Abs(distanceFrom) > tolerance)
			{
				return false;
			}

			// Use the same logic as in SegmentIntersection! At end points, check the actual distance:
			if (distanceAlong < 0)
			{
				return GeomUtils.GetDistanceSquaredXY(StartPoint, point) <= tolerance * tolerance;
			}

			if (distanceAlong > 1)
			{
				return GeomUtils.GetDistanceSquaredXY(EndPoint, point) <= tolerance * tolerance;
			}

			return true;
		}

		/// <summary>
		/// Determines whether the two lines have a 2D-intersection in the form of one point.
		/// The tolerance is applied with respect to the location of the actual intersection
		/// point along the distance vectors. Therefore the exact intersection point located 
		/// within the tolerance of an end point is counted as an intersection. However,
		/// if two end points are within the tolerance but the lines are almost parallel
		/// the exact end point is most likely not within the tolerance of an endpoint
		/// and will not be classified as an intersection.
		/// </summary>
		/// <param name="other">The other line</param>
		/// <param name="tolerance"></param>
		/// <param name="thisFactor">The factor to calculate the intersection point along this line.</param>
		/// <param name="otherFactor">The factor to calculate the intersection point along the other line.</param>
		/// <returns></returns>
		public bool HasIntersectionPointXY([NotNull] Line3D other,
		                                   double tolerance,
		                                   out double thisFactor, out double otherFactor)
		{
			thisFactor = double.NaN;
			otherFactor = double.NaN;

			if (! Intersects1D(XMin, XMax, other.XMin, other.XMax, tolerance))
			{
				return false;
			}

			if (! Intersects1D(YMin, YMax, other.YMin, other.YMax, tolerance))
			{
				return false;
			}

			if (! TryGetIntersectionPointFactorsXY(other, out thisFactor,
			                                       out otherFactor))
			{
				return false;
			}

			double thisToleranceFactor = tolerance / Length3D;

			if (thisFactor < 0 - thisToleranceFactor ||
			    thisFactor > 1 + thisToleranceFactor)
			{
				return false;
			}

			double otherToleranceFactor = tolerance / other.Length3D;
			if (otherFactor < 0 - otherToleranceFactor ||
			    otherFactor > 1 + otherToleranceFactor)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Determines the intersection point's factor of this line and the other line, if there is an
		/// intersection.
		/// </summary>
		/// <param name="other">The other line</param>
		/// <param name="thisFactor">The factor to calculate the intersection point along this line.</param>
		/// <param name="otherFactor">The factor to calculate the intersection point along the other line.</param>
		/// <returns></returns>
		public bool TryGetIntersectionPointFactorsXY(
			[NotNull] Line3D other,
			out double thisFactor, out double otherFactor)
		{
			thisFactor = double.NaN;
			otherFactor = double.NaN;

			// TODO: Pnt.VectorProduct is the same as the Perp Product but should probably be called VectorProductXY or PerpProduct?
			//       The vector product is not well-defined in 2D
			double d = PerpProduct(DeltaX, DeltaY,
			                       other.DeltaX, other.DeltaY);

			if (MathUtils.AreEqual(d, 0))
			{
				// parallel in XY plane
				return false;
			}

			Pnt3D w = StartPoint - other.StartPoint;

			thisFactor =
				PerpProduct(other.DeltaX, other.DeltaY, w.X, w.Y) / d;

			otherFactor = PerpProduct(DeltaX, DeltaY, w.X, w.Y) / d;

			return true;
		}

		/// <summary>
		/// Returns - larger 0 for testPoint left of the line from lineStart to lineEnd
		///         - 0 for testPoint on the line
		///         - smaller 0 for test point right of the line
		/// </summary>
		/// <param name="testPoint"></param>
		/// <returns></returns>
		public double IsLeftXY([NotNull] IPnt testPoint)
		{
			double vX = testPoint.X - StartPoint.X;
			double vY = testPoint.Y - StartPoint.Y;

			double perpProduct =
				PerpProduct(DirectionVector.X, DirectionVector.Y, vX, vY);

			return perpProduct;
		}

		/// <summary>
		/// The perpendicular XY-distance (i.e. 2D-distance disregarding Zs) of the infinite line defined by 
		/// the Start/End points of this line to the specified point. A positive value indicates that the 
		/// provided point lies on the left of the line.
		/// </summary>
		/// <param name="testPoint"></param>
		/// <returns></returns>
		public double GetDistanceXYPerpendicularSigned([NotNull] IPnt testPoint)
		{
			double vX = testPoint.X - StartPoint.X;
			double vY = testPoint.Y - StartPoint.Y;

			double perpProduct =
				PerpProduct(DirectionVector.X, DirectionVector.Y, vX, vY);

			if (Math.Abs(perpProduct) > 0)
			{
				double length2D = Length2D;

				Assert.True(length2D > 0, "0-length line has non-0 perp product.");

				return perpProduct / length2D;
			}

			return 0;
		}

		/// <summary>
		/// The perpendicular XY-distance (i.e. 2D-distance disregarding Zs) of the infinite line defined by 
		/// the Start/End points of this line to the specified point. A positive value indicates that the 
		/// provided point lies on the left of the line.
		/// </summary>
		/// <param name="testPoint"></param>
		/// <param name="distanceAlongRatio">The distance along the line as a ratio between 0 and 1 if the
		/// projection of the test point falls within the line, below 0 if it is before the start point,
		/// greater than 1 if it is after the end point.</param>
		/// <returns></returns>
		public double GetDistanceXYPerpendicularSigned(ICoordinates testPoint,
		                                               out double distanceAlongRatio)
		{
			double vX = testPoint.X - StartPoint.X;
			double vY = testPoint.Y - StartPoint.Y;

			double perpProduct = PerpProduct(DeltaX, DeltaY, vX, vY);

			if (Math.Abs(perpProduct) > 0)
			{
				double length2D = Length2D;

				Assert.True(length2D > 0, "0-length or vertical line where not expected");

				double dotProduct = vX * DeltaX + vY * DeltaY;

				distanceAlongRatio = dotProduct / Length2DSquared;

				return perpProduct / length2D;
			}

			// testPoint is on the straight line
			// GetDistanceAlongFactor in XY
			if (Math.Abs(DeltaX) > Math.Abs(DeltaY))
			{
				distanceAlongRatio = (testPoint.X - StartPoint.X) / DeltaX;
			}
			else if (Math.Abs(DeltaY) > 0)
			{
				distanceAlongRatio = (testPoint.Y - StartPoint.Y) / DeltaY;
			}
			else
			{
				// 0-length line or vertical line: Distance to StartPoint
				distanceAlongRatio = 0;
				return GeomUtils.GetDistanceXY(StartPoint, testPoint);
			}

			return 0;
		}

		/// <summary>
		/// The angle between this line and the positive x-axis in radians.
		/// </summary>
		/// <returns></returns>
		public double GetDirectionAngleXY()
		{
			return Math.Atan2(DeltaY, DeltaX);
		}

		public Line3D Clone()
		{
			return new Line3D(StartPoint.ClonePnt3D(), EndPoint.ClonePnt3D());
		}

		public bool EqualsXY(Line3D other, double tolerance)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			if (other.GetType() != GetType())
				return false;

			Assert.NotNull(StartPoint);
			Assert.NotNull(EndPoint);

			return IsCoincidentXY(other, tolerance);
		}

		public bool IsCoincidentXY(Line3D other, double tolerance)
		{
			Func<Pnt3D, Pnt3D, bool> pointComparison =
				(p1, p2) => p1.EqualsXY(p2, tolerance);

			return IsCoincident(other, pointComparison);
		}

		public bool IsCoincident3D(Line3D other, double tolerance)
		{
			Func<Pnt3D, Pnt3D, bool> pointComparison =
				(p1, p2) => p1.Equals(p2, tolerance);

			return IsCoincident(other, pointComparison);
		}

		private bool IsCoincident([NotNull] Line3D other,
		                          Func<Pnt3D, Pnt3D, bool> pointComparison)
		{
			bool startPointsEqual = pointComparison(StartPoint, other.StartPoint);

			if (startPointsEqual)
			{
				return pointComparison(EndPoint, other.EndPoint);
			}

			bool startEqualsEnd = pointComparison(StartPoint, other.EndPoint);

			if (startEqualsEnd)
			{
				return pointComparison(EndPoint, other.StartPoint);
			}

			return false;
		}

		public void SetStartPoint(Pnt3D newStartPoint)
		{
			StartPoint = newStartPoint;
			CoordinatesUpdated();
		}

		public void SetEndPoint(Pnt3D newEndPoint)
		{
			EndPoint = newEndPoint;
			CoordinatesUpdated();
		}

		public void CoordinatesUpdated()
		{
			UpdateBounds(StartPoint, EndPoint);
			UpdateDeltas();

			_directionVector = null;

			_length2DSquared = -1;
			_length2D = -1;
			_extent = null;
		}

		public override string ToString()
		{
			return $"From: {StartPoint} To: {EndPoint} (Length: {Length3D})";
		}

		public bool Equals(Line3D other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;

			return Equals(StartPoint, other.StartPoint) &&
			       Equals(EndPoint, other.EndPoint);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != GetType())
				return false;

			var cmpr = obj as Line3D;

			if (cmpr == null)
			{
				return false;
			}

			bool startEqual = StartPoint != null && cmpr.StartPoint != null &&
			                  StartPoint.Equals(cmpr.StartPoint) ||
			                  StartPoint == null && cmpr.StartPoint == null;

			bool endEqual =
				EndPoint != null && cmpr.EndPoint != null && EndPoint.Equals(cmpr.EndPoint) ||
				EndPoint == null && cmpr.EndPoint == null;

			return startEqual && endEqual;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((StartPoint?.GetHashCode() ?? 0) * 397) ^
				       (EndPoint?.GetHashCode() ?? 0);
			}
		}

		private void UpdateBounds(Pnt3D startPoint, Pnt3D endPoint)
		{
			XMin = Math.Min(startPoint.X, endPoint.X);
			YMin = Math.Min(startPoint.Y, endPoint.Y);
			ZMin = Math.Min(startPoint.Z, endPoint.Z);

			XMax = Math.Max(startPoint.X, endPoint.X);
			YMax = Math.Max(startPoint.Y, endPoint.Y);
			ZMax = Math.Max(startPoint.Z, endPoint.Z);
		}

		private void UpdateDeltas()
		{
			_deltaX = EndPoint.X - StartPoint.X;
			_deltaY = EndPoint.Y - StartPoint.Y;
			_deltaZ = EndPoint.Z - StartPoint.Z;
		}

		private static Line3D ProjectOntoXYPlane(Line3D line3D, double z = 0)
		{
			var otherStart2D = (Pnt3D) line3D.StartPoint.Clone();
			var otherEnd2D = (Pnt3D) line3D.EndPoint.Clone();

			otherStart2D.Z = z;
			otherEnd2D.Z = z;

			return new Line3D(otherStart2D, otherEnd2D);
		}

		private static double PerpProduct(Vector u, IPnt v)
		{
			return PerpProduct(u.X, u.Y, v.X, v.Y);
		}

		private static double PerpProduct(double uX, double uY, double vX, double vY)
		{
			return uX * vY - uY * vX;
		}

		private bool ExtentIntersects1D(int dimension,
		                                double otherMin, double otherMax,
		                                double tolerance)
		{
			double thisMax = Extent.Max[dimension];
			double thisMin = Extent.Min[dimension];

			return Intersects1D(thisMin, thisMax, otherMin, otherMax, tolerance);
		}

		private static bool Intersects1D(double thisMin, double thisMax, double otherMin,
		                                 double otherMax, double tolerance)
		{
			if (otherMin - tolerance > thisMax ||
			    otherMax + tolerance < thisMin)
			{
				return false;
			}

			return true;
		}
	}
}
