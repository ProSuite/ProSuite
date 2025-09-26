using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry
{
	/// <summary>
	/// A control point is a vertex with a non-zero ID.
	/// Control points may be used with representations
	/// and geometric effects to create nice cartography.
	/// </summary>
	public static class ControlPointUtils
	{
		/// <summary>
		/// Get the ID of the vertex at <paramref name="vertexIndex"/>
		/// of <paramref name="curve"/>.
		/// </summary>
		/// <remarks>
		/// The <paramref name="vertexIndex"/> is global, that is,
		/// counting across part boundaries in case of a multipart curve.
		/// </remarks>
		public static int GetControlPoint([NotNull] ICurve curve, int vertexIndex)
		{
			Assert.ArgumentNotNull(curve, nameof(curve));

			var vertices = (IPointCollection) curve;
			IEnumVertex enumVertex = vertices.EnumVertices;

			enumVertex.Reset();
			enumVertex.Skip(vertexIndex);

			IPoint vertex;
			enumVertex.Next(out vertex, out int _, out int _);

			return vertex.ID;
		}

		/// <summary>
		/// Get the ID of the vertex at <paramref name="vertexIndex"/>
		/// and <paramref name="partIndex"/> of <paramref name="curve"/>.
		/// </summary>
		/// <remarks>
		/// The <paramref name="vertexIndex"/> is local to the part
		/// at <paramref name="partIndex"/>.
		/// </remarks>
		public static int GetControlPoint([NotNull] ICurve curve, int partIndex,
		                                  int vertexIndex)
		{
			Assert.ArgumentNotNull(curve, nameof(curve));

			var vertices = (IPointCollection) curve;
			IEnumVertex enumVertex = vertices.EnumVertices;

			enumVertex.SetAt(partIndex, vertexIndex);

			IPoint vertex;
			enumVertex.Next(out vertex, out int _, out int _);

			return vertex.ID;
		}

		/// <summary>
		/// Make the vertex at <paramref name="vertexIndex"/> of <paramref name="curve"/>
		/// a control point  by setting its ID to <paramref name="value"/>.
		/// </summary>
		/// <remarks>
		/// The <paramref name="vertexIndex"/> is global, that is,
		/// counting across part boundaries in case of a multipart curve.
		/// <para/>
		/// Setting a control point at the FromPoint of a closed
		/// <paramref name="curve"/> will also set it at the ToPoint
		/// (and vice versa).
		/// <para/>
		/// Preserves non-linear segments.
		/// </remarks>
		public static void SetControlPoint([NotNull] ICurve curve, int vertexIndex,
		                                   int value)
		{
			Assert.ArgumentNotNull(curve, nameof(curve));

			TryEnsureIdAware(curve);

			var vertices = (IPointCollection) curve;
			IEnumVertex enumVertex = vertices.EnumVertices;

			enumVertex.Reset();
			enumVertex.Skip(vertexIndex);

			enumVertex.Next(out IPoint _, out int _, out int _);
			// It seems put_ID() after Skip() requires a Next().
			enumVertex.put_ID(value);

			// Notice:
			// IPointCollection.UpdatePoint() has the same FromPoint
			// and ToPoint behaviour on closed curves. Experimentation
			// shows that IPointCollection.ReplacePoints() can be used
			// to treat FromPoint and ToPoint separately. However,
			// ReplacePoints() destroys non-linear segments.
		}

		/// <summary>
		/// Make the vertex at <paramref name="vertexIndex"/> of the part
		/// at <paramref name="partIndex"/> of <paramref name="curve"/>
		/// a control point by setting its ID to <paramref name="value"/>.
		/// </summary>
		/// <remarks>
		/// The <paramref name="vertexIndex"/> is local to the part
		/// at <paramref name="partIndex"/>.
		/// <para/>
		/// Preserves non-linear segments.
		/// </remarks>
		public static void SetControlPoint([NotNull] ICurve curve, int partIndex,
		                                   int vertexIndex, int value)
		{
			Assert.ArgumentNotNull(curve, nameof(curve));

			TryEnsureIdAware(curve);

			var vertices = (IPointCollection) curve;
			IEnumVertex enumVertex = vertices.EnumVertices;

			enumVertex.SetAt(partIndex, vertexIndex);
			// Expect partIndex and vertexIndex to NOT change!
			enumVertex.Next(out IPoint _, out partIndex, out vertexIndex);
			// It seems put_ID() after SetAt() doesn't require Next(),
			// but we keep it for analogy with put_ID() after Skip(),
			// where it seems to be required.
			enumVertex.put_ID(value); // set control point
		}

		/// <summary>
		/// Return true iff the given <paramref name="geometry"/>
		/// has at least one vertex with a non-zero ID value.
		/// </summary>
		public static bool HasControlPoints([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			// Notice:
			// We COULD simply return CountControlPoints(geometry, 0) > 0,
			// but for this test, we can stop iterating after the first
			// control point found.

			if (! GeometryUtils.IsPointIDAware(geometry))
			{
				return false;
			}

			var points = geometry as IPointCollection;
			if (points != null)
			{
				var vertex = new PointClass();

				int vertexCount = points.PointCount;
				for (var i = 0; i < vertexCount; i++)
				{
					points.QueryPoint(i, vertex);
					if (vertex.ID != 0)
					{
						return true;
					}
				}
			}

			// Notice: points do not implement IPointCollection, but may
			// have a non-zero ID value. However, in ArcMap you cannot
			// set a control point on a point, so ignore this case here.

			return false;
		}

		/// <summary>
		/// Count control points of the given <paramref name="value"/>
		/// in <paramref name="geometry"/>. A <paramref name="value"/>
		/// of zero (the default) counts control points of any value.
		/// </summary>
		public static int CountControlPoints([NotNull] IGeometry geometry, int value = 0)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			if (! GeometryUtils.IsPointIDAware(geometry))
			{
				return 0;
			}

			var points = geometry as IPointCollection;
			if (points == null)
			{
				// ArcMap does not allow setting control points on IPoint geometries
				return 0;
			}

			var count = 0;

			var curve = geometry as ICurve;
			bool isClosed = curve != null && curve.IsClosed;

			IPoint vertex = new PointClass();
			IEnumVertex enumVertex = points.EnumVertices;

			var currentVertex = 0;
			int vertexCount = points.PointCount;
			enumVertex.Reset();

			while (currentVertex < vertexCount)
			{
				enumVertex.QueryNext(vertex, out int _, out int _);

				// If the geometry is a closed curve, do NOT count the last
				// vertex, which is, by definition, the same as the first one.

				if (! isClosed || ! enumVertex.IsLastInPart())
				{
					if (value == 0)
					{
						if (vertex.ID != 0)
						{
							count += 1;
						}
					}
					else
					{
						if (vertex.ID == value)
						{
							count += 1;
						}
					}
				}

				currentVertex += 1;
			}

			return count;
		}

		/// <summary>
		/// Reset control points by setting the vertex ID to zero.
		/// If <paramref name="value"/> is positive, reset only control points of this value.
		/// If <paramref name="perimeter"/> is non-null, reset only control points within perimeter.
		/// If <paramref name="value"/> is positive and <paramref name="perimeter"/>
		/// is non-null, reset only control points of this value and within perimeter.
		/// </summary>
		/// <returns>Number of control points reset.</returns>
		/// <remarks>Preserves non-linear segments.</remarks>
		public static int ResetControlPoints(
			[NotNull] ICurve curve, int value = -1, IGeometry perimeter = null)
		{
			if (! GeometryUtils.IsPointIDAware(curve))
			{
				return 0; // if not ID aware, then there is nothing to reset
			}

			return ResetControlPoints((IPointCollection) curve, value,
			                          perimeter as IRelationalOperator);
		}

		private static int ResetControlPoints(
			[NotNull] IPointCollection points, int value,
			[CanBeNull] IRelationalOperator perimeter)
		{
			Assert.ArgumentNotNull(points, nameof(points));

			var resetCount = 0;

			IPoint vertex = new PointClass();
			IEnumVertex enumVertex = points.EnumVertices;

			var currentVertex = 0;
			int vertexCount = points.PointCount;
			enumVertex.Reset();

			while (currentVertex < vertexCount)
			{
				enumVertex.QueryNext(vertex, out int _, out int _);

				bool inPerimeter = perimeter == null || perimeter.Contains(vertex);
				bool valueMatches = value < 0 || value == vertex.ID;

				if (vertex.ID != 0 && valueMatches && inPerimeter)
				{
					enumVertex.put_ID(0); // reset control point
					resetCount += 1;
				}

				currentVertex += 1;
			}

			return resetCount;
		}

		/// <summary>
		/// Reset control point pairs of the given <paramref name="value"/>
		/// to zero. If a <paramref name="perimeter"/> is given, only reset
		/// pairs if either (or both) endpoints are within the perimeter.
		/// If <paramref name="value"/> is negative, all values match.
		/// </summary>
		/// <returns>Number of control points (not pairs) reset</returns>
		public static int ResetControlPointPairs(
			[NotNull] ICurve curve, int value = -1, IGeometry perimeter = null)
		{
			if (!GeometryUtils.IsPointIDAware(curve))
			{
				return 0;
			}

			return ResetControlPointPairs((IPointCollection) curve, value,
			                              perimeter as IRelationalOperator);
		}

		private static int ResetControlPointPairs(
			[NotNull] IPointCollection points, int value,
			[CanBeNull] IRelationalOperator perimeter)
		{
			Assert.ArgumentNotNull(points, nameof(points));

			var resetCount = 0;

			int gapStartIndex = -1;
			var gapStartInPerimeter = false;
			IPoint vertex = new PointClass();

			var currentPoint = 0;
			int pointCount = points.PointCount;
			IEnumVertex enumVertex = points.EnumVertices;

			enumVertex.Reset();

			while (currentPoint < pointCount)
			{
				int partIndex, vertexIndex;
				enumVertex.QueryNext(vertex, out partIndex, out vertexIndex);

				if (vertex.ID == value)
				{
					bool inPerimeter = perimeter == null || perimeter.Contains(vertex);

					if (gapStartIndex >= 0)
					{
						if (inPerimeter || gapStartInPerimeter)
						{
							enumVertex.SetAt(partIndex, gapStartIndex);
							enumVertex.put_ID(0);
							enumVertex.SetAt(partIndex, vertexIndex);
							enumVertex.put_ID(0);
							enumVertex.Skip(1);

							resetCount += 2;
						}

						gapStartIndex = -1;
						gapStartInPerimeter = false;
					}
					else
					{
						gapStartIndex = vertexIndex;
						gapStartInPerimeter = inPerimeter;
					}
				}

				if (enumVertex.IsLastInPart())
				{
					if (gapStartIndex >= 0)
					{
						// An unpaired control point? Remove it.
						enumVertex.SetAt(partIndex, gapStartIndex);
						enumVertex.put_ID(0);
						enumVertex.SetAt(partIndex, vertexIndex);
						enumVertex.Skip(1);

						resetCount += 1;
					}

					gapStartIndex = -1;
					gapStartInPerimeter = false;
				}

				currentPoint += 1;
			}

			return resetCount;
		}

		private static void TryEnsureIdAware(IGeometry geometry)
		{
			// Beware that low-level geometries like paths
			// or segments do not implement IPointIDAware!
			var idAware = geometry as IPointIDAware;
			if (idAware != null && ! idAware.PointIDAware)
			{
				idAware.PointIDAware = true;
			}
		}

		#region Previously, w/o IEnumVertex, killing non-linear segments, but otherwise well-tested...

		//public static int ResetControlPointPairs(
		//    [NotNull] ICurve geometry, int value,
		//    [CanBeNull] IRelationalOperator perimeter)
		//{
		//    int resetCount = 0;

		//    var parts = geometry as IGeometryCollection;
		//    if (parts != null)
		//    {
		//        int partCount = parts.GeometryCount;
		//        for (int index = 0; index < partCount; index++)
		//        {
		//            var part = (ICurve)parts.get_Geometry(index);

		//            resetCount += ResetControlPointPairsPart(part, value, perimeter);
		//        }

		//        parts.GeometriesChanged(); // Really needed?
		//    }
		//    else
		//    {
		//        resetCount += ResetControlPointPairsPart(geometry, value, perimeter);
		//    }

		//    return resetCount;
		//}

		//private static int ResetControlPointPairsPart(ICurve part, int value, [CanBeNull] IRelationalOperator perimeter)
		//{
		//    int resetCount = 0;

		//    int gapStartIndex = -1;
		//    IPoint gapStartVertex = null;
		//    bool gapStartInPerimeter = false;

		//    var vertices = (IPointCollection)part;
		//    int vertexCount = vertices.PointCount;
		//    for (int index = 0; index < vertexCount; index++)
		//    {
		//        IPoint vertex = vertices.get_Point(index);

		//        if (vertex.ID == value)
		//        {
		//            bool inPerimeter = perimeter == null || perimeter.Contains(vertex);

		//            if (gapStartVertex != null)
		//            {
		//                if (inPerimeter || gapStartInPerimeter)
		//                {
		//                    gapStartVertex.ID = 0;
		//                    vertices.ReplacePoints(gapStartIndex, 1, 1, ref gapStartVertex);
		//                    vertex.ID = 0;
		//                    vertices.ReplacePoints(index, 1, 1, ref vertex);

		//                    resetCount += 2;
		//                }

		//                gapStartIndex = -1;
		//                gapStartVertex = null;
		//                gapStartInPerimeter = false;
		//            }
		//            else
		//            {
		//                gapStartIndex = index;
		//                gapStartVertex = vertex;
		//                gapStartInPerimeter = inPerimeter;
		//            }
		//        }
		//    }

		//    if (gapStartVertex != null)
		//    {
		//        // An unpaired control point? Remove it!
		//        gapStartVertex.ID = 0;
		//        vertices.ReplacePoints(gapStartIndex, 1, 1, ref gapStartVertex);

		//        resetCount += 1;
		//    }

		//    return resetCount;
		//}

		#endregion
	}
}
