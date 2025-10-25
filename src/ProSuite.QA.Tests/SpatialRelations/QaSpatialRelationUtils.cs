using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests.SpatialRelations
{
	public static class QaSpatialRelationUtils
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

		// point --> lines, polygons: the point interior must only intersect the boundary
		private static readonly IntersectionMatrix _touchesMatrixPointOther =
			new IntersectionMatrix("FT*******");

		// lines, polygons --> points: only the boundary must intersect the point interior
		private static readonly IntersectionMatrix _touchesMatrixOtherPoint =
			new IntersectionMatrix("F**T*****");

		// lines, polygons --> lines polygons: only the boundaries must intersect
		private static readonly IntersectionMatrix _touchesMatrixOther =
			new IntersectionMatrix("FF*FT****");

		// matrix for "crosses" EXCEPT for line/line
		private static readonly IntersectionMatrix _crossesMatrixOther =
			new IntersectionMatrix("T*T******");

		private const int _noError = 0;

		public static bool AreDuplicates(
			[NotNull] IReadOnlyRow row1, int tableIndex1,
			[NotNull] IReadOnlyRow row2, int tableIndex2,
			[CanBeNull] IValidRelationConstraint validRelationConstraint,
			[NotNull] out string errorDescription)
		{
			Assert.ArgumentNotNull(row1, nameof(row1));
			Assert.ArgumentNotNull(row2, nameof(row2));

			if (row1 == row2)
			{
				errorDescription = string.Empty;
				return false;
			}

			IGeometry g1 = ((IReadOnlyFeature) row1).Shape;
			IGeometry g2 = ((IReadOnlyFeature) row2).Shape;

			// equal if all corresponding vertices are within xy tolerance - z is ignored
			if (! GeometryUtils.AreEqualInXY(g1, g2))
			{
				errorDescription = string.Empty;
				return false;
			}

			return ! HasFulfilledConstraint(row1, tableIndex1,
			                                row2, tableIndex2,
			                                validRelationConstraint, "Geometries are equal",
			                                out errorDescription);
		}

		public static int ReportDuplicates(
			[NotNull] IReadOnlyRow row1, int tableIndex1,
			[NotNull] IReadOnlyRow row2, int tableIndex2,
			[NotNull] IErrorReporting errorReporting,
			[CanBeNull] IssueCode issueCode,
			[CanBeNull] IValidRelationConstraint validRelationConstraint)
		{
			Assert.ArgumentNotNull(row1, nameof(row1));
			Assert.ArgumentNotNull(row2, nameof(row2));
			Assert.ArgumentNotNull(errorReporting, nameof(errorReporting));

			string errorDescription;
			if (! AreDuplicates(row1, tableIndex1,
			                    row2, tableIndex2,
			                    validRelationConstraint,
			                    out errorDescription))
			{
				return _noError;
			}

			IGeometry errorGeometry = ((IReadOnlyFeature) row1).ShapeCopy;

			return errorReporting.Report(
				errorDescription, InvolvedRowUtils.GetInvolvedRows(row1, row2),
				errorGeometry, issueCode, null, reportIndividualParts: false);
		}

		public static int ReportTouches(
			[NotNull] IReadOnlyRow row1, int tableIndex1,
			[NotNull] IReadOnlyRow row2, int tableIndex2,
			[NotNull] IErrorReporting errorReporting,
			[CanBeNull] IssueCode issueCode,
			[CanBeNull] IValidRelationConstraint validRelationConstraint,
			[CanBeNull] GeometryConstraint validTouchGeometryConstraint,
			bool reportIndividualParts = false)
		{
			Assert.ArgumentNotNull(row1, nameof(row1));
			Assert.ArgumentNotNull(row2, nameof(row2));
			Assert.ArgumentNotNull(errorReporting, nameof(errorReporting));

			if (row1 == row2)
			{
				return _noError;
			}

			IGeometry g1 = ((IReadOnlyFeature) row1).Shape;
			IGeometry g2 = ((IReadOnlyFeature) row2).Shape;

			string errorDescription;
			if (HasFulfilledConstraint(row1, tableIndex1,
			                           row2, tableIndex2,
			                           validRelationConstraint, "Geometries touch",
			                           out errorDescription))
			{
				return _noError;
			}

			var errorCount = 0;
			foreach (IGeometry geometry in GetTouches(g1, g2))
			{
				if (geometry.IsEmpty)
				{
					continue;
				}

				if (reportIndividualParts)
				{
					foreach (IGeometry part in GeometryUtils.Explode(geometry))
					{
						if (part.IsEmpty ||
						    validTouchGeometryConstraint != null &&
						    validTouchGeometryConstraint.IsFulfilled(part))
						{
							continue;
						}

						errorCount += errorReporting.Report(
							errorDescription, InvolvedRowUtils.GetInvolvedRows(row1, row2),
							part, issueCode, null, reportIndividualParts: false // already exploded
						);
					}
				}
				else
				{
					if (validTouchGeometryConstraint != null &&
					    validTouchGeometryConstraint.IsFulfilled(geometry))
					{
						continue;
					}

					errorCount += errorReporting.Report(
						errorDescription, InvolvedRowUtils.GetInvolvedRows(row1, row2),
						geometry, issueCode, null, reportIndividualParts: false);
				}
			}

			return errorCount;
		}

		public static int ReportOverlaps(
			[NotNull] IReadOnlyRow row1, int tableIndex1,
			[NotNull] IReadOnlyRow row2, int tableIndex2,
			[NotNull] IErrorReporting errorReporting,
			[CanBeNull] IssueCode issueCode,
			[CanBeNull] IValidRelationConstraint validRelationConstraint,
			bool reportIndividualParts)
		{
			Assert.ArgumentNotNull(row1, nameof(row1));
			Assert.ArgumentNotNull(row2, nameof(row2));
			Assert.ArgumentNotNull(errorReporting, nameof(errorReporting));
			if (row1 == row2)
			{
				return _noError;
			}

			IGeometry g1 = ((IReadOnlyFeature) row1).Shape;
			IGeometry g2 = ((IReadOnlyFeature) row2).Shape;

			string errorDescription;
			if (HasFulfilledConstraint(row1, tableIndex1,
			                           row2, tableIndex2,
			                           validRelationConstraint, "Features overlap",
			                           out errorDescription))
			{
				return _noError;
			}

			IGeometry intersection = TestUtils.GetOverlap(g1, g2);

			if (intersection.IsEmpty)
			{
				Marshal.ReleaseComObject(intersection);
				return _noError;
			}

			return errorReporting.Report(
				errorDescription, InvolvedRowUtils.GetInvolvedRows(row1, row2),
				intersection, issueCode, null, reportIndividualParts);
		}

		public static int ReportIntersections(
			[NotNull] IReadOnlyRow row1, int tableIndex1,
			[NotNull] IReadOnlyRow row2, int tableIndex2,
			[NotNull] IErrorReporting errorReporting,
			[CanBeNull] IssueCode issueCode,
			[CanBeNull] IValidRelationConstraint validRelationConstraint,
			bool reportIndividualParts,
			[CanBeNull] GeometryConstraint validIntersectionGeometryConstraint = null,
			GeometryComponent geomComponent1 = GeometryComponent.EntireGeometry,
			GeometryComponent geomComponent2 = GeometryComponent.EntireGeometry)
		{
			Assert.ArgumentNotNull(row1, nameof(row1));
			Assert.ArgumentNotNull(row2, nameof(row2));
			Assert.ArgumentNotNull(errorReporting, nameof(errorReporting));

			if (row1 == row2)
			{
				return _noError;
			}

			string errorDescription;
			if (HasFulfilledConstraint(row1, tableIndex1,
			                           row2, tableIndex2,
			                           validRelationConstraint, "Features intersect",
			                           out errorDescription))
			{
				return _noError;
			}

			IGeometry shape1 = ((IReadOnlyFeature) row1).Shape;
			IGeometry shape2 = ((IReadOnlyFeature) row2).Shape;

			var g1 = GeometryComponentUtils.GetGeometryComponent(shape1, geomComponent1);
			var g2 = GeometryComponentUtils.GetGeometryComponent(shape2, geomComponent2);

			var errorCount = 0;
			if (g1 != null && g2 != null)
			{
				foreach (IGeometry errorGeometry in
				         IntersectionUtils.GetAllIntersections(g1, g2))
				{
					if (validIntersectionGeometryConstraint == null ||
					    ! validIntersectionGeometryConstraint.IsFulfilled(errorGeometry))
					{
						errorCount += errorReporting.Report(
							errorDescription, InvolvedRowUtils.GetInvolvedRows(row1, row2),
							errorGeometry, issueCode, null, reportIndividualParts);
					}
				}
			}

			return errorCount;
		}

		public static int ReportCrossings(
			[NotNull] IReadOnlyRow row1, int tableIndex1,
			[NotNull] IReadOnlyRow row2, int tableIndex2,
			[NotNull] IErrorReporting errorReporting,
			[CanBeNull] IssueCode issueCode,
			[CanBeNull] IValidRelationConstraint validRelationConstraint,
			bool reportIndividualParts)
		{
			Assert.ArgumentNotNull(row1, nameof(row1));
			Assert.ArgumentNotNull(row2, nameof(row2));
			Assert.ArgumentNotNull(errorReporting, nameof(errorReporting));

			if (row1 == row2)
			{
				return _noError;
			}

			IGeometry g1 = ((IReadOnlyFeature) row1).Shape;
			IGeometry g2 = ((IReadOnlyFeature) row2).Shape;

			string errorDescription;
			if (HasFulfilledConstraint(row1, tableIndex1,
			                           row2, tableIndex2,
			                           validRelationConstraint, "Features cross",
			                           out errorDescription))
			{
				return _noError;
			}

			var errorCount = 0;
			foreach (IGeometry errorGeometry in GetCrossings(g1, g2))
			{
				if (errorGeometry.IsEmpty)
				{
					continue;
				}

				errorCount += errorReporting.Report(
					errorDescription, InvolvedRowUtils.GetInvolvedRows(row1, row2),
					errorGeometry, issueCode, null, reportIndividualParts);
			}

			return errorCount;

			//const bool overlap = false;
			//IGeometry intersection = TestUtils.GetIntersection(g1, g2, overlap);

			//// TODO remove boundary

			//try
			//{
			//    if (intersection.IsEmpty)
			//    {
			//        return _noError;
			//    }

			//    return errorReporting.Report("Features cross",
			//                                 intersection, reportIndividualParts,
			//                                 row1, row2);
			//}
			//finally
			//{
			//    Marshal.ReleaseComObject(intersection);
			//}
		}

		[CanBeNull]
		public static ICollection<esriGeometryDimension> ParseDimensions(
			[CanBeNull] string intersectionDimensions)
		{
			if (intersectionDimensions == null ||
			    StringUtils.IsNullOrEmptyOrBlank(intersectionDimensions))
			{
				return null;
			}

			var separators = new[] {',', ';', ' '};
			string[] tokens = intersectionDimensions.Split(
				separators, StringSplitOptions.RemoveEmptyEntries);

			if (tokens.Length == 0)
			{
				return null;
			}

			Assert.ArgumentCondition(tokens.Length <= 3, "Invalid number of dimensions: {0}",
			                         intersectionDimensions);

			var result = new List<esriGeometryDimension>(3);

			foreach (string token in tokens)
			{
				int dimension;
				if (! int.TryParse(token, out dimension))
				{
					throw new ArgumentException($"Invalid dimension: {token}");
				}

				result.Add(GetDimension(dimension));
			}

			result.Sort();

			return result;
		}

		[NotNull]
		public static string GetDimensionText(esriGeometryDimension dimension)
		{
			switch (dimension)
			{
				case esriGeometryDimension.esriGeometry0Dimension:
					return "0 (point)";

				case esriGeometryDimension.esriGeometry1Dimension:
					return "1 (linear)";

				case esriGeometryDimension.esriGeometry2Dimension:
					return "2 (area)";

				case esriGeometryDimension.esriGeometry25Dimension:
					return "2.5";

				case esriGeometryDimension.esriGeometry3Dimension:
					return "3";

				case esriGeometryDimension.esriGeometryNoDimension:
					return "none";

				default:
					throw new ArgumentOutOfRangeException(nameof(dimension));
			}
		}

		private static esriGeometryDimension GetDimension(int intersectionDimension)
		{
			switch (intersectionDimension)
			{
				case 0:
					return esriGeometryDimension.esriGeometry0Dimension;

				case 1:
					return esriGeometryDimension.esriGeometry1Dimension;

				case 2:
					return esriGeometryDimension.esriGeometry2Dimension;

				default:
					throw new ArgumentOutOfRangeException(
						$"Unsupported intersection dimension: {intersectionDimension}");
			}
		}

		[NotNull]
		private static IEnumerable<IGeometry> GetCrossings([NotNull] IGeometry g1,
		                                                   [NotNull] IGeometry g2)
		{
			var polyline1 = g1 as IPolyline;
			var polyline2 = g2 as IPolyline;

			if (polyline1 != null && polyline2 != null)
			{
				// TODO get intersection matrix also?
				// 0********
				// but: dimension restriction needs to be better tested first
				yield return IntersectionUtils.GetLineCrossings(polyline1, polyline2);
			}
			else
			{
				// TODO for line/polygon it would be more useful to get the
				// locations where the line crosses the polygon boundary (and continues)
				// Probably also for polygon/polygon
				IList<IGeometry> intersections = g1.Dimension <= g2.Dimension
					                                 ? _crossesMatrixOther.GetIntersections(g1, g2)
					                                 : _crossesMatrixOther.GetIntersections(g2, g1);

				foreach (IGeometry geometry in intersections)
				{
					yield return geometry;
				}
			}
		}

		[NotNull]
		private static IEnumerable<IGeometry> GetTouches([NotNull] IGeometry g1,
		                                                 [NotNull] IGeometry g2)
		{
			IntersectionMatrix touchesMatrix;

			if (g1.Dimension == esriGeometryDimension.esriGeometry0Dimension)
			{
				touchesMatrix = _touchesMatrixPointOther;
			}
			else if (g2.Dimension == esriGeometryDimension.esriGeometry0Dimension)
			{
				touchesMatrix = _touchesMatrixOtherPoint;
			}
			else
			{
				touchesMatrix = _touchesMatrixOther;
			}

			foreach (IGeometry geometry in touchesMatrix.GetIntersections(g1, g2))
			{
				yield return geometry;
			}
		}

		private static bool HasFulfilledConstraint(
			[NotNull] IReadOnlyRow row1, int tableIndex1,
			[NotNull] IReadOnlyRow row2, int tableIndex2,
			[CanBeNull] IValidRelationConstraint validRelationConstraint,
			[NotNull] string message,
			[NotNull] out string formattedMessage)
		{
			Assert.ArgumentNotNull(row1, nameof(row1));
			Assert.ArgumentNotNull(row2, nameof(row2));
			Assert.ArgumentNotNullOrEmpty(message, nameof(message));

			if (validRelationConstraint == null || ! validRelationConstraint.HasConstraint)
			{
				formattedMessage = message;
				return false;
			}

			string conditionMessage;
			if (validRelationConstraint.IsFulfilled(row1, tableIndex1,
			                                        row2, tableIndex2,
			                                        out conditionMessage))
			{
				formattedMessage = string.Empty;
				return true;
			}

			formattedMessage =
				$"{message} and constraint is not fulfilled: {conditionMessage}";

			return false;
		}
	}
}
