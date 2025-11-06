using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
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
	public class QaMpFootprintHoles : ContainerTest
	{
		private readonly InnerRingHandling _innerRingHandling;
		private readonly IEnvelope _envelopeTemplate = new EnvelopeClass();
		private readonly ISpatialReference _spatialReference;

		private double _horizontalZTolerance;
		private const double _defaultResolutionFactor = 1;
		private double _resolutionFactor = _defaultResolutionFactor;
		private const double _defaultMinimumArea = -1;
		private double _minimumArea;

		private ISpatialReference _minimumToleranceSpatialReference;
		private readonly string _shapeFieldName;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string MultiPatchHasOnlyVerticalPatchesFormingARing =
				"MultiPatchHasOnlyVerticalPatchesFormingARing";

			public const string VerticalPatchNotCompletelyWithinFootprint =
				"VerticalPatchNotCompletelyWithinFootprint";

			public const string FootprintHasInnerRing = "FootprintHasInnerRing";

			public Code() : base("MpFootprintHoles") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMpFootprintHoles_0))]
		public QaMpFootprintHoles(
			[Doc(nameof(DocStrings.QaMpFootprintHoles_multiPatchClass))] [NotNull]
			IReadOnlyFeatureClass multiPatchClass,
			[Doc(nameof(DocStrings.QaMpFootprintHoles_innerRingHandling))]
			InnerRingHandling innerRingHandling)
			: base(multiPatchClass)
		{
			Assert.ArgumentNotNull(multiPatchClass, nameof(multiPatchClass));
			Assert.ArgumentCondition(
				multiPatchClass.ShapeType == esriGeometryType.esriGeometryMultiPatch,
				"Multipatch feature class expected");

			_innerRingHandling = innerRingHandling;
			_spatialReference = multiPatchClass.SpatialReference;
			_shapeFieldName = multiPatchClass.ShapeFieldName;
		}

		[InternallyUsedTest]
		public QaMpFootprintHoles(
			[NotNull] QaMpFootprintHolesDefinition definition)
			: this((IReadOnlyFeatureClass) definition.MultiPatchClass,
			       definition.InnerRingHandling)
		{
			HorizontalZTolerance = definition.HorizontalZTolerance;
			ResolutionFactor = definition.ResolutionFactor;
			MinimumArea = definition.MinimumArea;
			ReportVerticalPatchesNotCompletelyWithinFootprint =
				definition.ReportVerticalPatchesNotCompletelyWithinFootprint;
		}

		[UsedImplicitly]
		[TestParameter(0)]
		[Doc(nameof(DocStrings.QaMpFootprintHoles_HorizontalZTolerance))]
		public double HorizontalZTolerance
		{
			get { return _horizontalZTolerance; }
			set
			{
				Assert.ArgumentCondition(value >= 0, "value must be >= 0");

				_horizontalZTolerance = value;
			}
		}

		[UsedImplicitly]
		[TestParameter(_defaultResolutionFactor)]
		[Doc(nameof(DocStrings.QaMpFootprintHoles_ResolutionFactor))]
		public double ResolutionFactor
		{
			get { return _resolutionFactor; }
			set
			{
				Assert.ArgumentCondition(value >= 1, "value must be >= 1");

				_resolutionFactor = value;
			}
		}

		[UsedImplicitly]
		[TestParameter(_defaultMinimumArea)]
		[Doc(nameof(DocStrings.QaMpFootprintHoles_MinimumArea))]
		public double MinimumArea
		{
			get { return _minimumArea; }
			set
			{
				Assert.ArgumentCondition(
					Math.Abs(value - (-1)) < double.Epsilon || value > 0,
					"value must be either -1 (no area limit) or greater than 0");

				_minimumArea = value;
			}
		}

		[UsedImplicitly]
		[TestParameter(true)]
		[Doc(nameof(DocStrings
			            .QaMpFootprintHoles_ReportVerticalPatchesNotCompletelyWithinFootprint))]
		public bool ReportVerticalPatchesNotCompletelyWithinFootprint { get; set; } =
			true;

		public override bool IsQueriedTable(int tableIndex)
		{
			return false;
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			return false;
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			var feature = row as IReadOnlyFeature;
			if (feature == null)
			{
				return NoError;
			}

			var multiPatch = feature.Shape as IMultiPatch;
			if (multiPatch == null)
			{
				return NoError;
			}

			if (_resolutionFactor > 1 && _minimumToleranceSpatialReference == null)
			{
				_minimumToleranceSpatialReference =
					SpatialReferenceUtils.CreateSpatialReferenceWithMinimumTolerance(
						_spatialReference, _resolutionFactor);
			}

			ICollection<SegmentProxy> verticalFaceSegments;
			IPolygon footprint = GetFootprint(feature, _minimumToleranceSpatialReference,
			                                  out verticalFaceSegments);

			int errorCount = 0;

			if (footprint != null &&
			    footprint.ExteriorRingCount !=
			    ((IGeometryCollection) footprint).GeometryCount)
			{
				errorCount += ReportError(footprint, multiPatch, row);
			}

			if (verticalFaceSegments.Count > 0)
			{
				errorCount +=
					ValidateVerticalFaces(feature, verticalFaceSegments, footprint);
			}

			return errorCount;
		}

		[CanBeNull]
		private IPolygon GetFootprint(
			[NotNull] IReadOnlyFeature feature,
			[CanBeNull] ISpatialReference alternateSpatialReference,
			[NotNull] out ICollection<SegmentProxy> verticalFaceSegments)
		{
			FootprintProvider provider = GetFootprintProvider(feature,
			                                                  alternateSpatialReference);
			return provider.GetFootprint(out verticalFaceSegments);
		}

		private int ValidateVerticalFaces(
			[NotNull] IReadOnlyFeature feature,
			[NotNull] IEnumerable<SegmentProxy> verticalFaceSegments,
			[CanBeNull] IPolygon footPrint)
		{
			int errorCount = 0;

			IGeometryCollection errorParts = null;
			foreach (SegmentProxy segment in verticalFaceSegments)
			{
				const bool forceCreation = true;
				IPolyline line = segment.GetPolyline(forceCreation);

				if (Math.Abs(line.Length) < double.Epsilon)
				{
					continue;
				}

				if (footPrint == null ||
				    ! ((IRelationalOperator2) footPrint).ContainsEx(
					    line, esriSpatialRelationExEnum.esriSpatialRelationExBoundary))
				{
					if (errorParts == null)
					{
						errorParts = new PolylineClass();
					}

					errorParts.AddGeometryCollection((IGeometryCollection) line);
				}
			}

			if (errorParts != null)
			{
				((ITopologicalOperator) errorParts).Simplify();
				if (footPrint == null)
				{
					if (GeometryUtils.IsSelfIntersecting((IGeometry) errorParts) ||
					    ((IPolyline) errorParts).IsClosed)
					{
						errorCount +=
							ReportError(
								"Multipatch has only vertical patches forming a ring",
								InvolvedRowUtils.GetInvolvedRows(feature),
								(IPolyline) errorParts,
								Codes[Code.MultiPatchHasOnlyVerticalPatchesFormingARing],
								_shapeFieldName);
					}
				}
				else
				{
					if (ReportVerticalPatchesNotCompletelyWithinFootprint)
					{
						errorCount +=
							ReportError(
								"Segments of vertical patch is not (completely) within footprint",
								InvolvedRowUtils.GetInvolvedRows(feature),
								(IPolyline) errorParts,
								Codes[Code.VerticalPatchNotCompletelyWithinFootprint],
								_shapeFieldName);
					}
				}
			}

			return errorCount;
		}

		private int ReportError([NotNull] IPolygon footPrint,
		                        [NotNull] IMultiPatch multiPatch,
		                        [NotNull] IReadOnlyRow row)
		{
			IGeometryCollection innerRings = new PolygonClass();

			var rings = (IGeometryCollection) footPrint;

			int ringCount = rings.GeometryCount;
			for (int index = 0; index < ringCount; index++)
			{
				var ring = (IRing) rings.Geometry[index];

				if (ring.IsExterior)
				{
					continue;
				}

				object missing = Type.Missing;
				innerRings.AddGeometry(GeometryFactory.Clone(ring), ref missing,
				                       ref missing);
			}

			((IZAware) innerRings).ZAware = true;
			((IZ) innerRings).SetConstantZ(multiPatch.Envelope.ZMin);

			int errorCount = 0;

			foreach (IRing ring in GeometryUtils.GetRings((IPolygon) innerRings))
			{
				double area = Math.Abs(((IArea) ring).Area);

				if (_minimumArea > 0 && area >= _minimumArea)
				{
					continue;
				}

				string description = string.Format("Footprint has inner ring (area: {0})",
				                                   FormatArea(area, _spatialReference));
				errorCount += ReportError(
					description, InvolvedRowUtils.GetInvolvedRows(row),
					GeometryFactory.CreatePolygon(ring),
					Codes[Code.FootprintHasInnerRing],
					_shapeFieldName, values: new object[] { area });
			}

			return errorCount;
		}

		[NotNull]
		private FootprintProvider GetFootprintProvider(
			[NotNull] IReadOnlyFeature multiPatchFeature,
			[CanBeNull] ISpatialReference alternateSpatialReference)
		{
			switch (_innerRingHandling)
			{
				case InnerRingHandling.None:
					return new SimpleFootprintProvider(multiPatchFeature,
					                                   alternateSpatialReference);

				case InnerRingHandling.IgnoreInnerRings:
					return new IgnoreInnerRingsFootprintProvider(multiPatchFeature,
					                                             alternateSpatialReference);

				case InnerRingHandling.IgnoreHorizontalInnerRings:
					return new IgnoreHorizontalInnerRingsFootprintProvider(
						multiPatchFeature,
						_horizontalZTolerance,
						_envelopeTemplate,
						alternateSpatialReference);
				default:
					throw new InvalidOperationException("Unhandled InnerRingHandling " +
					                                    _innerRingHandling);
			}
		}

		private abstract class FootprintProvider
		{
			[CanBeNull] private readonly ISpatialReference _alternateSpatialReference;
			private bool _initialized;
			private IIndexedMultiPatch _indexedMultipatch;

			private IMultiPatch _nonVerticalMultipatch;
			private List<SegmentProxy> _verticalFaceSegments;

			/// <summary>
			/// Initializes a new instance of the <see cref="FootprintProvider"/> class.
			/// </summary>
			/// <param name="alternateSpatialReference">
			/// An optional alternate spatial reference to get the multipatch in 
			/// (must have the same coordinate system; resolution/tolerance may be different).</param>
			protected FootprintProvider(
				[CanBeNull] ISpatialReference alternateSpatialReference)
			{
				_alternateSpatialReference = alternateSpatialReference;
			}

			[CanBeNull]
			public IPolygon GetFootprint(
				[NotNull] out ICollection<SegmentProxy> verticalFaceSegments)
			{
				if (! _initialized)
				{
					Initialize();
					_initialized = true;
				}

				verticalFaceSegments = _verticalFaceSegments;
				return GetFootprint(_nonVerticalMultipatch);
			}

			[NotNull]
			protected IMultiPatch GetMultiPatch([NotNull] IReadOnlyFeature multiPatchFeature)
			{
				if (_alternateSpatialReference != null)
				{
					var copy = (IMultiPatch) multiPatchFeature.ShapeCopy;
					copy.SpatialReference = _alternateSpatialReference;
					return copy;
				}

				return (IMultiPatch) multiPatchFeature.Shape;
			}

			[NotNull]
			protected abstract IIndexedMultiPatch GetAdaptedMultiPatch();

			[NotNull]
			protected IIndexedMultiPatch GetIndexedMultipatch(
				[NotNull] IReadOnlyFeature multiPatchFeature)
			{
				Assert.ArgumentNotNull(multiPatchFeature, nameof(multiPatchFeature));

				var indexedMultiPatchFeature =
					multiPatchFeature as IIndexedMultiPatchFeature;

				IIndexedMultiPatch result =
					indexedMultiPatchFeature != null && _alternateSpatialReference == null
						? indexedMultiPatchFeature.IndexedMultiPatch
						: ProxyUtils.CreateIndexedMultiPatch(
							GetMultiPatch(multiPatchFeature));

				return result;
			}

			private void Initialize()
			{
				_indexedMultipatch = GetAdaptedMultiPatch();
				IMultiPatch multiPatch = _indexedMultipatch.BaseGeometry;

				_verticalFaceSegments = new List<SegmentProxy>();

				var patches = (IGeometryCollection) multiPatch;
				var verticalPatchParts = new Dictionary<int, List<int>>();

				int patchCount = patches.GeometryCount;

				for (int patchIndex = 0; patchIndex < patchCount; patchIndex++)
				{
					List<int> partIndexes = _indexedMultipatch.GetPartIndexes(patchIndex);

					foreach (int partIndex in partIndexes)
					{
						int partSegmentCount =
							_indexedMultipatch.GetPartSegmentCount(partIndex);

						var segments = new List<SegmentProxy>(partSegmentCount);
						for (int segmentIndex = 0;
						     segmentIndex < partSegmentCount;
						     segmentIndex++)
						{
							segments.Add(
								_indexedMultipatch.GetSegment(partIndex, segmentIndex));
						}

						Plane plane = ProxyUtils.CreatePlane(segments);

						if (Math.Abs(plane.GetNormalVector().Z) < double.Epsilon)
						{
							List<int> verticalParts;
							if (! verticalPatchParts.TryGetValue(
								    patchIndex, out verticalParts))
							{
								verticalParts = new List<int>();
								verticalPatchParts.Add(patchIndex, verticalParts);
							}

							verticalParts.Add(partIndex);
							_verticalFaceSegments.AddRange(segments);
						}
					}
				}

				if (verticalPatchParts.Count > 0)
				{
					object missing = Type.Missing;
					IMultiPatch nonVerticalMultiPatch =
						new MultiPatchClass
						{
							SpatialReference = multiPatch.SpatialReference
						};

					for (int patchIndex = 0; patchIndex < patchCount; patchIndex++)
					{
						List<int> verticalParts;
						IGeometry patch =
							((IGeometryCollection) multiPatch).Geometry[patchIndex];

						if (! verticalPatchParts.TryGetValue(
							    patchIndex, out verticalParts))
						{
							IGeometry clone = GeometryFactory.Clone(patch);
							((IGeometryCollection) nonVerticalMultiPatch).AddGeometry(
								clone,
								ref missing,
								ref missing);
							var ring = patch as IRing;
							if (ring != null)
							{
								bool isBeginning = false;
								esriMultiPatchRingType ringType =
									multiPatch.GetRingType(ring, ref isBeginning);
								nonVerticalMultiPatch.PutRingType(
									(IRing) clone, ringType);
							}
						}
						else
						{
							if (patch is IRing)
							{
								continue;
							}

							List<int> partIndexes =
								_indexedMultipatch.GetPartIndexes(patchIndex);

							foreach (int partIndex in partIndexes)
							{
								if (verticalParts.Contains(partIndex))
								{
									continue;
								}

								int partSegmentCount =
									_indexedMultipatch.GetPartSegmentCount(partIndex);

								var points = new List<WKSPointZ>(3);

								for (int segmentIndex = 0;
								     segmentIndex < partSegmentCount;
								     segmentIndex++)
								{
									SegmentProxy segment =
										_indexedMultipatch.GetSegment(
											partIndex, segmentIndex);

									const bool as3D = true;
									Pnt p = segment.GetStart(as3D);

									points.Add(
										WKSPointZUtils.CreatePoint(p.X, p.Y, p[2]));
								}

								IRing ring = CreateRing(points);

								((IGeometryCollection) nonVerticalMultiPatch).AddGeometry(
									ring,
									ref missing,
									ref missing);
							}
						}
					}

					_nonVerticalMultipatch = nonVerticalMultiPatch;
				}
				else
				{
					_nonVerticalMultipatch = multiPatch;
				}
			}

			[NotNull]
			private static IRing CreateRing([NotNull] List<WKSPointZ> points)
			{
				WKSPointZ[] pointArray = points.ToArray();

				IPointCollection4 ring = new RingClass();

				GeometryUtils.SetWKSPointZs(ring, pointArray);

				return (IRing) ring;
			}

			[CanBeNull]
			private static IPolygon GetFootprint([NotNull] IMultiPatch multiPatch)
			{
				return multiPatch.IsEmpty
					       ? null
					       : GeometryFactory.CreatePolygon(multiPatch);
			}
		}

		private class SimpleFootprintProvider : FootprintProvider
		{
			private readonly IIndexedMultiPatch _multiPatch;

			/// <summary>
			/// Initializes a new instance of the <see cref="SimpleFootprintProvider"/> class.
			/// </summary>
			/// <param name="multiPatchFeature">The multipatch feature.</param>
			/// <param name="alternateSpatialReference">
			/// An optional alternate spatial reference to get the multipatch in 
			/// (must have the same coordinate system; resolution/tolerance may be different).</param>
			public SimpleFootprintProvider([NotNull] IReadOnlyFeature multiPatchFeature,
			                               [CanBeNull] ISpatialReference
				                               alternateSpatialReference)
				: base(alternateSpatialReference)
			{
				_multiPatch = GetIndexedMultipatch(multiPatchFeature);
			}

			protected override IIndexedMultiPatch GetAdaptedMultiPatch()
			{
				return _multiPatch;
			}
		}

		private abstract class InnerRingsFootprintProviderBase : FootprintProvider
		{
			protected InnerRingsFootprintProviderBase(
				[CanBeNull] ISpatialReference alternateSpatialReference)
				: base(alternateSpatialReference) { }

			[NotNull]
			protected static IMultiPatch CopyWithConvertedInnerRings(
				[NotNull] IMultiPatch multiPatch,
				[NotNull] IEnumerable<int> outerRingIndexes)
			{
				IMultiPatch result = GeometryFactory.Clone(multiPatch);
				var allFollowingRings = new List<IRing>();

				foreach (int outerRingIndex in outerRingIndexes)
				{
					var ring =
						(IRing) ((IGeometryCollection) result).Geometry[outerRingIndex];

					int followingRingCount = result.FollowingRingCount[ring];
					var followingRings = new IRing[followingRingCount];
					GeometryUtils.GeometryBridge.QueryFollowingRings(result, ring,
						ref followingRings);

					allFollowingRings.AddRange(followingRings);
				}

				foreach (IRing followingRing in allFollowingRings)
				{
					result.PutRingType(followingRing,
					                   esriMultiPatchRingType.esriMultiPatchOuterRing);
				}

				return result;
			}
		}

		private class IgnoreInnerRingsFootprintProvider : InnerRingsFootprintProviderBase
		{
			private readonly IReadOnlyFeature _multiPatchFeature;

			public IgnoreInnerRingsFootprintProvider(
				[NotNull] IReadOnlyFeature multiPatchFeature,
				[CanBeNull] ISpatialReference alternateSpatialReference)
				: base(alternateSpatialReference)
			{
				_multiPatchFeature = multiPatchFeature;
			}

			protected override IIndexedMultiPatch GetAdaptedMultiPatch()
			{
				IMultiPatch multiPatch = GetMultiPatch(_multiPatchFeature);
				var parts = (IGeometryCollection) multiPatch;
				var outerRingIndexes = new List<int>();

				int partCount = parts.GeometryCount;

				for (int partIndex = 0; partIndex < partCount; partIndex++)
				{
					var ring = parts.Geometry[partIndex] as IRing;
					if (ring == null)
					{
						continue;
					}

					bool isBeginning = false;
					multiPatch.GetRingType(ring, ref isBeginning);

					if (! isBeginning)
					{
						continue;
					}

					int followingRings = multiPatch.FollowingRingCount[ring];
					if (followingRings <= 0)
					{
						continue;
					}

					outerRingIndexes.Add(partIndex);
				}

				if (outerRingIndexes.Count > 0)
				{
					IMultiPatch adapted =
						CopyWithConvertedInnerRings(multiPatch, outerRingIndexes);

					return ProxyUtils.CreateIndexedMultiPatch(adapted);
				}

				return GetIndexedMultipatch(_multiPatchFeature);
			}
		}

		private class IgnoreHorizontalInnerRingsFootprintProvider :
			InnerRingsFootprintProviderBase
		{
			private readonly IReadOnlyFeature _multiPatchFeature;
			private readonly double _horizontalZTolerance;
			private readonly IEnvelope _envelopeTemplate;

			public IgnoreHorizontalInnerRingsFootprintProvider(
				[NotNull] IReadOnlyFeature multiPatchFeature,
				double horizontalZTolerance,
				[NotNull] IEnvelope envelopeTemplate,
				[CanBeNull] ISpatialReference alternateSpatialReference)
				: base(alternateSpatialReference)
			{
				_multiPatchFeature = multiPatchFeature;
				_horizontalZTolerance = horizontalZTolerance;
				_envelopeTemplate = envelopeTemplate;
			}

			protected override IIndexedMultiPatch GetAdaptedMultiPatch()
			{
				IMultiPatch multiPatch = GetMultiPatch(_multiPatchFeature);
				var parts = (IGeometryCollection) multiPatch;
				var outerRingIndexes = new List<int>();

				int partCount = parts.GeometryCount;
				for (int partIndex = 0; partIndex < partCount; partIndex++)
				{
					var ring = parts.Geometry[partIndex] as IRing;
					if (ring == null)
					{
						continue;
					}

					bool isBeginning = false;
					multiPatch.GetRingType(ring, ref isBeginning);

					if (! isBeginning)
					{
						continue;
					}

					int followingRings = multiPatch.FollowingRingCount[ring];
					if (followingRings <= 0)
					{
						continue;
					}

					if (! IsHorizontal(ring))
					{
						continue;
					}

					outerRingIndexes.Add(partIndex);
				}

				if (outerRingIndexes.Count > 0)
				{
					IMultiPatch adapted =
						CopyWithConvertedInnerRings(multiPatch, outerRingIndexes);
					return ProxyUtils.CreateIndexedMultiPatch(adapted);
				}

				return GetIndexedMultipatch(_multiPatchFeature);
			}

			private bool IsHorizontal([NotNull] IRing ring)
			{
				ring.QueryEnvelope(_envelopeTemplate);

				return _envelopeTemplate.Depth <= _horizontalZTolerance;
			}
		}
	}
}
