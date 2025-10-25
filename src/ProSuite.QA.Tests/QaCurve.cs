using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Reports non-linear polycurve segments as errors
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaCurve : ContainerTest
	{
		private readonly string _shapeFieldName;

		[NotNull] private static readonly Dictionary<esriGeometryType, SegmentTypeInfo>
			_segmentTypes = GetSegmentTypeInfos();

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NonLinearSegments = "NonLinearSegments";
			public const string NonLinearSegments_Bezier = "NonLinearSegments.Bezier";

			public const string NonLinearSegments_CircularArc =
				"NonLinearSegments.CircularArc";

			public const string NonLinearSegments_EllipticArc =
				"NonLinearSegments.EllipticArc";

			public Code() : base("SegmentTypes") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaCurve_0))]
		public QaCurve(
			[Doc(nameof(DocStrings.QaCurve_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass)
			: base(featureClass)
		{
			_shapeFieldName = featureClass.ShapeFieldName;
		}

		[InternallyUsedTest]
		public QaCurve(
		[NotNull] QaCurveDefinition definition)
			: this((IReadOnlyFeatureClass)definition.FeatureClass)
		{
			AllowedNonLinearSegmentTypes = definition.AllowedNonLinearSegmentTypes;
			GroupIssuesBySegmentType = definition.GroupIssuesBySegmentType;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaCurve_AllowedNonLinearSegmentTypes))]
		public IList<NonLinearSegmentType> AllowedNonLinearSegmentTypes { get; set; }

		[TestParameter(false)]
		[Doc(nameof(DocStrings.QaCurve_GroupIssuesBySegmentType))]
		public bool GroupIssuesBySegmentType { get; set; }

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

			IGeometry shape = feature.Shape;

			var segments = shape as ISegmentCollection;
			if (segments == null)
			{
				return NoError;
			}

			var hasNonLinearSegments = false;
			segments.HasNonLinearSegments(ref hasNonLinearSegments);

			return ! hasNonLinearSegments
				       ? NoError
				       : ReportCurveSegments(segments, row, shape.SpatialReference);
		}

		[NotNull]
		private static Dictionary<esriGeometryType, SegmentTypeInfo> GetSegmentTypeInfos()
		{
			return new Dictionary<esriGeometryType, SegmentTypeInfo>
			       {
				       {
					       esriGeometryType.esriGeometryBezier3Curve,
					       new SegmentTypeInfo
					       {
						       Type = NonLinearSegmentType.Bezier,
						       Description = "Bezier curve",
						       IssueCode = Code.NonLinearSegments_Bezier
					       }
				       },
				       {
					       esriGeometryType.esriGeometryCircularArc,
					       new SegmentTypeInfo
					       {
						       Type = NonLinearSegmentType.CircularArc,
						       Description = "Circular arc",
						       IssueCode = Code.NonLinearSegments_CircularArc
					       }
				       },
				       {
					       esriGeometryType.esriGeometryEllipticArc,
					       new SegmentTypeInfo
					       {
						       Type = NonLinearSegmentType.EllipticArc,
						       Description = "Elliptic arc",
						       IssueCode = Code.NonLinearSegments_EllipticArc
					       }
				       }
			       };
		}

		private int ReportCurveSegments([NotNull] ISegmentCollection segments,
		                                [NotNull] IReadOnlyRow row,
		                                [CanBeNull] ISpatialReference spatialReference)
		{
			var errorCount = 0;
			object missing = Type.Missing;

			// IEnumSegments ist massiv schneller als .get_Segment
			IEnumSegment enumSegments = segments.EnumSegments;
			enumSegments.Reset();

			var partIndex = 0;
			var segmentIndex = 0;
			ISegment segment;
			enumSegments.Next(out segment, ref partIndex, ref segmentIndex);

			// this will be null when no open sequence of error segments exist, 
			// and non-null while such a sequence exists and has not yet been reported
			IPolyline consecutiveSegmentsPolyline = null;
			int currentPartIndex = partIndex;

			var latestSegmentType = esriGeometryType.esriGeometryNull;

			while (segment != null)
			{
				if (! IsAllowedCurveType(segment.GeometryType))
				{
					if (consecutiveSegmentsPolyline != null && GroupIssuesBySegmentType &&
					    segment.GeometryType != latestSegmentType)
					{
						// report the consecutive curves found so far
						errorCount += ReportError(row, consecutiveSegmentsPolyline);

						// get ready for next sequence of curves
						consecutiveSegmentsPolyline = null;
					}

					if (consecutiveSegmentsPolyline == null)
					{
						consecutiveSegmentsPolyline =
							GeometryFactory.CreatePolyline(spatialReference);
					}

					// important: the segment enumerator is *recycling*, it is mandatory
					// to create a copy of the segment. Even if it was not recycling it's better to
					// clone, as AddSegment() just stores a reference to the passed segment (in 
					// the source geometry!)
					ISegment segmentCopy = GeometryFactory.Clone(segment);

					((ISegmentCollection) consecutiveSegmentsPolyline).AddSegment(
						segmentCopy, ref missing, ref missing);
				}
				else
				{
					// it's a linear or allowed segment

					if (consecutiveSegmentsPolyline != null)
					{
						// report the consecutive curves found so far
						errorCount += ReportError(row, consecutiveSegmentsPolyline);

						// get ready for next sequence of curves
						consecutiveSegmentsPolyline = null;
					}
				}

				latestSegmentType = segment.GeometryType;

				// release the segment, otherwise "pure virtual function call" occurs 
				// when there are certain circular arcs (IsLine == true ?)
				Marshal.ReleaseComObject(segment);

				enumSegments.Next(out segment, ref partIndex, ref segmentIndex);

				if (segment != null && partIndex != currentPartIndex)
				{
					// next segment is on another part
					if (consecutiveSegmentsPolyline != null)
					{
						// there is an ongoing sequence of curves, report it
						errorCount += ReportError(row, consecutiveSegmentsPolyline);

						// get ready for next sequence of curves
						consecutiveSegmentsPolyline = null;
					}

					currentPartIndex = partIndex;
				}
			}

			if (consecutiveSegmentsPolyline != null)
			{
				// report the last sequence
				errorCount += ReportError(row, consecutiveSegmentsPolyline);
			}

			return errorCount;
		}

		private bool IsAllowedCurveType(esriGeometryType geometryType)
		{
			if (geometryType == esriGeometryType.esriGeometryLine)
			{
				return true;
			}

			if (AllowedNonLinearSegmentTypes == null)
			{
				return false;
			}

			SegmentTypeInfo segmentType;
			if (! _segmentTypes.TryGetValue(geometryType, out segmentType))
			{
				throw new InvalidOperationException("Unhandled geometry type: " + geometryType);
			}

			return AllowedNonLinearSegmentTypes.Any(
				nonLinearSegmentType => nonLinearSegmentType == segmentType.Type);
		}

		private int ReportError([NotNull] IReadOnlyRow row,
		                        [NotNull] IPolyline consecutiveErrorSegments)
		{
			var segments = (ISegmentCollection) consecutiveErrorSegments;

			int segmentCount = segments.SegmentCount;

			string description;
			string issueCode;

			if (GroupIssuesBySegmentType)
			{
				esriGeometryType geometryType = segments.Segment[0].GeometryType;
				SegmentTypeInfo segmentType = _segmentTypes[geometryType];

				description = segmentCount > 1
					              ? string.Format("{0} consecutive {1} segments",
					                              segmentCount,
					                              segmentType.Description.ToLower())
					              : string.Format("{0} segment",
					                              segmentType.Description);

				issueCode = segmentType.IssueCode;
			}
			else
			{
				description = segmentCount > 1
					              ? string.Format("{0} consecutive non-linear segments",
					                              segmentCount)
					              : "Non-linear segment";
				issueCode = Code.NonLinearSegments;
			}

			// use from point of sequence as error geometry, if Simplify caused the 
			// polyline to become empty (short segments)
			IGeometry errorGeometry = GetErrorGeometry(consecutiveErrorSegments);

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row), errorGeometry,
				Codes[issueCode], _shapeFieldName);
		}

		[NotNull]
		private static IGeometry GetErrorGeometry(
			[NotNull] IPolyline consecutiveErrorSegments)
		{
			IPoint fromPoint = consecutiveErrorSegments.FromPoint;

			consecutiveErrorSegments.SimplifyNetwork();

			return consecutiveErrorSegments.IsEmpty
				       ? (IGeometry) fromPoint
				       : consecutiveErrorSegments;
		}

		private class SegmentTypeInfo
		{
			public NonLinearSegmentType Type { get; set; }
			public string Description { get; set; }
			public string IssueCode { get; set; }
		}
	}
}
