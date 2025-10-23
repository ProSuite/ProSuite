using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Properties;
using Pnt = ProSuite.Commons.Geom.Pnt;
using SegmentUtils_ = ProSuite.QA.Container.Geometry.SegmentUtils_;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check that polygons are not too thin and long
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaSliverPolygon : ContainerTest
	{
		private readonly double _limit;
		private readonly double _maxArea;
		private readonly int _areaFieldIndex;
		private readonly int _lengthFieldIndex;
		private readonly bool _useFields;
		private readonly string _shapeFieldName;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string AreaTooSmall = "AreaTooSmall";
			public const string SliverRatioTooLarge = "SliverRatioTooLarge";

			public const string AreaTooSmallAndSliverRatioTooLarge =
				"AreaTooSmallAndSliverRatioTooLarge";

			public Code() : base("SliverPolygons") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaSliverPolygon_0))]
		public QaSliverPolygon(
				[Doc(nameof(DocStrings.QaSliverPolygon_polygonClass))]
				IReadOnlyFeatureClass polygonClass,
				[Doc(nameof(DocStrings.QaSliverPolygon_limit))]
				double limit)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polygonClass, limit, -1) { }

		[Doc(nameof(DocStrings.QaSliverPolygon_0))]
		public QaSliverPolygon(
			[Doc(nameof(DocStrings.QaSliverPolygon_polygonClass))]
			IReadOnlyFeatureClass polygonClass,
			[Doc(nameof(DocStrings.QaSliverPolygon_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaSliverPolygon_maxArea))]
			double maxArea)
			: base(polygonClass)
		{
			Assert.ArgumentNotNull(polygonClass, nameof(polygonClass));
			Assert.ArgumentCondition(
				polygonClass.ShapeType == esriGeometryType.esriGeometryPolygon ||
				polygonClass.ShapeType == esriGeometryType.esriGeometryMultiPatch,
				"Not a polygon or Multipatch feature class");

			_limit = limit;
			_maxArea = maxArea;
			_shapeFieldName = polygonClass.ShapeFieldName;

			IField areaField = polygonClass.AreaField;
			IField lengthField = polygonClass.LengthField;

			_areaFieldIndex = areaField == null
				                  ? -1
				                  : polygonClass.FindField(areaField.Name);
			_lengthFieldIndex = lengthField == null
				                    ? -1
				                    : polygonClass.FindField(lengthField.Name);
			_useFields = _areaFieldIndex >= 0 && _lengthFieldIndex >= 0;
		}

		[InternallyUsedTest]
		public QaSliverPolygon([NotNull] QaSliverPolygonDefinition definition)
			: this((IReadOnlyFeatureClass) definition.PolygonClass, definition.Limit,
			       definition.MaxArea) { }

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
			if (_maxArea <= 0 && _limit <= 0)
			{
				// neither max area nor limit is specified to an active value
				return NoError;
			}

			var feature = row as IReadOnlyFeature;
			if (feature == null)
			{
				return NoError;
			}

			using (SliverAreaProvider provider = GetSliverAreaProvider(feature))
			{
				int errorCount = 0;

				SliverArea sliverArea;
				while ((sliverArea = provider.ReadSliverArea()) != null)
				{
					double absoluteArea = sliverArea.AbsoluteArea;
					if (_maxArea > 0 && absoluteArea > _maxArea)
					{
						// area is big enough
						continue;
					}

					// small area, or no area limit --> check sliver ratio
					double perimeter = sliverArea.Perimeter;
					double ratio = perimeter * perimeter / absoluteArea;

					if (ratio <= _limit)
					{
						// ratio is less than limit --> allow
						continue;
					}

					// ratio is larger than limit (but limit may be inactive)

					IGeometry geometry = provider.GetErrorGeometry();

					string description =
						GetErrorDescription(absoluteArea, perimeter, ratio, geometry);

					errorCount += ReportError(
						description, InvolvedRowUtils.GetInvolvedRows(row), geometry,
						GetIssueCode(), _shapeFieldName);
				}

				return errorCount;
			}
		}

		[CanBeNull]
		private IssueCode GetIssueCode()
		{
			if (_maxArea <= 0)
			{
				// only limit is active
				return Codes[Code.SliverRatioTooLarge];
			}

			if (_limit <= 0)
			{
				// only maxArea is active
				return Codes[Code.AreaTooSmall];
			}

			// both are active
			// - an error is reported when BOTH the area is too small and the 
			//   sliver ratio is too large
			return Codes[Code.AreaTooSmallAndSliverRatioTooLarge];
		}

		[NotNull]
		private string GetErrorDescription(double absoluteArea,
		                                   double perimeter,
		                                   double ratio,
		                                   [NotNull] IGeometry geometry)
		{
			ISpatialReference spatialReference = geometry.SpatialReference;

			return string.Format(
				LocalizableStrings.QaSliverPolygon_Message,
				IsExteriorRing(geometry)
					? "Exterior"
					: "Interior",
				_maxArea > 0
					? FormatAreaComparison(absoluteArea, "<=", _maxArea, spatialReference)
					: FormatArea(absoluteArea, spatialReference),
				double.IsInfinity(ratio) || double.IsNaN(ratio)
					? "not defined"
					: _limit > 0
						? FormatAreaComparison(ratio, ">", _limit, spatialReference)
						: FormatArea(ratio, spatialReference),
				FormatLength(perimeter, spatialReference));
		}

		[NotNull]
		private SliverAreaProvider GetSliverAreaProvider([NotNull] IReadOnlyFeature feature)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			if (_useFields)
			{
				return new FromFieldsSliverAreaProvider(feature, _areaFieldIndex,
				                                        _lengthFieldIndex);
			}

			if (feature.Shape is IPolygon polygon)
			{
				return new PolygonRingsSliverAreaProvider(polygon);
			}

			if (feature.Shape is IMultiPatch multiPatch)
			{
				var indexedMultiPatchFeature = feature as IIndexedMultiPatchFeature;

				IIndexedMultiPatch indexedMultiPatch =
					indexedMultiPatchFeature != null
						? indexedMultiPatchFeature.IndexedMultiPatch
						: ProxyUtils.CreateIndexedMultiPatch(multiPatch);

				return new MultiPatchPartsSliverAreaProvider(indexedMultiPatch);
			}

			throw new InvalidOperationException("feature is unhandled");
		}

		private class FromFieldsSliverAreaProvider : SliverAreaProvider
		{
			private readonly IReadOnlyFeature _feature;
			private readonly int _areaFieldIndex;
			private readonly int _lengthFieldIndex;

			private bool _completed;

			public override void Dispose() { }

			public FromFieldsSliverAreaProvider([NotNull] IReadOnlyFeature feature,
			                                    int areaFieldIndex,
			                                    int lengthFieldIndex)
			{
				_feature = feature;
				_areaFieldIndex = areaFieldIndex;
				_lengthFieldIndex = lengthFieldIndex;

				_completed = false;
			}

			protected override bool GetNextSliverArea(out double area, out double perimeter)
			{
				if (_completed)
				{
					area = double.NaN;
					perimeter = double.NaN;
					return false;
				}

				double? areaValue = GdbObjectUtils.ReadRowValue<double>(_feature, _areaFieldIndex);
				double? perimeterValue = GdbObjectUtils.ReadRowValue<double>(_feature,
					_lengthFieldIndex);

				if (areaValue == null || perimeterValue == null)
				{
					area = double.NaN;
					perimeter = double.NaN;
					return false;
				}

				area = areaValue.Value;
				perimeter = perimeterValue.Value;
				_completed = true;
				return true;
			}

			public override IGeometry GetErrorGeometry()
			{
				return _feature.ShapeCopy;
			}
		}

		private class PolygonRingsSliverAreaProvider : SliverAreaProvider
		{
			[NotNull] private readonly IEnumerator<IRing> _ringEnum;

			private IRing _latestRing;

			public PolygonRingsSliverAreaProvider([NotNull] IPolygon polygon)
			{
				_ringEnum = GeometryUtils.GetRings(polygon).GetEnumerator();
			}

			public override void Dispose()
			{
				if (_latestRing != null)
				{
					Marshal.ReleaseComObject(_latestRing);
				}

				_latestRing = null;
			}

			protected override bool GetNextSliverArea(out double area, out double perimeter)
			{
				if (_latestRing != null)
				{
					Marshal.ReleaseComObject(_latestRing);
					_latestRing = null;
				}

				if (! _ringEnum.MoveNext())
				{
					area = double.NaN;
					perimeter = double.NaN;
					return false;
				}

				_latestRing = Assert.NotNull(_ringEnum.Current);

				area = ((IArea) _latestRing).Area;
				perimeter = _latestRing.Length;

				return true;
			}

			public override IGeometry GetErrorGeometry()
			{
				bool isExterior = _latestRing.IsExterior;

				IPolygon result = GeometryFactory.CreatePolygon(_latestRing);

				if (! isExterior)
				{
					result.ReverseOrientation();
				}

				return result;
			}
		}

		private class MultiPatchPartsSliverAreaProvider : SliverAreaProvider
		{
			private readonly IIndexedMultiPatch _indexedMultiPatch;
			private readonly IEnumerator<SegmentProxy> _segmentsEnum;

			private List<SegmentProxy> _latestPartSegments;
			private bool _enumValid;

			public MultiPatchPartsSliverAreaProvider(
				[NotNull] IIndexedMultiPatch indexedMultiPatch)
			{
				_indexedMultiPatch = indexedMultiPatch;
				_segmentsEnum = _indexedMultiPatch.GetSegments().GetEnumerator();
				_enumValid = _segmentsEnum.MoveNext();
			}

			public override void Dispose() { }

			protected override bool GetNextSliverArea(out double area, out double perimeter)
			{
				_latestPartSegments = null;
				if (! _enumValid)
				{
					area = double.NaN;
					perimeter = double.NaN;
					return false;
				}

				SegmentProxy segment = Assert.NotNull(_segmentsEnum.Current);

				int currentPart = segment.PartIndex;
				int segmentCount = _indexedMultiPatch.GetPartSegmentCount(currentPart);
				var partSegments = new List<SegmentProxy>(segmentCount) {segment};

				while ((_enumValid = _segmentsEnum.MoveNext()) &&
				       Assert.NotNull(_segmentsEnum.Current).PartIndex == currentPart)
				{
					partSegments.Add(_segmentsEnum.Current);
				}

				List<Pnt> planePoints = ProxyUtils.GetPoints(partSegments);
				Plane plane = ProxyUtils.CreatePlane(partSegments);

				ProxyUtils.CalculateProjectedArea(plane, planePoints, out area, out perimeter);

				_latestPartSegments = partSegments;

				return true;
			}

			public override IGeometry GetErrorGeometry()
			{
				return SegmentUtils_.CreateMultiPatch(Assert.NotNull(_latestPartSegments));
			}
		}

		private static bool IsExteriorRing([NotNull] IGeometry geometry)
		{
			var ring = geometry as IRing;
			return ring == null || ring.IsExterior;
		}

		private class SliverArea
		{
			public readonly double AbsoluteArea;
			public readonly double Perimeter;

			public SliverArea(double area, double perimeter)
			{
				AbsoluteArea = Math.Abs(area);
				Perimeter = perimeter;
			}
		}

		private abstract class SliverAreaProvider : IDisposable
		{
			[CanBeNull]
			public SliverArea ReadSliverArea()
			{
				double area;
				double perimeter;

				bool hasNext = GetNextSliverArea(out area, out perimeter);

				return ! hasNext
					       ? null
					       : new SliverArea(area, perimeter);
			}

			public abstract void Dispose();

			protected abstract bool GetNextSliverArea(out double area, out double perimeter);

			[NotNull]
			public abstract IGeometry GetErrorGeometry();
		}
	}
}
