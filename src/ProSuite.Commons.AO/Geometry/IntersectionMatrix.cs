using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry
{
	public class IntersectionMatrix
	{
		// matrix layout:
		// 1 G1.interior G2.interior
		// 2 G1.interior G2.boundary
		// 3 G1.interior G2.exterior
		// 4 G1.boundary G2.interior
		// 5 G1.boundary G2.boundary
		// 6 G1.boundary G2.exterior
		// 7 G1.exterior G2.interior
		// 8 G1.exterior G2.boundary
		// 9 G1.exterior G2.exterior

		// points have no boundary, only interior

		// resources: 
		// http://resources.esri.com/help/9.3/ArcGISEngine/dotnet/40de6491-9b2d-440d-848b-2609efcd46b1.htm
		// http://resources.esri.com/help/9.3/ArcGISEngine/arcobjects/esriGeodatabase/ISpatialFilter_SpatialRelDescription.htm

		private const int _boundary = 1;
		private const int _exterior = 2;
		private const int _interior = 0;
		private readonly IntersectionConstraint[,] _matrix;
		private bool? _isSymmetric;

		/// <summary>
		/// Initializes a new instance of the <see cref="IntersectionMatrix"/> class.
		/// </summary>
		/// <param name="matrixString">The 9IM matrix string.</param>
		public IntersectionMatrix([NotNull] string matrixString)
		{
			Assert.ArgumentNotNullOrEmpty(matrixString, nameof(matrixString));
			Assert.ArgumentCondition(matrixString.Length == 9,
			                         "Invalid intersection matrix (must be 9 characters long): {0}",
			                         matrixString);

			MatrixString = matrixString;

			_matrix = CreateMatrix(matrixString);
		}

		[NotNull]
		public string MatrixString { get; }

		public bool Intersects => MustIntersect(_matrix[_interior, _interior]) ||
		                          MustIntersect(_matrix[_interior, _boundary]) ||
		                          MustIntersect(_matrix[_boundary, _interior]) ||
		                          MustIntersect(_matrix[_boundary, _boundary]);

		public bool IntersectsExterior => MustIntersect(_matrix[_interior, _exterior]) ||
		                                  MustIntersect(_matrix[_boundary, _exterior]);

		public bool Symmetric
		{
			get
			{
				if (_isSymmetric == null)
				{
					_isSymmetric = IsSymmetric(_matrix);
				}

				return _isSymmetric.Value;
			}
		}

		public IntersectionConstraint GetConstraint(PointSet pointSet1, PointSet pointSet2)
		{
			return _matrix[(int) pointSet1, (int) pointSet2];
		}

		[NotNull]
		[CLSCompliant(false)]
		public IList<IGeometry> GetIntersections([NotNull] IGeometry g1,
		                                         [NotNull] IGeometry g2)
		{
			var geometries = new List<IGeometry>();

			Dimension maximumDimension;

			// g1.interior
			if (MustIntersect(_matrix[_interior, _interior], out maximumDimension))
			{
				geometries.AddRange(GetInteriorInteriorIntersections(g1, g2,
				                                                     maximumDimension));
			}

			if (MustIntersect(_matrix[_interior, _boundary], out maximumDimension))
			{
				geometries.AddRange(GetInteriorBoundaryIntersections(g1, g2,
				                                                     maximumDimension));
			}

			if (MustIntersect(_matrix[_interior, _exterior], out maximumDimension))
			{
				geometries.AddRange(GetInteriorExteriorIntersections(g1, g2,
				                                                     maximumDimension));
			}

			// g1.boundary
			if (MustIntersect(_matrix[_boundary, _interior], out maximumDimension))
			{
				// swap
				geometries.AddRange(GetInteriorBoundaryIntersections(g2, g1,
				                                                     maximumDimension));
			}

			if (MustIntersect(_matrix[_boundary, _boundary], out maximumDimension))
			{
				geometries.AddRange(GetBoundaryBoundaryIntersections(g1, g2,
				                                                     maximumDimension));
			}

			if (MustIntersect(_matrix[_boundary, _exterior], out maximumDimension))
			{
				geometries.AddRange(GetBoundaryExteriorIntersections(g1, g2,
				                                                     maximumDimension));
			}

			// g1.exterior
			if (MustIntersect(_matrix[_exterior, _interior], out maximumDimension))
			{
				// swap
				geometries.AddRange(GetInteriorExteriorIntersections(g2, g1,
				                                                     maximumDimension));
			}

			if (MustIntersect(_matrix[_exterior, _boundary], out maximumDimension))
			{
				// swap
				geometries.AddRange(GetBoundaryExteriorIntersections(g2, g1,
				                                                     maximumDimension));
			}

			if (MustIntersect(_matrix[_exterior, _exterior], out maximumDimension))
			{
				// makes no sense, ignore
			}

			return Cleanup(geometries);
		}

		private static bool IsSymmetric([NotNull] IntersectionConstraint[,] matrix)
		{
			Assert.ArgumentNotNull(matrix, nameof(matrix));

			for (int g1 = _interior; g1 <= _exterior; g1++)
			{
				for (int g2 = _interior; g2 <= _exterior; g2++)
				{
					if (matrix[g1, g2] != matrix[g2, g1])
					{
						return false;
					}
				}
			}

			return true;
		}

		[NotNull]
		private static IList<IGeometry> Cleanup([NotNull] IList<IGeometry> geometries)
		{
			if (geometries.Count <= 1)
			{
				return geometries;
			}

			// group by geometry type
			var geometriesByType = new Dictionary<esriGeometryType, List<IGeometry>>();

			foreach (IGeometry geometry in geometries)
			{
				if (geometry.IsEmpty)
				{
					continue;
				}

				esriGeometryType geometryType = geometry.GeometryType;
				List<IGeometry> list;
				if (! geometriesByType.TryGetValue(geometryType, out list))
				{
					list = new List<IGeometry>();
					geometriesByType.Add(geometryType, list);
				}

				list.Add(geometry);
			}

			// union by geometry type
			var unionedList = new List<IGeometry>();
			foreach (
				KeyValuePair<esriGeometryType, List<IGeometry>> pair in geometriesByType)
			{
				List<IGeometry> geometryList = pair.Value;

				IGeometry unionTarget = geometryList[0];

				GeometryUtils.AllowIndexing(unionTarget);

				var unionedTopoOp = (ITopologicalOperator) unionTarget;

				for (var i = 1; i < geometryList.Count; i++)
				{
					unionTarget = unionedTopoOp.Union(geometryList[i]);

					Simplify(unionTarget);
					GeometryUtils.AllowIndexing(unionTarget);
				}

				unionedList.Add(unionTarget);
			}

			// sort the list by dimension (higher dimension first)
			//  invert -> higher dimension first
			unionedList.Sort((g1, g2) => -1 * g1.Dimension.CompareTo(g2.Dimension));

			// reduce lower dimension geometries by difference from higher dimension geometries
			for (var i = 0; i < unionedList.Count; i++)
			{
				IGeometry lower = unionedList[i];

				for (var j = 0; j < i; j++)
				{
					IGeometry higher = unionedList[j];

					IGeometry oldLower = lower;
					lower = GetDifference(lower, higher);
					Simplify(lower);

					Marshal.ReleaseComObject(oldLower);
				}

				unionedList[i] = lower;
			}

			// return non-empty parts
			return unionedList.Where(geometry => ! geometry.IsEmpty).ToList();
		}

		private static void Simplify([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			const bool allowReorder = true;
			const bool allowPathSplitAtIntersections = true;

			GeometryUtils.Simplify(geometry, allowReorder, allowPathSplitAtIntersections);
		}

		[NotNull]
		private static IEnumerable<IGeometry> GetInteriorInteriorIntersections(
			[NotNull] IGeometry g1,
			[NotNull] IGeometry g2,
			Dimension maximumDimension)
		{
			// return GetRelevantIntersections(g1, g2, maximumDimension);
			IGeometry g1Boundary = null;
			IGeometry g2Boundary = null;

			try
			{
				foreach (
					IGeometry intersection in
					GetRelevantIntersections(g1, g2, maximumDimension))
				{
					// remove the boundaries from the intersection.

					// Only remove a boundary if the corresponding geometry has a higher
					// dimension than the intersection. Otherwise the boundary (which has 1 dimension 
					// less than the geometry) will have a lower dimension than the intersection, 
					// and difference then makes no sense (and at least in some cases it throws an error)

					IGeometry remainder;
					if (intersection.Dimension < g1.Dimension)
					{
						if (g1Boundary == null)
						{
							g1Boundary = GetBoundary(g1);
						}

						remainder = GetDifference(intersection, g1Boundary);
						Simplify(remainder);

						Marshal.ReleaseComObject(intersection);
					}
					else
					{
						remainder = intersection;
					}

					if (remainder.IsEmpty)
					{
						continue;
					}

					if (remainder.Dimension < g2.Dimension)
					{
						if (g2Boundary == null)
						{
							g2Boundary = GetBoundary(g2);
						}

						// calculate the new remainder by
						// cutting away the g2 boundary from the 
						// previous remainder
						IGeometry previousRemainder = remainder;

						remainder = GetDifference(previousRemainder, g2Boundary);
						Simplify(remainder);

						Marshal.ReleaseComObject(previousRemainder);
					}

					if (! remainder.IsEmpty)
					{
						yield return remainder;
					}
				}
			}
			finally
			{
				if (g1Boundary != null)
				{
					Marshal.ReleaseComObject(g1Boundary);
				}

				if (g2Boundary != null)
				{
					Marshal.ReleaseComObject(g2Boundary);
				}
			}
		}

		[NotNull]
		private static IEnumerable<IGeometry> GetInteriorBoundaryIntersections(
			[NotNull] IGeometry g1,
			[NotNull] IGeometry g2,
			Dimension maximumDimension)
		{
			IGeometry g1Boundary = null;

			foreach (
				IGeometry intersection in
				GetRelevantIntersections(g1, GetBoundary(g2), maximumDimension))
			{
				// cut away boundary of g1 from result (if applicable)
				if (g1Boundary == null)
				{
					// calculate g1 boundary only if needed
					g1Boundary = GetBoundary(g1);
				}

				IGeometry difference = GetDifference(intersection, g1Boundary);

				if (! difference.IsEmpty)
				{
					yield return difference;
				}
			}
		}

		[NotNull]
		private static IGeometry GetBoundary([NotNull] IGeometry geometry)
		{
			// call method that does simplify (on copy) if input is non-simple
			return GeometryUtils.GetBoundary(geometry);
		}

		[NotNull]
		private static IEnumerable<IGeometry> GetInteriorExteriorIntersections(
			[NotNull] IGeometry g1,
			[NotNull] IGeometry g2,
			Dimension maximumDimension)
		{
			// TODO cut away boundary of g1 from result (if applicable)
			return GetRelevantDifferences(g1, g2, maximumDimension);
		}

		[NotNull]
		private static IEnumerable<IGeometry> GetRelevantIntersections(
			[NotNull] IGeometry g1,
			[NotNull] IGeometry g2,
			Dimension maximumDimension)
		{
			foreach (IGeometry geometry in IntersectionUtils.GetAllIntersections(g1, g2))
			{
				if (HasEqualOrLowerDimension(geometry, maximumDimension))
				{
					Simplify(geometry);
					yield return geometry;
				}
				else
				{
					Marshal.ReleaseComObject(geometry);
				}
			}
		}

		private static bool HasEqualOrLowerDimension([NotNull] IGeometry geometry,
		                                             Dimension maximumDimension)
		{
			switch (maximumDimension)
			{
				case Dimension.Any:
					return true;

				case Dimension.Dim0:
					return geometry.Dimension <=
					       esriGeometryDimension.esriGeometry0Dimension;

				case Dimension.Dim1:
					return geometry.Dimension <=
					       esriGeometryDimension.esriGeometry1Dimension;

				case Dimension.Dim2:
					return geometry.Dimension <=
					       esriGeometryDimension.esriGeometry2Dimension;

				default:
					throw new ArgumentOutOfRangeException(nameof(maximumDimension));
			}
		}

		[NotNull]
		private static IEnumerable<IGeometry> GetRelevantDifferences(
			[NotNull] IGeometry g1,
			[NotNull] IGeometry g2,
			Dimension maximumDimension)
		{
			IGeometry difference = GetDifference(g1, g2);

			if (difference.IsEmpty || ! HasEqualOrLowerDimension(difference, maximumDimension))
			{
				yield break;
			}

			Simplify(difference);
			yield return difference;
		}

		/// <summary>
		/// Gets the difference between two geometries, g1 - g2.
		/// </summary>
		/// <param name="g1">The first geometry.</param>
		/// <param name="g2">The second difference.</param>
		/// <returns>The points on g1 that are not on g2</returns>
		/// <remarks>The result is always a new instance.</remarks>
		[NotNull]
		private static IGeometry GetDifference([NotNull] IGeometry g1,
		                                       [NotNull] IGeometry g2)
		{
			if (g2.Dimension < g1.Dimension)
			{
				// the geometry to subtract has a lower dimension than the 
				// geometry to subtract from --> no subtraction possible
				// (subtracting polyline from polygon or point from polyline/polygon
				//  -> return the unchanged g1 geometry)
				return GeometryFactory.Clone(g1);
			}

			GeometryUtils.AllowIndexing(g1);
			GeometryUtils.AllowIndexing(g2);

			// g1=point and g2 = multipoint causes an AccessViolationException
			// anyway if g1 is a point a simple disjoint test is sufficient
			var p1 = g1 as IPoint;
			if (p1 == null)
			{
				// g1 is not a point --> difference should work
				return ((ITopologicalOperator) g1).Difference(g2);
			}

			IPoint result = GeometryFactory.Clone(p1);

			if (g2 is IMultipoint)
			{
				// Workaround for NIM074889 (see https://issuetracker02.eggits.net/browse/TOP-4131)
				if (((IRelationalOperator) p1).Within(g2))
				{
					// g2 covers g1 --> difference is empty
					result.SetEmpty();
				}
			}
			else
			{
				if (! ((IRelationalOperator) p1).Disjoint(g2))
				{
					// g2 covers g1 --> difference is empty
					result.SetEmpty();
				}
			}

			return result;
		}

		[NotNull]
		private static IEnumerable<IGeometry> GetBoundaryBoundaryIntersections(
			[NotNull] IGeometry g1,
			[NotNull] IGeometry g2,
			Dimension maximumDimension)
		{
			return GetRelevantIntersections(GetBoundary(g1),
			                                GetBoundary(g2), maximumDimension);
		}

		[NotNull]
		private static IEnumerable<IGeometry> GetBoundaryExteriorIntersections(
			[NotNull] IGeometry g1,
			[NotNull] IGeometry g2,
			Dimension maximumDimension)
		{
			return GetRelevantDifferences(GetBoundary(g1), g2, maximumDimension);
		}

		private static bool MustIntersect(IntersectionConstraint constraint)
		{
			Dimension dimension;
			return MustIntersect(constraint, out dimension);
		}

		private static bool MustIntersect(IntersectionConstraint constraint,
		                                  out Dimension maximumDimension)
		{
			switch (constraint)
			{
				case IntersectionConstraint.MustNotIntersect:
				case IntersectionConstraint.NotChecked:
					maximumDimension = Dimension.Any;
					return false;

				case IntersectionConstraint.MustIntersect:
					maximumDimension = Dimension.Any;
					return true;

				case IntersectionConstraint.MustIntersectWithMaxDimension0:
					maximumDimension = Dimension.Dim0;
					return true;

				case IntersectionConstraint.MustIntersectWithMaxDimension1:
					maximumDimension = Dimension.Dim1;
					return true;

				case IntersectionConstraint.MustIntersectWithMaxDimension2:
					maximumDimension = Dimension.Dim2;
					return true;

				default:
					throw new ArgumentOutOfRangeException(nameof(constraint));
			}
		}

		[NotNull]
		private static IntersectionConstraint[,] CreateMatrix(string matrixString)
		{
			var matrix = new IntersectionConstraint[3, 3];

			// first row
			matrix[_interior, _interior] = GetConstraint(matrixString[0]);
			matrix[_interior, _boundary] = GetConstraint(matrixString[1]);
			matrix[_interior, _exterior] = GetConstraint(matrixString[2]);

			// second row
			matrix[_boundary, _interior] = GetConstraint(matrixString[3]);
			matrix[_boundary, _boundary] = GetConstraint(matrixString[4]);
			matrix[_boundary, _exterior] = GetConstraint(matrixString[5]);

			// third row
			matrix[_exterior, _interior] = GetConstraint(matrixString[6]);
			matrix[_exterior, _boundary] = GetConstraint(matrixString[7]);
			matrix[_exterior, _exterior] = GetConstraint(matrixString[8]);

			return matrix;
		}

		private static IntersectionConstraint GetConstraint(char matrixCharacter)
		{
			switch (char.ToUpper(matrixCharacter))
			{
				case 'T':
					return IntersectionConstraint.MustIntersect;
				case 'F':
					return IntersectionConstraint.MustNotIntersect;
				case '0':
					return IntersectionConstraint.MustIntersectWithMaxDimension0;
				case '1':
					return IntersectionConstraint.MustIntersectWithMaxDimension1;
				case '2':
					return IntersectionConstraint.MustIntersectWithMaxDimension2;
				case '*':
					return IntersectionConstraint.NotChecked;
				default:
					throw new ArgumentOutOfRangeException(nameof(matrixCharacter),
					                                      matrixCharacter,
					                                      @"Invalid intersection matrix character");
			}
		}

		#region Nested type: Dimension

		private enum Dimension
		{
			Any,
			Dim0,
			Dim1,
			Dim2
		}

		#endregion
	}
}
