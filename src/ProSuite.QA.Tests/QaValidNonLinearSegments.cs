using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
	[GeometryTest]
	[UsedImplicitly]
	public class QaValidNonLinearSegments : NonContainerTest
	{
		private readonly IFeatureClass _featureClass;
		private readonly double _minimumChordHeight;
		private readonly ISpatialReference _spatialReference;
		private readonly bool _canHaveNonLinearSegments;
		private readonly double _xyTolerance;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string CicularArc_NoCenterPoint = "CicularArc.NoCenterPoint";

			public const string CircularArc_ChordHeightTooSmall =
				"CircularArc.ChordHeightTooSmall";

			public Code() : base("ValidNonLinearSegments") { }
		}

		#endregion

		[Doc("QaValidNonLinearSegments_0")]
		public QaValidNonLinearSegments(
				[Doc("QaValidNonLinearSegments_featureClass")] [NotNull]
				IFeatureClass featureClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, 0d) { }

		[Doc("QaValidNonLinearSegments_1")]
		public QaValidNonLinearSegments(
			[Doc("QaValidNonLinearSegments_featureClass")] [NotNull]
			IFeatureClass featureClass,
			[Doc("QaValidNonLinearSegments_minimumChordHeight")]
			double minimumChordHeight)
			: base(new[] {(ITable) featureClass})
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			_featureClass = featureClass;
			_minimumChordHeight = minimumChordHeight;

			esriGeometryType shapeType = featureClass.ShapeType;
			_canHaveNonLinearSegments =
				shapeType == esriGeometryType.esriGeometryPolyline ||
				shapeType == esriGeometryType.esriGeometryPolygon;

			_spatialReference = ((IGeoDataset) featureClass).SpatialReference;
			_xyTolerance = GeometryUtils.GetXyTolerance(featureClass);
		}

		#region Overrides of TestBase

		public override int Execute()
		{
			return ExecuteGeometry(null);
		}

		public override int Execute(IEnvelope boundingBox)
		{
			return ExecuteGeometry(boundingBox);
		}

		public override int Execute(IPolygon area)
		{
			return ExecuteGeometry(area);
		}

		public override int Execute(IEnumerable<IRow> selectedRows)
		{
			if (! _canHaveNonLinearSegments)
			{
				return NoError;
			}

			int errorCount = 0;

			foreach (IRow row in selectedRows)
			{
				if (row.Table != _featureClass)
				{
					continue;
				}

				errorCount += VerifyFeature((IFeature) row);
			}

			return errorCount;
		}

		public override int Execute(IRow row)
		{
			return _canHaveNonLinearSegments
				       ? VerifyFeature((IFeature) row)
				       : NoError;
		}

		protected override ISpatialReference GetSpatialReference()
		{
			return _spatialReference;
		}

		#endregion

		private int ExecuteGeometry([CanBeNull] IGeometry geometry)
		{
			if (! _canHaveNonLinearSegments)
			{
				return NoError;
			}

			IQueryFilter filter = TestUtils.CreateFilter(geometry, AreaOfInterest,
			                                             GetConstraint(0),
			                                             (ITable) _featureClass,
			                                             null);

			GdbQueryUtils.SetSubFields(filter,
			                           _featureClass.OIDFieldName,
			                           _featureClass.ShapeFieldName);

			int errorCount = 0;

			const bool recycle = true;
			foreach (IFeature feature in
				GdbQueryUtils.GetFeatures(_featureClass, filter, recycle))
			{
				errorCount += VerifyFeature(feature);
			}

			return errorCount;
		}

		private int VerifyFeature([NotNull] IFeature feature)
		{
			// don't cancel based on stop conditions, since this test will usually 
			// be a stop condition itself - would only find the first segment per feature

			IGeometry shape = feature.Shape;
			var segments = shape as ISegmentCollection;

			return segments == null
				       ? NoError
				       : ReportCorruptNonLinearSegments(segments, feature);
		}

		private static bool HasNonLinearSegments([NotNull] ISegmentCollection segments)
		{
			bool result = false;
			segments.HasNonLinearSegments(ref result);

			return result;
		}

		private int ReportCorruptNonLinearSegments([NotNull] ISegmentCollection segments,
		                                           [NotNull] IRow row)
		{
			return ! HasNonLinearSegments(segments)
				       ? NoError
				       : GetSegments(segments).Sum(segment => CheckSegment(segment, row));
		}

		[NotNull]
		private static IEnumerable<ISegment> GetSegments(
			[NotNull] ISegmentCollection segments)
		{
			int segmentCount = segments.SegmentCount;

			for (int i = 0; i < segmentCount; i++)
			{
				yield return segments.get_Segment(i);
			}
		}

		private int CheckSegment([NotNull] ISegment segment, [NotNull] IRow row)
		{
			esriGeometryType segmentType = segment.GeometryType;

			switch (segmentType)
			{
				case esriGeometryType.esriGeometryCircularArc:
					return CheckCircularArc((ICircularArc) segment, row);

				case esriGeometryType.esriGeometryEllipticArc:
					// add checks as needed
					return NoError;

				case esriGeometryType.esriGeometryBezier3Curve:
					// add checks as needed
					return NoError;

				default:
					return NoError;
			}
		}

		private int CheckCircularArc([NotNull] ICircularArc circularArc,
		                             [NotNull] IRow row)
		{
			IPoint centerPoint = circularArc.CenterPoint;

			if (centerPoint == null || centerPoint.IsEmpty)
			{
				return ReportError("Circular arc has no center point",
				                   GetErrorGeometry((ISegment) circularArc),
				                   Codes[Code.CicularArc_NoCenterPoint],
				                   TestUtils.GetShapeFieldName(row),
				                   row);
			}

			if (circularArc.ChordHeight < _minimumChordHeight)
			{
				return ReportError(
					string.Format("Chord height of circular arc is too small ({0})",
					              FormatLengthComparison(circularArc.ChordHeight, "<",
					                                     _minimumChordHeight,
					                                     _spatialReference)),
					GetErrorGeometry((ISegment) circularArc),
					Codes[Code.CircularArc_ChordHeightTooSmall],
					TestUtils.GetShapeFieldName(row),
					row);
			}

			// add checks as needed
			return NoError;
		}

		[CanBeNull]
		private IGeometry GetErrorGeometry([NotNull] ISegment segment)
		{
			IPoint fromPoint = segment.FromPoint;
			IPoint toPoint = segment.ToPoint;

			if (fromPoint != null && ! fromPoint.IsEmpty &&
			    toPoint != null && ! toPoint.IsEmpty)
			{
				return GeometryFactory.CreateLine(fromPoint, toPoint);
			}

			return TestUtils.GetEnlargedExtentPolygon(segment, _xyTolerance);
		}
	}
}
