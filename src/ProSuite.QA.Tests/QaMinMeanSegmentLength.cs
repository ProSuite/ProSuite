using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using SegmentUtils_ = ProSuite.QA.Container.Geometry.SegmentUtils_;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	public class QaMinMeanSegmentLength : ContainerTest
	{
		private readonly double _limit;
		private readonly bool _perPart;
		private readonly bool _is3D;
		private readonly ISpatialReference _spatialReference;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string InvalidSegmentCount = "InvalidSegmentCount";

			public const string AverageSegmentLengthBelowLimit =
				"AverageSegmentLengthBelowLimit";

			public Code() : base("MinMeanSegmentLength") { }
		}

		#endregion

		#region constructors

		[Doc(nameof(DocStrings.QaMinMeanSegmentLength_0))]
		public QaMinMeanSegmentLength(
			[Doc(nameof(DocStrings.QaMinMeanSegmentLength_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMinMeanSegmentLength_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaMinMeanSegmentLength_perPart))]
			bool perPart)
			: this(
				featureClass, limit, perPart,
				featureClass.ShapeType == esriGeometryType.esriGeometryMultiPatch) { }

		[Doc(nameof(DocStrings.QaMinMeanSegmentLength_0))]
		public QaMinMeanSegmentLength(
			[Doc(nameof(DocStrings.QaMinMeanSegmentLength_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMinMeanSegmentLength_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaMinMeanSegmentLength_perPart))]
			bool perPart,
			[Doc(nameof(DocStrings.QaMinMeanSegmentLength_is3D))]
			bool is3D)
			: base(featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			_limit = limit;
			_perPart = perPart;
			_is3D = is3D;

			_spatialReference = featureClass.SpatialReference;
		}

		#endregion

		[InternallyUsedTest]
		public QaMinMeanSegmentLength(
			[NotNull] QaMinMeanSegmentLengthDefinition definition)
			: this((IReadOnlyFeatureClass) definition.FeatureClass,
			       definition.Limit, definition.PerPart, definition.Is3D) { }

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
			using (
				MeanSegmentLengthProvider provider =
				GetMeanSegmentLengthProvider((IReadOnlyFeature) row))
			{
				int errorCount = 0;
				foreach (MeanSegmentLength meanSegmentLength in provider.ReadSegmentLength())
				{
					errorCount += CheckLength(meanSegmentLength, row);
				}

				return errorCount;
			}
		}

		[NotNull]
		private MeanSegmentLengthProvider GetMeanSegmentLengthProvider(IReadOnlyFeature row)
		{
			MeanSegmentLengthProvider provider;

			if (! _is3D && row.Shape is ICurve)
			{
				provider = new CurveMeanSegmentsLengthProvider((ICurve) row.Shape, _perPart);
			}
			else
			{
				var segmentsFeature = row as IIndexedSegmentsFeature;
				if (segmentsFeature != null && segmentsFeature.AreIndexedSegmentsLoaded)
				{
					provider = new IndexedMeanLengthProvider(row.Shape,
					                                         segmentsFeature.IndexedSegments,
					                                         _perPart, _is3D);
				}
				else if (row.Shape is IMultiPatch)
				{
					IIndexedSegments indexedSegments =
						ProxyUtils.CreateIndexedMultiPatch((IMultiPatch) row.Shape);
					provider = new IndexedMeanLengthProvider(row.Shape, indexedSegments, _perPart,
					                                         _is3D);
				}
				else
				{
					provider = new SegmentCollectionMeanLengthProvider(
						(ISegmentCollection) row.Shape,
						_perPart, _is3D);
				}
			}

			return provider;
		}

		private int CheckLength([NotNull] MeanSegmentLength meanSegmentLength,
		                        [NotNull] IReadOnlyRow row)
		{
			int segmentCount = meanSegmentLength.SegmentCount;
			double length = meanSegmentLength.FullLength;
			if (segmentCount <= 0)
			{
				return ReportError(
					"Invalid segment count",
					InvolvedRowUtils.GetInvolvedRows(row),
					meanSegmentLength.GetErrorGeometry(),
					Codes[Code.InvalidSegmentCount], TestUtils.GetShapeFieldName(row));
			}

			double averageSegmentLength = length / segmentCount;

			if (averageSegmentLength >= _limit)
			{
				return NoError;
			}

			string description =
				string.Format(
					"Average segment length {0} (segment count: {1:N0}, total length: {2})",
					FormatLengthComparison(averageSegmentLength, "<", _limit, _spatialReference),
					segmentCount,
					FormatLength(length, _spatialReference));

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row),
				meanSegmentLength.GetErrorGeometry(),
				Codes[Code.AverageSegmentLengthBelowLimit],
				TestUtils.GetShapeFieldName(row));
		}

		[NotNull]
		private static IGeometry GetErrorGeometry([NotNull] ICurve shape)
		{
			// TODO use envelope if vertex count is too big?
			// return shape.Envelope;

			IPolycurve polycurve = CreateHighLevelCopy(shape);

			const int maxAllowableOffsetFactor = 10;
			polycurve.Weed(maxAllowableOffsetFactor);

			return polycurve;
		}

		[NotNull]
		private static IPolycurve CreateHighLevelCopy(ICurve shape)
		{
			if (shape is IPolyline || shape is IPolygon)
			{
				return (IPolycurve) GeometryFactory.Clone(shape);
			}

			if (shape is IRing)
			{
				return GeometryFactory.CreatePolygon(shape);
			}

			if (shape is IPath)
			{
				return GeometryFactory.CreatePolyline(shape);
			}

			throw new ArgumentException(
				string.Format("Unsupported shape type: {0}", shape.GeometryType), nameof(shape));
		}

		private class MeanSegmentLength
		{
			private readonly double _fullLength;
			private readonly int _segmentCount;
			private readonly Func<IGeometry> _getErrorGeometry;

			public MeanSegmentLength(double fullLength, int segmentCount,
			                         Func<IGeometry> getErrorGeometry)
			{
				_fullLength = fullLength;
				_segmentCount = segmentCount;
				_getErrorGeometry = getErrorGeometry;
			}

			public double FullLength => _fullLength;

			public int SegmentCount => _segmentCount;

			public IGeometry GetErrorGeometry()
			{
				return _getErrorGeometry();
			}
		}

		private abstract class MeanSegmentLengthProvider : IDisposable
		{
			public IEnumerable<MeanSegmentLength> ReadSegmentLength()
			{
				return ReadSegmentLengthCore();
			}

			protected abstract IEnumerable<MeanSegmentLength> ReadSegmentLengthCore();

			public void Dispose() { }
		}

		private class CurveMeanSegmentsLengthProvider : MeanSegmentLengthProvider
		{
			[NotNull] private readonly ICurve _curve;
			private readonly bool _perPart;

			public CurveMeanSegmentsLengthProvider([NotNull] ICurve curve, bool perPart)
			{
				_curve = curve;
				_perPart = perPart;
			}

			protected override IEnumerable<MeanSegmentLength> ReadSegmentLengthCore()
			{
				if (_curve.IsEmpty)
				{
					yield break;
				}

				var geometryCollection = _curve as IGeometryCollection;

				if (! _perPart || geometryCollection == null ||
				    geometryCollection.GeometryCount == 1)
				{
					yield return GetMeanSegmentLength(_curve);
				}
				else
				{
					foreach (IGeometry part in GeometryUtils.GetParts(geometryCollection))
					{
						yield return GetMeanSegmentLength((ICurve) part);
					}
				}
			}

			private static MeanSegmentLength GetMeanSegmentLength(ICurve curve)
			{
				var segments = curve as ISegmentCollection;
				var meanSegmentLentgh = new MeanSegmentLength(curve.Length,
				                                              segments?.SegmentCount ?? 1,
				                                              () => GetErrorGeometry(curve));
				return meanSegmentLentgh;
			}
		}

		private class IndexedMeanLengthProvider : MeanSegmentLengthProvider
		{
			private readonly IGeometry _baseGeometry;
			private readonly IIndexedSegments _indexedSegments;
			private readonly bool _is3D;
			private readonly bool _perPart;

			public IndexedMeanLengthProvider([NotNull] IGeometry baseGeometry,
			                                 [NotNull] IIndexedSegments indexedSegments,
			                                 bool perPart, bool is3D)
			{
				_baseGeometry = baseGeometry;
				_indexedSegments = indexedSegments;
				_perPart = perPart;
				_is3D = is3D;
			}

			protected override IEnumerable<MeanSegmentLength> ReadSegmentLengthCore()
			{
				int partIndex = -1;
				int segmentCount = 0;
				double fullLength = 0;
				foreach (SegmentProxy segment in _indexedSegments.GetSegments())
				{
					if (_perPart && segment.PartIndex != partIndex)
					{
						if (segmentCount > 0)
						{
							int index = partIndex;
							yield return
								new MeanSegmentLength(fullLength, segmentCount,
								                      () => GetGeometry(index));
						}

						segmentCount = 0;
						fullLength = 0;
						partIndex = segment.PartIndex;
					}

					double length = segment.Length;
					if (_is3D)
					{
						double z0 = segment.GetStart(true)[2];
						double z1 = segment.GetEnd(true)[2];
						double dz = z1 - z0;
						length = Math.Sqrt(length * length + dz * dz);
					}

					fullLength += length;
					segmentCount++;
				}

				if (segmentCount > 0)
				{
					yield return
						new MeanSegmentLength(fullLength, segmentCount,
						                      () => GetGeometry(partIndex));
				}
			}

			private IGeometry GetGeometry(int partIndex)
			{
				IGeometry geometry;
				if (! _perPart)
				{
					geometry = GeometryFactory.Clone(_baseGeometry);
				}
				else if (_baseGeometry is ICurve)
				{
					var geometryCollection = _baseGeometry as IGeometryCollection;
					if (geometryCollection == null || geometryCollection.GeometryCount == 1)
					{
						geometry = GetErrorGeometry((ICurve) _baseGeometry);
					}
					else
					{
						var curve = (ICurve) geometryCollection.get_Geometry(partIndex);
						geometry = GetErrorGeometry(curve);
					}

					return geometry;
				}
				else
				{
					IGeometryCollection geometryCollection;
					if (_baseGeometry.GeometryType == esriGeometryType.esriGeometryPolygon)
					{
						geometryCollection = ProxyUtils.CreatePolygon(_baseGeometry);
					}
					else if (_baseGeometry.GeometryType == esriGeometryType.esriGeometryPolyline)
					{
						geometryCollection = ProxyUtils.CreatePolyline(_baseGeometry);
					}
					else if (_baseGeometry.GeometryType == esriGeometryType.esriGeometryMultiPatch)
					{
						geometryCollection = new MultiPatchClass();
					}
					else
					{
						throw new InvalidOperationException("unhandled geometry type " +
						                                    _baseGeometry.GeometryType);
					}

					int segmentCount = _indexedSegments.GetPartSegmentCount(partIndex);
					var partSegments = new List<SegmentProxy>(segmentCount);
					for (int iSegment = 0; iSegment < segmentCount; iSegment++)
					{
						partSegments.Add(_indexedSegments.GetSegment(partIndex, iSegment));
					}

					SegmentUtils_.CreateGeometry(geometryCollection, partSegments);
					geometry = (IGeometry) geometryCollection;
				}

				return geometry;
			}
		}

		private class SegmentCollectionMeanLengthProvider : MeanSegmentLengthProvider
		{
			private readonly ISegmentCollection _segments;
			private readonly bool _is3D;
			private readonly bool _perPart;

			public SegmentCollectionMeanLengthProvider(ISegmentCollection segments,
			                                           bool perPart, bool is3D)
			{
				_segments = segments;
				_perPart = perPart;
				_is3D = is3D;
			}

			protected override IEnumerable<MeanSegmentLength> ReadSegmentLengthCore()
			{
				IEnumSegment enumSegments = _segments.EnumSegments;
				bool isRecycling = enumSegments.IsRecycling;
				enumSegments.Reset();
				int lastPartIndex = -1;
				int segmentCount = 0;
				double fullLength = 0;
				ISegment segment = null;
				int partIndex = 0;
				int segmentIndex = 0;
				do
				{
					if (isRecycling && segment != null)
					{
						Marshal.ReleaseComObject(segment);
					}

					enumSegments.Next(out segment, ref partIndex, ref segmentIndex);

					if (segment != null)
					{
						if (_perPart && partIndex != lastPartIndex)
						{
							if (segmentCount > 0)
							{
								int index = lastPartIndex;
								yield return
									new MeanSegmentLength(fullLength, segmentCount,
									                      () => GetGeometry(index));
							}

							segmentCount = 0;
							fullLength = 0;
							lastPartIndex = partIndex;
						}

						double length = segment.Length;
						if (_is3D)
						{
							double z0 = segment.FromPoint.Z;
							double z1 = segment.ToPoint.Z;
							double dz = z1 - z0;
							length = Math.Sqrt(length * length + dz * dz);
						}

						fullLength += length;
						segmentCount++;
					}
				} while (segment != null);

				if (segmentCount > 0)
				{
					yield return new MeanSegmentLength(fullLength,
					                                   segmentCount,
					                                   () => GetGeometry(lastPartIndex));
				}

				Marshal.ReleaseComObject(enumSegments);
			}

			private IGeometry GetGeometry(int partIndex)
			{
				IGeometry geometry;
				var geometryCollection = _segments as IGeometryCollection;
				if (! _perPart || geometryCollection == null ||
				    geometryCollection.GeometryCount == 1)
				{
					geometry = GetErrorGeometry((ICurve) _segments);
				}
				else
				{
					var curve = (ICurve) geometryCollection.get_Geometry(partIndex);
					geometry = GetErrorGeometry(curve);
				}

				return geometry;
			}
		}
	}
}
