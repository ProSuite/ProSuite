using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.CreateFootprint;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	public class QaMpSinglePartFootprint : ContainerTest
	{
		private readonly ISpatialReference _spatialReference;
		private readonly string _shapeFieldName;

		private const double _defaultResolutionFactor = 100;
		private double _resolutionFactor = _defaultResolutionFactor;

		private ISpatialReference _highResolutionSpatialReference;
		private double _xyTolerance;
		private double _originalXyTolerance;
		private readonly IEnvelope _envelopeTemplate1 = new EnvelopeClass();
		private readonly IEnvelope _envelopeTemplate2 = new EnvelopeClass();
		private readonly IEnvelope _envelopeTemplateBuffer = new EnvelopeClass();

		private BufferFactory _bufferFactory;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string FootprintHasMultipleParts = "FootprintHasMultipleParts";

			public Code() : base("MpSinglePartFootprint") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMpSinglePartFootprint_0))]
		public QaMpSinglePartFootprint(
			[Doc(nameof(DocStrings.QaMpSinglePartFootprint_multiPatchClass))] [NotNull]
			IReadOnlyFeatureClass multiPatchClass)
			: base(multiPatchClass)
		{
			Assert.ArgumentNotNull(multiPatchClass, nameof(multiPatchClass));

			_spatialReference = multiPatchClass.SpatialReference;
			_shapeFieldName = multiPatchClass.ShapeFieldName;
		}

		[InternallyUsedTest]
		public QaMpSinglePartFootprint(
			[NotNull] QaMpSinglePartFootprintDefinition definition)
			: this((IReadOnlyFeatureClass) definition.MultiPatchClass)
		{
			ResolutionFactor = definition.ResolutionFactor;
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			return false;
		}

		public override bool IsQueriedTable(int tableIndex)
		{
			return false;
		}

		[UsedImplicitly]
		[TestParameter(_defaultResolutionFactor)]
		[Doc(nameof(DocStrings.QaMpSinglePartFootprint_ResolutionFactor))]
		public double ResolutionFactor
		{
			get { return _resolutionFactor; }
			set
			{
				Assert.ArgumentCondition(value >= 1, "value must be >= 1");

				_resolutionFactor = value;
			}
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			var feature = row as IReadOnlyFeature;
			if (feature == null)
			{
				return NoError;
			}

			if (_bufferFactory == null)
			{
				// initialize

				if (_resolutionFactor > 1 && _highResolutionSpatialReference == null)
				{
					_highResolutionSpatialReference =
						SpatialReferenceUtils.CreateSpatialReferenceWithMinimumTolerance(
							_spatialReference, _resolutionFactor);
				}

				ISpatialReference spatialReference = _highResolutionSpatialReference ??
				                                     _spatialReference;

				_xyTolerance = ((ISpatialReferenceTolerance) spatialReference).XYTolerance;
				_originalXyTolerance =
					((ISpatialReferenceTolerance) _spatialReference).XYTolerance;

				double densifyDeviation = _xyTolerance / 2;
				_bufferFactory = CreateBufferFactory(densifyDeviation);
			}

			IMultiPatch multiPatch;
			if (_highResolutionSpatialReference == null)
			{
				multiPatch = (IMultiPatch) feature.Shape;
			}
			else
			{
				multiPatch = (IMultiPatch) feature.ShapeCopy;
				multiPatch.SpatialReference = _highResolutionSpatialReference;
			}

			return CheckMultiPatch(feature, multiPatch);
		}

		private int CheckMultiPatch([NotNull] IReadOnlyFeature feature,
		                            [NotNull] IMultiPatch multiPatch)
		{
			IPolyline tooSmallRings = null;

			IPolygon footprint = null;
			if (IntersectionUtils.UseCustomIntersect)
			{
				footprint =
					CreateFootprintUtils.TryGetGeomFootprint(
						multiPatch, _xyTolerance, out tooSmallRings);
			}

			if (footprint == null)
			{
				footprint = GetFootprint(multiPatch, _xyTolerance, out tooSmallRings);
			}

			var errorCount = 0;

			if (tooSmallRings != null)
			{
				// buffer all vertical face segments
				double bufferDistance = _originalXyTolerance * 4;
				IPolygon bufferedSmallRings = GetBufferedSmallRings(tooSmallRings, bufferDistance);

				if (footprint != null)
				{
					var completeFootprint = (IPolygon) GeometryUtils.Union(footprint,
						bufferedSmallRings);

					if (completeFootprint.ExteriorRingCount > 1)
					{
						foreach (IPolygon smallPolygon in
						         GetSmallestDisjointPolygons(completeFootprint))
						{
							// for each small polygon, get the contained vertical segments (if any)
							// and use those to report the disjoint part. If none contained: use ring as is
							errorCount += ReportDisjointFootprintPolygon(feature,
								smallPolygon,
								multiPatch);
						}
					}
				}
				else
				{
					if (bufferedSmallRings.ExteriorRingCount > 1)
					{
						foreach (IPolygon smallPolygon in
						         GetSmallestDisjointPolygons(bufferedSmallRings))
						{
							errorCount += ReportDisjointFootprintPolygon(feature,
								smallPolygon,
								multiPatch);
						}
					}
				}
			}
			else
			{
				if (footprint != null)
				{
					if (footprint.ExteriorRingCount > 1)
					{
						foreach (IPolygon smallPolygon in GetSmallestDisjointPolygons(footprint))
						{
							errorCount += ReportDisjointFootprintPolygon(feature,
								smallPolygon,
								multiPatch);
						}
					}
				}
			}

			return errorCount;
		}

		private int ReportDisjointFootprintPolygon([NotNull] IReadOnlyFeature feature,
		                                           [NotNull] IPolygon footprintPolygon,
		                                           [NotNull] IMultiPatch multiPatch)
		{
			IGeometry errorGeometry = GetErrorGeometry(multiPatch, footprintPolygon);

			IssueCode issueCode = Codes[Code.FootprintHasMultipleParts];

			return ReportError(
				"Footprint of MultiPatch feature contains more than one part",
				InvolvedRowUtils.GetInvolvedRows(feature), errorGeometry,
				issueCode, _shapeFieldName);
		}

		[NotNull]
		private IGeometry GetErrorGeometry([NotNull] IMultiPatch multiPatch,
		                                   [NotNull] IPolygon footprintPolygon)
		{
			ISpatialReference spatialReference = multiPatch.SpatialReference;
			var result = new MultiPatchClass
			             {
				             SpatialReference = spatialReference
			             };

			GeometryUtils.AllowIndexing(footprintPolygon);
			footprintPolygon.QueryEnvelope(_envelopeTemplate1);

			object missing = Type.Missing;

			foreach (IGeometry part in GeometryUtils.GetParts(
				         (IGeometryCollection) multiPatch))
			{
				var ring = part as IRing;
				if (ring == null)
				{
					continue;
				}

				var isBeginningRing = false;
				esriMultiPatchRingType ringType = multiPatch.GetRingType(ring, ref isBeginningRing);

				if (! isBeginningRing)
				{
					continue;
				}

				ring.QueryEnvelope(_envelopeTemplate2);

				if (! ((IRelationalOperator) _envelopeTemplate1).Contains(_envelopeTemplate2))
				{
					// the ring envelope is not contained in the footprint envelope
					continue;
				}

				IPolyline polyline = CreatePolyline(new[] { ring }, spatialReference);

				if (polyline == null)
				{
					continue;
				}

				if (! ((IRelationalOperator2) footprintPolygon).ContainsEx(
					    polyline, esriSpatialRelationExEnum.esriSpatialRelationExBoundary))
				{
					continue;
				}

				// the beginning ring is inside the footprint

				// add the beginning ring
				IRing clonedRing = GeometryFactory.Clone(ring);
				((IGeometryCollection) result).AddGeometry(clonedRing, ref missing, ref missing);
				result.PutRingType(clonedRing, ringType);

				int followingRingCount = multiPatch.get_FollowingRingCount(ring);

				if (followingRingCount > 0)
				{
					// add the following rings

					var followingRings = new IRing[followingRingCount];
					GeometryUtils.GeometryBridge.QueryFollowingRings(multiPatch, ring,
						ref followingRings);

					foreach (IRing followingRing in followingRings)
					{
						var dummy = false;
						esriMultiPatchRingType followingRingType = multiPatch.GetRingType(
							followingRing,
							ref dummy);

						IRing clonedFollowingRing = GeometryFactory.Clone(followingRing);

						((IGeometryCollection) result).AddGeometry(clonedFollowingRing,
							ref missing, ref missing);

						result.PutRingType(clonedFollowingRing, followingRingType);
					}
				}
			}

			if (result.IsEmpty)
			{
				// ((IZ) footprintPolygon).SetConstantZ(multiPatch.Envelope.ZMin);
				return footprintPolygon;
			}

			return result;
		}

		[NotNull]
		private static IEnumerable<IPolygon> GetSmallestDisjointPolygons(
			[NotNull] IPolygon polygon)
		{
			IGeometryBag connectedComponents = ((IPolygon4) polygon).ConnectedComponentBag;

			List<IPolygon> polygons =
				GeometryUtils.GetParts((IGeometryCollection) connectedComponents)
				             .Cast<IPolygon>()
				             .ToList();

			return TestUtils.GetSmallestPolygons(polygons, polygons.Count - 1);
		}

		[NotNull]
		private static BufferFactory CreateBufferFactory(double densifyDeviation)
		{
			const bool densify = true;
			const bool explodeBuffers = false;
			return new BufferFactory(explodeBuffers, densify, densifyDeviation)
			       {
				       UnionOverlappingBuffers = true
			       };
		}

		[NotNull]
		private IPolygon GetBufferedSmallRings([NotNull] IPolyline tooSmallRings,
		                                       double bufferDistance)
		{
			IList<IPolygon> bufferOutput = _bufferFactory.Buffer(tooSmallRings, bufferDistance);
			Assert.AreEqual(1, bufferOutput.Count,
			                "Unexpected buffer output (distance: {0})", bufferDistance);
			IPolygon result = bufferOutput[0];

			if (! ((IZAware) result).ZAware)
			{
				tooSmallRings.QueryEnvelope(_envelopeTemplateBuffer);

				((IZAware) result).ZAware = true;
				((IZ) result).SetConstantZ(_envelopeTemplateBuffer.ZMin);
			}

			return result;
		}

		[CanBeNull]
		private IPolygon GetFootprint([NotNull] IMultiPatch multiPatch,
		                              double xyTolerance,
		                              [CanBeNull] out IPolyline tooSmallRings)
		{
			Assert.ArgumentNotNull(multiPatch, nameof(multiPatch));

			var notLargeEnoughRings = new List<IRing>();
			IMultiPatch largeEnoughRingsMultiPatch = null;

			object missing = Type.Missing;

			foreach (IGeometry part in GeometryUtils.GetParts(
				         (IGeometryCollection) multiPatch))
			{
				var ring = part as IRing;
				if (ring == null)
				{
					continue;
				}

				var isBeginning = false;
				esriMultiPatchRingType ringType = multiPatch.GetRingType(ring, ref isBeginning);

				if (IsTooSmall(ring, xyTolerance))
				{
					if (isBeginning)
					{
						// throw away following rings (holes) that are too small
						notLargeEnoughRings.Add(ring);
					}
				}
				else
				{
					if (largeEnoughRingsMultiPatch == null)
					{
						largeEnoughRingsMultiPatch = new MultiPatchClass
						                             {
							                             SpatialReference =
								                             multiPatch.SpatialReference
						                             };
						((IZAware) largeEnoughRingsMultiPatch).ZAware = true;
					}

					IRing clone = GeometryFactory.Clone(ring);
					((IGeometryCollection) largeEnoughRingsMultiPatch).AddGeometry(clone,
						ref missing,
						ref missing);

					largeEnoughRingsMultiPatch.PutRingType(clone, ringType);
				}
			}

			ISpatialReference spatialReference = multiPatch.SpatialReference;

			tooSmallRings = notLargeEnoughRings.Count > 0
				                ? CreatePolyline(notLargeEnoughRings, spatialReference)
				                : null;

			return largeEnoughRingsMultiPatch != null
				       ? GeometryFactory.CreatePolygon(largeEnoughRingsMultiPatch)
				       : null;
		}

		[CanBeNull]
		private static IPolyline CreatePolyline([NotNull] IEnumerable<IRing> rings,
		                                        [NotNull] ISpatialReference spatialReference)
		{
			const bool makeZAware = true;
			const bool makeMAware = false;
			IPolyline result = GeometryFactory.CreatePolyline(spatialReference,
			                                                  makeZAware, makeMAware);

			var tooSmallRingSegments = (ISegmentCollection) result;
			foreach (IRing ring in rings)
			{
				tooSmallRingSegments.AddSegmentCollection(
					(ISegmentCollection) GeometryFactory.Clone(ring));
			}

			// don't split at intersections/overlaps
			// merge paths where an end point is shared (without merging: IPolyline6.SimplifyNonPlanar())
			result.SimplifyNetwork();

			return result.IsEmpty
				       ? null
				       : result;
		}

		private bool IsTooSmall([NotNull] IRing ring, double xyTolerance)
		{
			IPolygon polygon = GeometryFactory.CreatePolygon(ring);

			GeometryUtils.Simplify(polygon, true, false);

			if (polygon.IsEmpty)
			{
				return true;
			}

			ring.QueryEnvelope(_envelopeTemplate1);

			double ringWidth = _envelopeTemplate1.Width;
			double ringHeight = _envelopeTemplate1.Height;

			polygon.QueryEnvelope(_envelopeTemplate1);

			double simplifiedWidth = _envelopeTemplate1.Width;
			double simplifiedHeight = _envelopeTemplate1.Height;

			double maximumReduction = xyTolerance * 4;

			return ringWidth - simplifiedWidth > maximumReduction ||
			       ringHeight - simplifiedHeight > maximumReduction;
		}
	}
}
