using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
	[UsedImplicitly]
	[GeometryTest]
	public class QaMpVerticalFaces : ContainerTest
	{
		private readonly double _nearCosinus;
		private readonly double _toleranceSinus;
		private readonly double _xyTolerance;
		private readonly double _toleranceAngleRad;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NotSufficientlyVertical = "NotSufficientlyVertical";

			public Code() : base("MpVerticalFaces") { }
		}

		#endregion

		[Doc("QaMpVerticalFaces_0")]
		public QaMpVerticalFaces(
			[Doc("QaMpVerticalFaces_multiPatchClass")] [NotNull]
			IFeatureClass multiPatchClass,
			[Doc("QaMpVerticalFaces_nearAngle")] double nearAngle,
			[Doc("QaMpVerticalFaces_toleranceAngle")]
			double toleranceAngle)
			: base((ITable) multiPatchClass)
		{
			AngleUnit = AngleUnit.Degree;

			double nearAngleRad = MathUtils.ToRadians(nearAngle);
			_toleranceAngleRad = MathUtils.ToRadians(toleranceAngle);

			_nearCosinus = Math.Cos(nearAngleRad);
			_toleranceSinus = Math.Sin(_toleranceAngleRad);

			_xyTolerance = GeometryUtils.GetXyTolerance(multiPatchClass);
		}

		public override bool IsQueriedTable(int tableIndex)
		{
			return false;
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			return false;
		}

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			var feature = row as IFeature;
			if (feature == null)
			{
				return NoError;
			}

			var multiPatch = feature.Shape as IMultiPatch;
			if (multiPatch == null)
			{
				return NoError;
			}

			var errorCount = 0;

			VerticalFaceProvider verticalFaceProvider = GetPlaneProvider(feature);

			VerticalFace verticalFace;
			while ((verticalFace = verticalFaceProvider.ReadFace()) != null)
			{
				Plane plane = verticalFace.Plane;

				WKSPointZ normal = plane.GetNormalVector();
				double verticalCosinus = Math.Abs(normal.Z);

				if (verticalCosinus > _nearCosinus)
				{
					continue;
				}

				if (verticalCosinus <= _toleranceSinus)
				{
					continue;
				}

				double height = verticalFace.Height;
				double toleranceSinus = _xyTolerance / height;

				if (verticalCosinus <= toleranceSinus)
				{
					continue;
				}

				double nonVerticalAngle = Math.Asin(verticalCosinus);

				string description = GetIssueDescription(nonVerticalAngle);
				IGeometry errorGeometry = verticalFaceProvider.GetErrorGeometry();

				errorCount += ReportError(description, errorGeometry,
				                          Codes[Code.NotSufficientlyVertical],
				                          TestUtils.GetShapeFieldName(row),
				                          row);
			}

			return errorCount;
		}

		[NotNull]
		private string GetIssueDescription(double nonVerticalAngle)
		{
			string angleFormat = FormatUtils.CompareFormat(
				MathUtils.ToDegrees(nonVerticalAngle), ">",
				MathUtils.ToDegrees(_toleranceAngleRad), "N1");

			const string format =
				"The face is almost, but not sufficently vertical ( difference from vertical {0} > {1})";
			return string.Format(format,
			                     FormatAngle(nonVerticalAngle, angleFormat),
			                     FormatAngle(_toleranceAngleRad, angleFormat));
		}

		[NotNull]
		private static VerticalFaceProvider GetPlaneProvider([NotNull] IFeature feature)
		{
			var indexedMultiPatchFeature = feature as IIndexedMultiPatchFeature;

			IIndexedMultiPatch indexedMultiPatch =
				indexedMultiPatchFeature?.IndexedMultiPatch ??
				QaGeometryUtils.CreateIndexedMultiPatch((IMultiPatch) feature.Shape);

			return new PartVerticalFaceProvider(indexedMultiPatch);
		}

		/// <summary>
		/// use private or by unit tests only
		/// </summary>
		private class VerticalFace
		{
			public VerticalFace([NotNull] Plane plane, double height)
			{
				Plane = plane;
				Height = height;
			}

			[NotNull]
			public Plane Plane { get; }

			public double Height { get; }
		}

		/// <summary>
		/// use private or by unit tests only
		/// </summary>
		private abstract class VerticalFaceProvider
		{
			[CanBeNull]
			public abstract VerticalFace ReadFace();

			[NotNull]
			public abstract IGeometry GetErrorGeometry();
		}

		private class PartVerticalFaceProvider : VerticalFaceProvider
		{
			[NotNull] private readonly IIndexedSegments _indexedSegments;
			[NotNull] private readonly IEnumerator<SegmentProxy> _segmentsEnum;

			[CanBeNull] private List<SegmentProxy> _latestPartSegments;
			private bool _enumValid;

			public PartVerticalFaceProvider([NotNull] IIndexedSegments indexedSegments)
			{
				_indexedSegments = indexedSegments;
				_segmentsEnum = _indexedSegments.GetSegments().GetEnumerator();
				_enumValid = _segmentsEnum.MoveNext();
			}

			public override VerticalFace ReadFace()
			{
				_latestPartSegments = null;

				if (! _enumValid)
				{
					return null;
				}

				int currentPart = Assert.NotNull(_segmentsEnum.Current).PartIndex;
				int segmentCount = _indexedSegments.GetPartSegmentCount(currentPart);

				var partSegments = new List<SegmentProxy>(segmentCount) {_segmentsEnum.Current};

				if (_segmentsEnum.Current == null)
				{
					return null;
				}

				double zMin = _segmentsEnum.Current.GetStart(true)[2];
				double zMax = zMin;

				while ((_enumValid = _segmentsEnum.MoveNext()) &&
				       Assert.NotNull(_segmentsEnum.Current).PartIndex == currentPart)
				{
					partSegments.Add(_segmentsEnum.Current);

					if (_segmentsEnum.Current != null)
					{
						double z = _segmentsEnum.Current.GetStart(true)[2];
						zMin = Math.Min(z, zMin);
						zMax = Math.Max(z, zMax);
					}
				}

				Plane plane = QaGeometryUtils.CreatePlane((IEnumerable<SegmentProxy>) partSegments);
				_latestPartSegments = partSegments;

				return new VerticalFace(plane, zMax - zMin);
			}

			public override IGeometry GetErrorGeometry()
			{
				return SegmentUtils.CreateMultiPatch(Assert.NotNull(_latestPartSegments));
			}
		}
	}
}
