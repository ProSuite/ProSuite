using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.PointEnumerators;
using ProSuite.QA.Tests.Properties;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using IPnt = ProSuite.Commons.Geom.IPnt;
using Pnt = ProSuite.Commons.Geom.Pnt;
using ProSuite.Commons.AO.Geodatabase;

namespace ProSuite.QA.Tests
{
	public class VertexCoincidenceChecker
	{
		[NotNull] private readonly IErrorReporting _errorReporting;
		private readonly double _maximumXYTolerance;

		[NotNull] private readonly Func<double, string, double, string, string>
			_formatComparisonFunction;

		private double _pointTolerance;
		private double _edgeTolerance;

		private const int _noError = 0;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NoVertexOnNearbyEdge_SameFeature =
				"NoVertexOnNearbyEdge.SameFeature";

			public const string NoVertexOnNearbyEdge_DifferentFeature =
				"NoVertexOnNearbyEdge.DifferentFeature";

			public const string NearbyVertexNotCoincident_DifferentFeature =
				"NearbyVertexNotCoincident.DifferentFeature";

			public const string NearbyVertexNotCoincident_SameFeature =
				"NearbyVertexNotCoincident.SameFeature";

			public const string ZDifference_CoincidentVertex_SameFeature =
				"ZDifference.CoincidentVertex.SameFeature";

			public const string ZDifference_CoincidentVertex_DifferentFeature =
				"ZDifference.CoincidentVertex.DifferentFeature";

			public const string ZDifference_CoincidentEdge_SameFeature =
				"ZDifference.CoincidentEdge.SameFeature";

			public const string ZDifference_CoincidentEdge_DifferentFeature =
				"ZDifference.CoincidentEdge.DifferentFeature";

			public const string NearbyEdgeNotPassingThroughVertex_DifferentFeature =
				"NearbyEdgeNotPassingThroughVertex.DifferentFeature";

			public const string NearbyEdgeNotPassingThroughVertex_SameFeature =
				"NearbyEdgeNotPassingThroughVertex.SameFeature";

			public Code() : base("VertexCoincidence") { }
		}

		#endregion

		public VertexCoincidenceChecker(
			[NotNull] IErrorReporting errorReporting,
			[NotNull] Func<double, string, double, string, string> formatComparisonFunction,
			double maximumXYTolerance)
		{
			Assert.ArgumentNotNull(errorReporting, nameof(errorReporting));
			Assert.ArgumentNotNull(formatComparisonFunction, nameof(formatComparisonFunction));
			Assert.ArgumentCondition(maximumXYTolerance > 0, "maximum xy tolerance must be > 0");

			_errorReporting = errorReporting;
			_formatComparisonFunction = formatComparisonFunction;
			_maximumXYTolerance = maximumXYTolerance;
		}

		public double PointTolerance
		{
			get { return _pointTolerance; }
			set
			{
				_pointTolerance = value;
				UpdateSearchDistance();
			}
		}

		public double EdgeTolerance
		{
			get { return _edgeTolerance; }
			set
			{
				_edgeTolerance = value;
				UpdateSearchDistance();
			}
		}

		public double CoincidenceTolerance { get; set; }

		/// <summary>
		/// value &gt; 0: if z difference is greater -> significant -> ignore <para/>
		/// value == 0: ignore z (any difference is significant/allowed)<para/>
		/// value &lt; 0: any z difference (greater than ZCoincidenceTolerance) is unallowed
		/// </summary>
		public double ZTolerance { get; set; }

		private bool CheckZ => Math.Abs(ZTolerance) > double.Epsilon;

		/// <summary>
		/// value &lt; 0: use max(fclass1.ztolerance, fclass2.ztolerance)<para/>
		/// value == 0: zero tolerance<para/>
		/// value > 0: treat as z-coincident if z difference is smaller than value
		/// </summary>
		public double ZCoincidenceTolerance { get; set; }

		public bool ReportCoordinates { get; set; }

		private double GetZCoincidenceTolerance([NotNull] IEnumerable<IReadOnlyFeature> features)
		{
			if (ZCoincidenceTolerance >= 0)
			{
				return ZCoincidenceTolerance;
			}

			double tolerance = 0;
			foreach (IReadOnlyFeature feature in features)
			{
				ISpatialReference sr = feature.Shape.SpatialReference;
				var srt = (ISpatialReferenceTolerance) sr;
				if (srt.ZToleranceValid == esriSRToleranceEnum.esriSRToleranceOK)
				{
					tolerance = Math.Max(tolerance, srt.ZTolerance);
				}
			}

			return tolerance;
		}

		public bool Is3D { get; set; }

		public double SearchDistance { get; private set; }

		public bool RequireVertexOnNearbyEdge { get; set; }

		public bool VerifyWithinFeature { get; set; }

		public int CheckCoincidence([NotNull] IPointsEnumerator pointsEnumerator,
		                            [NotNull] IReadOnlyFeature nearFeature)
		{
			NearFeatureCoincidence nearFeatureCoincidence =
				CreateCoincidenceChecker(nearFeature);

			var errorCount = 0;

			foreach (Pnt point in pointsEnumerator.GetPoints())
			{
				errorCount += CheckCoincidence(pointsEnumerator.Feature,
				                               point,
				                               pointsEnumerator.SpatialReference,
				                               pointsEnumerator.XYTolerance,
				                               nearFeatureCoincidence);
			}

			return errorCount;
		}

		private void UpdateSearchDistance()
		{
			SearchDistance =
				Math.Max(
					Math.Max(_pointTolerance, _edgeTolerance),
					_maximumXYTolerance);
		}

		[NotNull]
		private string FormatComparison(double closestDistance, double tolerance)
		{
			return _formatComparisonFunction(closestDistance, "<", tolerance, "N2");
		}

		private int CheckCoincidence([NotNull] IReadOnlyFeature feature,
		                             [NotNull] Pnt point,
		                             [NotNull] ISpatialReference spatialReference,
		                             double xyTolerance,
		                             [NotNull] NearFeatureCoincidence nearFeatureCoincidence)
		{
			double pointTolerance = _pointTolerance < 0
				                        ? xyTolerance
				                        : _pointTolerance;
			double edgeTolerance = _edgeTolerance < 0
				                       ? xyTolerance
				                       : _edgeTolerance;

			if (feature == nearFeatureCoincidence.Feature)
			{
				// same feature

				if (! VerifyWithinFeature)
				{
					return _noError;
				}

				return CheckCoincidenceSameFeature(point, feature,
				                                   GetProximities(point, nearFeatureCoincidence),
				                                   pointTolerance, edgeTolerance,
				                                   spatialReference);
			}

			return CheckCoincidenceDifferentFeatures(point, feature,
			                                         nearFeatureCoincidence.Feature,
			                                         GetProximities(point,
			                                                        nearFeatureCoincidence),
			                                         pointTolerance, edgeTolerance,
			                                         spatialReference);
		}

		[NotNull]
		private IEnumerable<Proximity> GetProximities(
			[NotNull] Pnt point,
			[NotNull] NearFeatureCoincidence nearFeatureCoincidence)
		{
			IBox searchBox = CreateSearchBox(point);

			return nearFeatureCoincidence.GetProximities(point, Is3D, searchBox);
		}

		private bool IsInvalidDeltaZ(double deltaZ, double zCoincidenceTolerance)
		{
			double absDeltaZ = Math.Abs(deltaZ);
			return absDeltaZ > zCoincidenceTolerance &&
			       (ZTolerance < 0 || absDeltaZ < ZTolerance);
		}

		private int CheckCoincidenceDifferentFeatures(
			[NotNull] IPnt point,
			[NotNull] IReadOnlyFeature feature,
			[NotNull] IReadOnlyFeature nearFeature,
			[NotNull] IEnumerable<Proximity> proximities,
			double pointTolerance,
			double edgeTolerance,
			[NotNull] ISpatialReference spatialReference)
		{
			double closestPointDistance = double.MaxValue;
			double closestEdgeDistance = double.MaxValue;
			double closestEdgeDeltaZ = 0;

			double epsilon =
				MathUtils.GetDoubleSignificanceEpsilon(Math.Max(Math.Abs(point.X),
				                                                Math.Abs(point.Y)));
			Pnt closestPoint = null;

			foreach (Proximity proximity in proximities)
			{
				if (pointTolerance > 0)
				{
					Pnt nearestVertex = proximity.GetNearestVertex();
					double vertexDistance = proximity.GetPointDistance(nearestVertex);

					if (MathUtils.IsWithinTolerance(vertexDistance, pointTolerance, epsilon))
					{
						closestPointDistance = Math.Min(vertexDistance, closestPointDistance);
						closestPoint = nearestVertex;

						if (MathUtils.IsWithinTolerance(closestPointDistance, CoincidenceTolerance,
						                                epsilon))
						{
							// there is at least one close enough point on the near feature

							if (CheckZ)
							{
								// ReSharper disable once ConvertToConstant.Local
								double deltaZ = nearestVertex[2] - proximity.Point[2];
								double zCoincidenceTolerance =
									GetZCoincidenceTolerance(new[] {feature, nearFeature});

								// get delta Z (also if is3D = false !!!!)
								if (IsInvalidDeltaZ(deltaZ, zCoincidenceTolerance))
								{
									return ReportZDiffersCoincidentVertex(
										point, zCoincidenceTolerance,
										deltaZ,
										spatialReference, feature, nearFeature);
								}
							}

							return _noError;
						}
					}
				}

				if (edgeTolerance > 0)
				{
					bool isVertex;
					IPnt nearestEdgePoint = proximity.GetNearestEdgePoint(out isVertex);
					double edgeDistance = proximity.GetPointDistance(nearestEdgePoint);

					if (! isVertex && edgeDistance < closestEdgeDistance)
					{
						closestEdgeDistance = edgeDistance;

						if (CheckZ)
						{
							closestEdgeDeltaZ = nearestEdgePoint[2] - proximity.Point[2];
						}
					}
				}
			}

			if (MathUtils.IsWithinTolerance(closestPointDistance, pointTolerance, epsilon) &&
			    ! MathUtils.IsWithinTolerance(closestPointDistance, CoincidenceTolerance,
			                                  epsilon))
			{
				Assert.NotNull(closestPoint);
				return ReportNearbyVertexNotCoincident(point, closestPoint,
				                                       pointTolerance,
				                                       closestPointDistance,
				                                       spatialReference,
				                                       feature, nearFeature);
			}

			if (MathUtils.IsWithinTolerance(closestEdgeDistance, edgeTolerance, epsilon))
			{
				// there is a nearby edge within the edge tolerance

				if (RequireVertexOnNearbyEdge)
				{
					// and that is never allowed (without a coincident vertex)

					// the nearby edge does not have a vertex
					return ReportNoVertexOnNearbyEdge(point,
					                                  edgeTolerance, closestEdgeDistance,
					                                  spatialReference, feature, nearFeature);
				}

				if (! MathUtils.IsWithinTolerance(closestEdgeDistance, CoincidenceTolerance,
				                                  epsilon))
				{
					// the nearby edge does not pass through the vertex
					return ReportNearbyEdgeNotPassingThroughVertex(point, edgeTolerance,
					                                               closestEdgeDistance,
					                                               spatialReference, feature,
					                                               nearFeature);
				}

				if (CheckZ)
				{
					double zCoincidenceTolerance =
						GetZCoincidenceTolerance(new[] {feature, nearFeature});
					if (IsInvalidDeltaZ(closestEdgeDeltaZ, zCoincidenceTolerance))
					{
						return ReportZDiffersCoincidentEdge(point, zCoincidenceTolerance,
						                                    closestEdgeDeltaZ,
						                                    spatialReference, feature,
						                                    nearFeature);
					}
				}

				// the closest edge on the near feature is near enough
				return _noError;
			}

			return _noError;
		}

		private int CheckCoincidenceSameFeature(
			[NotNull] IPnt point,
			[NotNull] IReadOnlyFeature feature,
			[NotNull] IEnumerable<Proximity> proximities,
			double pointTolerance,
			double edgeTolerance,
			[NotNull] ISpatialReference spatialReference)
		{
			double closestEdgeDistance = double.MaxValue;
			double closestEdgeDeltaZ = 0;

			double? zCoincidenceTolerance = null;

			foreach (Proximity proximity in proximities)
			{
				if (pointTolerance > 0)
				{
					IPnt nearestVertex = proximity.GetNearestVertex();
					double vertexDistance = proximity.GetPointDistance(nearestVertex);

					if (vertexDistance < pointTolerance)
					{
						if (Math.Abs(vertexDistance) < double.Epsilon)
						{
							// probably the same vertex (can't be sure though!)
						}
						else if (vertexDistance > CoincidenceTolerance)
						{
							// not coincident
							return ReportNearbyVertexNotCoincident(point, nearestVertex,
							                                       pointTolerance,
							                                       vertexDistance, spatialReference,
							                                       feature);
						}

						if (CheckZ)
						{
							double deltaZ = nearestVertex[2] - proximity.Point[2];
							zCoincidenceTolerance = zCoincidenceTolerance ??
							                        GetZCoincidenceTolerance(new[] {feature});
							if (IsInvalidDeltaZ(deltaZ, zCoincidenceTolerance.Value))
							{
								return ReportZDiffersCoincidentVertex(
									point, zCoincidenceTolerance.Value,
									deltaZ,
									spatialReference, feature);
							}
						}
					}
				}

				if (edgeTolerance > 0)
				{
					bool isVertex;
					IPnt nearestEdgePoint = proximity.GetNearestEdgePoint(out isVertex);
					double edgeDistance = proximity.GetPointDistance(nearestEdgePoint);

					if (edgeDistance < edgeTolerance)
					{
						if (! isVertex && edgeDistance < closestEdgeDistance)
						{
							closestEdgeDistance = edgeDistance;
							if (CheckZ)
							{
								closestEdgeDeltaZ = nearestEdgePoint[2] - proximity.Point[2];
							}
						}
					}
				}
			}

			if (closestEdgeDistance < edgeTolerance)
			{
				// there is a nearby edge within the edge tolerance

				if (RequireVertexOnNearbyEdge)
				{
					// and that is never allowed (without a coincident vertex)
					return ReportNoVertexOnNearbyEdge(point, edgeTolerance,
					                                  closestEdgeDistance,
					                                  spatialReference, feature);
				}

				if (closestEdgeDistance > CoincidenceTolerance)
				{
					// the nearby edge does not pass through the vertex
					return ReportNearbyEdgeNotPassingThroughVertex(
						point, edgeTolerance, closestEdgeDistance,
						spatialReference, feature);
				}

				if (CheckZ)
				{
					zCoincidenceTolerance = zCoincidenceTolerance ??
					                        GetZCoincidenceTolerance(new[] {feature});

					if (IsInvalidDeltaZ(closestEdgeDeltaZ, zCoincidenceTolerance.Value))
					{
						return ReportZDiffersCoincidentEdge(point, zCoincidenceTolerance.Value,
						                                    closestEdgeDeltaZ,
						                                    spatialReference, feature);
					}
				}
			}

			return _noError;
		}

		private int ReportNearbyVertexNotCoincident(
			[NotNull] IPnt point,
			[NotNull] IPnt nearbyVertex,
			double pointTolerance,
			double vertexDistance,
			[NotNull] ISpatialReference spatialReference,
			params IReadOnlyFeature[] features)
		{
			bool sameFeature = features.Length == 1;

			string code = sameFeature
				              ? Code.NearbyVertexNotCoincident_SameFeature
				              : Code.NearbyVertexNotCoincident_DifferentFeature;

			string format = sameFeature
				                ? LocalizableStrings
					                .VertexCoincidenceChecker_NearbyVertexNotCoincident_SameFeature
				                : LocalizableStrings
					                .VertexCoincidenceChecker_NearbyVertexNotCoincident_DifferentFeature;
			string description = string.Format(format,
			                                   FormatComparison(vertexDistance, pointTolerance));

			if (ReportCoordinates)
			{
				description = description +
				              string.Format(
					              LocalizableStrings
						              .VertexCoincidenceChecker_NearbyVertexNotCoincident_ReportCoordinatesSuffix,
					              point.X, point.Y, nearbyVertex.X, nearbyVertex.Y);
			}

			return _errorReporting.Report(description,
			                              CreateErrorGeometry(point, spatialReference),
			                              Codes[code],
			                              GetAffectedComponent(features),
			                              new object[] {vertexDistance},
			                              features.Cast<IReadOnlyRow>().ToArray());
		}

		private int ReportZDiffersCoincidentVertex(
			[NotNull] IPnt point,
			double zCoincidenceTolerance, double deltaZ,
			[NotNull] ISpatialReference spatialReference,
			params IReadOnlyFeature[] features)
		{
			bool sameFeature = features.Length == 1;

			string format =
				sameFeature
					? LocalizableStrings
						.VertexCoincidenceChecker_ZDifference_CoincidentVertex_SameFeature
					: LocalizableStrings
						.VertexCoincidenceChecker_ZDifference_CoincidentVertex_DifferentFeature;
			string code = sameFeature
				              ? Code.ZDifference_CoincidentVertex_SameFeature
				              : Code.ZDifference_CoincidentVertex_DifferentFeature;

			string description = string.Format(
				format,
				_formatComparisonFunction(Math.Abs(deltaZ), ">", zCoincidenceTolerance, "N2"));

			return _errorReporting.Report(description,
			                              CreateErrorGeometry(point, spatialReference),
			                              Codes[code],
			                              GetAffectedComponent(features),
			                              new object[] {Math.Abs(deltaZ)},
			                              features.Cast<IReadOnlyRow>().ToArray());
		}

		private int ReportNearbyEdgeNotPassingThroughVertex(
			[NotNull] IPnt point,
			double edgeTolerance,
			double edgeDistance,
			[NotNull] ISpatialReference spatialReference,
			params IReadOnlyFeature[] features)
		{
			bool sameFeature = features.Length == 1;

			string format =
				sameFeature
					? LocalizableStrings
						.VertexCoincidenceChecker_NearbyEdgeNotPassingThroughVertex_SameFeature
					: LocalizableStrings
						.VertexCoincidenceChecker_NearbyEdgeNotPassingThroughVertex_DifferentFeature;
			string code = sameFeature
				              ? Code.NearbyEdgeNotPassingThroughVertex_SameFeature
				              : Code.NearbyEdgeNotPassingThroughVertex_DifferentFeature;

			string description = string.Format(format,
			                                   FormatComparison(edgeDistance, edgeTolerance));

			if (ReportCoordinates)
			{
				description = description +
				              string.Format(
					              LocalizableStrings
						              .VertexCoincidenceChecker_NearbyEdgeNotPassingThroughVertex_ReportCoordinatesSuffix,
					              point.X, point.Y);
			}

			return _errorReporting.Report(description,
			                              CreateErrorGeometry(point, spatialReference),
			                              Codes[code],
			                              GetAffectedComponent(features),
			                              new object[] {edgeDistance},
			                              features.Cast<IReadOnlyRow>().ToArray());
		}

		private int ReportZDiffersCoincidentEdge(
			[NotNull] IPnt point,
			double zTolerance, double deltaZ,
			[NotNull] ISpatialReference spatialReference,
			params IReadOnlyFeature[] features)
		{
			bool sameFeature = features.Length == 1;

			string format =
				sameFeature
					? LocalizableStrings
						.VertexCoincidenceChecker_ZDifference_CoincidentEdge_SameFeature
					: LocalizableStrings
						.VertexCoincidenceChecker_ZDifference_CoincidentEdge_DifferentFeature;
			string code = sameFeature
				              ? Code.ZDifference_CoincidentEdge_SameFeature
				              : Code.ZDifference_CoincidentEdge_DifferentFeature;

			string description = string.Format(format,
			                                   _formatComparisonFunction(Math.Abs(deltaZ), ">",
			                                                             zTolerance, "N2"));

			return _errorReporting.Report(description,
			                              CreateErrorGeometry(point, spatialReference),
			                              Codes[code],
			                              GetAffectedComponent(features),
			                              new object[] {Math.Abs(deltaZ)},
			                              features.Cast<IReadOnlyRow>().ToArray());
		}

		private int ReportNoVertexOnNearbyEdge(
			[NotNull] IPnt point,
			double edgeTolerance,
			double edgeDistance,
			[NotNull] ISpatialReference spatialReference,
			params IReadOnlyFeature[] features)
		{
			bool sameFeature = features.Length == 1;

			string format =
				sameFeature
					? LocalizableStrings.VertexCoincidenceChecker_NoVertexOnNearbyEdge_SameFeature
					: LocalizableStrings
						.VertexCoincidenceChecker_NoVertexOnNearbyEdge_DifferentFeature;
			string code = sameFeature
				              ? Code.NoVertexOnNearbyEdge_SameFeature
				              : Code.NoVertexOnNearbyEdge_DifferentFeature;

			string description = string.Format(format,
			                                   FormatComparison(edgeDistance, edgeTolerance));

			if (ReportCoordinates)
			{
				description = description +
				              string.Format(
					              LocalizableStrings
						              .VertexCoincidenceChecker_NoVertexOnNearbyEdge_ReportCoordinatesSuffix,
					              point.X, point.Y);
			}

			return _errorReporting.Report(description,
			                              CreateErrorGeometry(point, spatialReference),
			                              Codes[code],
			                              GetAffectedComponent(features),
			                              new object[] {edgeDistance},
			                              features.Cast<IReadOnlyRow>().ToArray());
		}

		[CanBeNull]
		private static string GetAffectedComponent(
			[NotNull] IEnumerable<IReadOnlyFeature> features)
			=> TestUtils.GetShapeFieldNames(features);

		[NotNull]
		private IBox CreateSearchBox([NotNull] IPnt point)
		{
			double e = SearchDistance;
			return GeomUtils.CreateBox(point.X - e, point.Y - e,
			                           point.X + e, point.Y + e);
		}

		[NotNull]
		private static IPoint CreateErrorGeometry(
			[NotNull] IPnt point,
			[NotNull] ISpatialReference spatialReference)
		{
			IPoint result = new PointClass();

			result.PutCoords(point.X, point.Y);
			result.Z = point[2];
			result.SpatialReference = spatialReference;

			return result;
		}

		[NotNull]
		private static NearFeatureCoincidence CreateCoincidenceChecker(
			[NotNull] IReadOnlyFeature feature)
		{
			var indexedSegmentsFeature = feature as IIndexedSegmentsFeature;
			if (indexedSegmentsFeature != null)
			{
				return new IndexedSegmentsNearFeatureCoincidence(
					feature, indexedSegmentsFeature.IndexedSegments);
			}

			IGeometry shape = feature.Shape;
			esriGeometryType shapeType = shape.GeometryType;

			if (shapeType == esriGeometryType.esriGeometryPolygon ||
			    shapeType == esriGeometryType.esriGeometryPolyline)
			{
				var indexedPolycurve = new IndexedPolycurve((IPointCollection4) shape);
				return new IndexedSegmentsNearFeatureCoincidence(feature, indexedPolycurve);
			}

			if (shapeType == esriGeometryType.esriGeometryMultiPatch)
			{
				IIndexedMultiPatch indexedMultiPatch =
					QaGeometryUtils.CreateIndexedMultiPatch((IMultiPatch) shape);
				return new IndexedSegmentsNearFeatureCoincidence(feature,
				                                                 indexedMultiPatch);
			}

			if (shapeType == esriGeometryType.esriGeometryPoint)
			{
				return new PointNearFeatureCoincidence(feature, (IPoint) shape);
			}

			if (shapeType == esriGeometryType.esriGeometryMultipoint)
			{
				return new MultipointNearFeatureCoincidence(feature, (IMultipoint) shape);
			}

			throw new InvalidOperationException("Unhandled geometry type: " +
			                                    feature.Shape.GeometryType);
		}

		#region Nested types

		private abstract class NearFeatureCoincidence
		{
			protected NearFeatureCoincidence([NotNull] IReadOnlyFeature feature)
			{
				Assert.ArgumentNotNull(feature, nameof(feature));

				Feature = feature;
			}

			[NotNull]
			public IReadOnlyFeature Feature { get; }

			[NotNull]
			public abstract IEnumerable<Proximity> GetProximities([NotNull] Pnt point,
			                                                      bool as3D,
			                                                      [NotNull] IBox box);
		}

		private abstract class Proximity
		{
			protected Proximity([NotNull] Pnt point, bool as3D)
			{
				Point = point;
				As3D = as3D;
			}

			[NotNull]
			public Pnt Point { get; }

			protected bool As3D { get; }

			public abstract Pnt GetNearestVertex();

			public abstract IPnt GetNearestEdgePoint(out bool isVertex);

			public double GetPointDistance([NotNull] IPnt vertex)
			{
				Pnt difference = Point - vertex;

				double distance2D;
				if (MathUtils.AreSignificantDigitsEqual(Point.X, vertex.X))
				{
					distance2D = MathUtils.AreSignificantDigitsEqual(Point.Y, vertex.Y)
						             ? 0
						             : Math.Abs(difference.Y);
				}
				else if (MathUtils.AreSignificantDigitsEqual(Point.Y, vertex.Y))
				{
					distance2D = MathUtils.AreSignificantDigitsEqual(Point.X, vertex.X)
						             ? 0
						             : Math.Abs(difference.X);
				}
				else
				{
					// both differences are significant
					double distanceSquared = difference.X * difference.X +
					                         difference.Y * difference.Y;

					if (As3D)
					{
						if (! MathUtils.AreSignificantDigitsEqual(Point[2], vertex[2]))
						{
							distanceSquared += difference[2] * difference[2];
						}
					}

					return Math.Sqrt(distanceSquared);
				}

				if (As3D)
				{
					double distanceSquared = distance2D * distance2D;

					if (! MathUtils.AreSignificantDigitsEqual(Point[2], vertex[2]))
					{
						distanceSquared += difference[2] * difference[2];
					}

					return Math.Sqrt(distanceSquared);
				}

				return distance2D;
			}
		}

		private class IndexedSegmentsNearFeatureCoincidence : NearFeatureCoincidence
		{
			[NotNull] private readonly IIndexedSegments _indexedSegments;

			public IndexedSegmentsNearFeatureCoincidence(
				[NotNull] IReadOnlyFeature feature,
				[NotNull] IIndexedSegments indexedSegments)
				: base(feature)
			{
				Assert.ArgumentNotNull(indexedSegments, nameof(indexedSegments));

				_indexedSegments = indexedSegments;
			}

			public override IEnumerable<Proximity> GetProximities(Pnt point,
			                                                      bool as3D,
			                                                      IBox box)
			{
				foreach (SegmentProxy segmentProxy in _indexedSegments.GetSegments(box))
				{
					if (! segmentProxy.Extent.Intersects(box))
					{
						continue;
					}

					yield return new SegmentProximity(point, as3D, segmentProxy);
				}
			}
		}

		private class PointNearFeatureCoincidence : NearFeatureCoincidence
		{
			[NotNull] private readonly Pnt _point;

			public PointNearFeatureCoincidence([NotNull] IReadOnlyFeature feature,
			                                   [NotNull] IPoint point)
				: base(feature)
			{
				_point = QaGeometryUtils.CreatePoint3D(point);
			}

			public override IEnumerable<Proximity> GetProximities(Pnt point,
			                                                      bool as3D,
			                                                      IBox box)
			{
				if (box.Contains((IPnt) _point))
				{
					yield return new PointProximity(point, as3D, _point);
				}
			}
		}

		private class MultipointNearFeatureCoincidence : NearFeatureCoincidence
		{
			[NotNull] private readonly WKSPointZ[] _wksPoints;

			public MultipointNearFeatureCoincidence([NotNull] IReadOnlyFeature feature,
			                                        IMultipoint multipoint)
				: base(feature)
			{
				var points = (IPointCollection4) multipoint;
				_wksPoints = new WKSPointZ[points.PointCount];
				GeometryUtils.QueryWKSPointZs(points, _wksPoints);
			}

			public override IEnumerable<Proximity> GetProximities(Pnt point,
			                                                      bool as3D,
			                                                      IBox box)
			{
				foreach (WKSPointZ wksPoint in _wksPoints)
				{
					Pnt part = QaGeometryUtils.CreatePoint3D(wksPoint);

					if (box.Contains((IPnt) part))
					{
						yield return new PointProximity(point, as3D, part);
					}
				}
			}
		}

		private class SegmentProximity : Proximity
		{
			[NotNull] private readonly SegmentProxy _segmentProxy;

			private double? _fraction;

			public SegmentProximity([NotNull] Pnt point,
			                        bool as3D,
			                        [NotNull] SegmentProxy segmentProxy)
				: base(point, as3D)
			{
				_segmentProxy = segmentProxy;
			}

			public override Pnt GetNearestVertex()
			{
				double fraction = Fraction;

				Pnt vertex = fraction < 0.5
					             ? _segmentProxy.GetStart(as3D: true)
					             : _segmentProxy.GetEnd(as3D: true);

				return vertex;
			}

			public override IPnt GetNearestEdgePoint(out bool isVertex)
			{
				double fraction = Fraction;

				if (fraction < 0 || fraction > 1)
				{
					isVertex = true;
					return GetNearestVertex();
				}

				isVertex = fraction <= 0 || fraction >= 1;
				IPnt edgePoint = _segmentProxy.GetPointAt(fraction, as3D: true);
				return edgePoint;
			}

			private double Fraction
			{
				get
				{
					if (! _fraction.HasValue)
					{
						_fraction =
							SegmentUtils.GetClosestPointFraction(_segmentProxy, Point, As3D);
					}

					return _fraction.Value;
				}
			}
		}

		private class PointProximity : Proximity
		{
			[NotNull] private readonly Pnt _proximityPoint;

			public PointProximity([NotNull] Pnt point,
			                      bool as3D,
			                      [NotNull] Pnt proximityPoint)
				: base(point, as3D)
			{
				_proximityPoint = proximityPoint;
			}

			public override Pnt GetNearestVertex()
			{
				return _proximityPoint;
			}

			public override IPnt GetNearestEdgePoint(out bool isVertex)
			{
				isVertex = true;
				return _proximityPoint;
			}
		}

		#endregion
	}
}
