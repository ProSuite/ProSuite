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
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	[MValuesTest]
	public class QaMeasures : ContainerTest
	{
		private readonly bool _hasM;
		private readonly esriGeometryType _shapeType;
		private readonly IPoint _queryPoint = new PointClass();
		private readonly string _shapeFieldName;

		private readonly double _invalidValue;

		private WKSPointVA[] _sourcePoints;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string UndefinedMValues = "UndefinedMValues";
			public const string InvalidMValues = "InvalidMValues";

			public Code() : base("MeasureValues") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMeasures_0))]
		public QaMeasures(
				[Doc(nameof(DocStrings.QaMeasures_featureClass))] [NotNull]
				IReadOnlyFeatureClass featureClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, double.NaN) { }

		[Doc(nameof(DocStrings.QaMeasures_1))]
		public QaMeasures(
			[Doc(nameof(DocStrings.QaMeasures_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMeasures_invalidValue))]
			double invalidValue)
			: base(featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			_hasM = DatasetUtils.GetGeometryDef(featureClass).HasM;
			_shapeType = featureClass.ShapeType;
			_shapeFieldName = featureClass.ShapeFieldName;

			_invalidValue = invalidValue;
		}

		[InternallyUsedTest]
		public QaMeasures(
			[NotNull] QaMeasuresDefinition definition)
			: this((IReadOnlyFeatureClass) definition.FeatureClass,
			       definition.InvalidValue) { }

		#region Overrides of ContainerTest

		public override bool IsQueriedTable(int tableIndex)
		{
			return false;
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			return false;
		}

		protected virtual bool IsKnownValid(IMAware mAware)
		{
			return double.IsNaN(_invalidValue) && mAware.MSimple;
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			if (! _hasM)
			{
				return NoError;
			}

			var feature = row as IReadOnlyFeature;
			if (feature == null)
			{
				return NoError;
			}

			IGeometry shape = feature.Shape;

			var mAware = (IMAware) shape;
			if (! mAware.MAware)
			{
				// This can actually happen, despite the feature class having M values!
				return ReportError(
					"Geometry is not M-aware", InvolvedRowUtils.GetInvolvedRows(row),
					GeometryFactory.Clone(shape), Codes[Code.UndefinedMValues], _shapeFieldName);
			}

			Assert.True(mAware.MAware, "The geometry is not M-aware");

			var errorCount = 0;

			if (IsKnownValid(mAware))
			{
				return NoError;
			}

			switch (_shapeType)
			{
				case esriGeometryType.esriGeometryPoint:
					errorCount += ReportInvalidPoint((IPoint) shape, row);
					break;

				case esriGeometryType.esriGeometryMultipoint:
				case esriGeometryType.esriGeometryMultiPatch:
					errorCount += ReportInvalidPoints((IPointCollection) shape, row);
					break;

				case esriGeometryType.esriGeometryPolyline:
				case esriGeometryType.esriGeometryPolygon:
					errorCount += ReportInvalidSegments((ISegmentCollection) shape, row);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			return errorCount;
		}

		#endregion

		private int ReportInvalidPoint([NotNull] IPoint point,
		                               [NotNull] IReadOnlyRow row)
		{
			double m = point.M;
			string error;
			const bool isNewRow = true;
			IssueCode issueCode;

			if (! IsInvalidValue(m, row, isNewRow, out error, out issueCode))
			{
				return NoError;
			}

			string errorDescription;
			if (StringUtils.IsNotEmpty(error))
			{
				errorDescription = string.Format("Invalid M value: {0}", error);
			}
			else if (double.IsNaN(m))
			{
				errorDescription = "Undefined M value";
			}
			else
			{
				errorDescription = string.Format("Invalid M value: {0}", _invalidValue);
			}

			return ReportError(
				errorDescription, InvolvedRowUtils.GetInvolvedRows(row),
				GeometryFactory.Clone(point), issueCode, _shapeFieldName);
		}

		private int ReportInvalidPoints([NotNull] IPointCollection points,
		                                [NotNull] IReadOnlyRow row)
		{
			var points5 = points as IPointCollection5;
			if (points5 == null)
			{
				points5 = new MultipointClass();
				points5.AddPointCollection(points);
			}

			int pointCount = points.PointCount;
			if (_sourcePoints == null || _sourcePoints.Length < points.PointCount)
			{
				const int margin = 2000;
				_sourcePoints = new WKSPointVA[pointCount + margin];
			}

			points5.QueryWKSPointVA(0, pointCount, out _sourcePoints[0]);
			// This would be fast, but about 4* slower then the statement above
			//for (int i = 0; i < pointCount; i++)
			//{ 
			//	WKSPointVA wksPoint;
			//	points5.QueryWKSPointVA(i, 1, out wksPoint);

			Dictionary<IssueCode, ErrorPoints> errorPointsDict = null;
			for (var i = 0; i < pointCount; i++)
			{
				WKSPointVA wksPoint = _sourcePoints[i];

				string error;
				bool isNewRow = i == 0;
				IssueCode code;
				if (IsInvalidValue(wksPoint.m_m, row, isNewRow, out error, out code))
				{
					if (errorPointsDict == null)
					{
						errorPointsDict = new Dictionary<IssueCode, ErrorPoints>();
					}

					Assert.NotNull(code);
					ErrorPoints errorPoints;
					if (! errorPointsDict.TryGetValue(code, out errorPoints))
					{
						errorPoints = new ErrorPoints();
						errorPointsDict.Add(code, errorPoints);
					}

					errorPoints.Add(wksPoint, error);
				}
			}
			//ICollection<IPoint> errorPoints = MeasureUtils.GetPointsWithInvalidM(
			//	points, _queryPoint, invalidValue);

			if (errorPointsDict == null)
			{
				return NoError;
			}

			var errorCount = 0;
			foreach (KeyValuePair<IssueCode, ErrorPoints> pair in errorPointsDict)
			{
				IssueCode issueCode = pair.Key;

				ErrorPoints errorPoints = pair.Value;
				IPointCollection5 errorGeometry = new MultipointClass();
				errorGeometry.AddWKSPointVA(errorPoints.Points.Count,
				                            ref errorPoints.Points.ToArray()[0]);
				((IGeometry) errorGeometry).SpatialReference =
					((IGeometry) points).SpatialReference;

				string errorDescription = GetErrorDescription(issueCode, errorPoints.Points.Count,
				                                              errorPoints.Errors);

				errorCount += ReportError(
					errorDescription, InvolvedRowUtils.GetInvolvedRows(row),
					(IGeometry) errorGeometry, issueCode, _shapeFieldName);
			}

			return errorCount;
		}

		private class ErrorPoints
		{
			private const int maxErrors = 6;

			public readonly List<WKSPointVA> Points = new List<WKSPointVA>();
			public HashSet<string> Errors { get; private set; }

			public void Add(WKSPointVA wksPoint, string error)
			{
				Points.Add(wksPoint);

				if (StringUtils.IsNotEmpty(error))
				{
					if (Errors == null)
					{
						Errors = new HashSet<string>();
					}

					if (Errors.Count > maxErrors) { }
					else if (Errors.Count == maxErrors)
					{
						Errors.Add("...");
					}
					else if (! Errors.Contains(error))
					{
						Errors.Add(error);
					}
				}
			}
		}

		protected virtual bool IsInvalidValue(double m,
		                                      [NotNull] IReadOnlyRow row,
		                                      bool isNewRow,
		                                      [CanBeNull] out string error,
		                                      [CanBeNull] out IssueCode issueCode)
		{
			error = string.Empty;
			issueCode = null;
			if (MeasureUtils.IsInvalidValue(m, _invalidValue))
			{
				if (double.IsNaN(_invalidValue))
				{
					issueCode = Codes[Code.UndefinedMValues];
				}
				else
				{
					issueCode = Codes[Code.InvalidMValues];
				}

				return true;
			}

			return false;
		}

		[NotNull]
		private string GetErrorDescription(
			IssueCode issueCode,
			int invalidMVerticesCount,
			[CanBeNull] ICollection<string> errors = null)
		{
			int count = invalidMVerticesCount;

			if (errors != null && errors.Count > 0)
			{
				string error = StringUtils.Concatenate(errors, ";");
				return count == 1
					       ? string.Format("One vertex has an invalid M value: {0}", error)
					       : string.Format("{0} vertices have invalid M values: {1}",
					                       count, error);
			}

			if (issueCode == Codes[Code.UndefinedMValues])
			{
				return count == 1
					       ? "One vertex has an undefined M value"
					       : string.Format("{0} vertices have undefined M values",
					                       count);
			}

			return count == 1
				       ? string.Format("One vertex has an invalid M value: {0}", _invalidValue)
				       : string.Format("{0} vertices have invalid M values: {1}",
				                       count, _invalidValue);
		}

		[NotNull]
		private static string GetErrorDescription(
			double invalidValue,
			[NotNull] ICollection<ISegment> invalidMSegments,
			out IssueCode issueCode)
		{
			int count = invalidMSegments.Count;

			if (double.IsNaN(invalidValue))
			{
				issueCode = Codes[Code.UndefinedMValues];
				return count == 1
					       ? "One segment has an undefined M value"
					       : string.Format("{0} segments have undefined M values",
					                       count);
			}

			issueCode = Codes[Code.InvalidMValues];
			return count == 1
				       ? string.Format("One segment has an invalid M value: {0}", invalidValue)
				       : string.Format("{0} segments have invalid M values: {1}",
				                       count, invalidValue);
		}

		private int ReportInvalidSegments([NotNull] ISegmentCollection segments,
		                                  [NotNull] IReadOnlyRow row)
		{
			var errorCount = 0;

			List<ISegment> currentInvalidSequence = null;
			int currentPartIndex = -1;

			IEnumSegment enumSegment = segments.EnumSegments;

			int partIndex = -1;
			int segmentIndex = -1;
			ISegment segment;
			enumSegment.Next(out segment, ref partIndex, ref segmentIndex);

			while (segment != null)
			{
				if (currentInvalidSequence != null && partIndex != currentPartIndex)
				{
					// report sequence from previous part
					errorCount += ReportInvalidSequence(currentInvalidSequence, row, _invalidValue);
					currentInvalidSequence = null;
					currentPartIndex = -1;
				}

				if (MeasureUtils.HasInvalidMValue(segment, _queryPoint, _invalidValue))
				{
					if (currentInvalidSequence == null)
					{
						// initialize sequence
						currentInvalidSequence = new List<ISegment>();
						currentPartIndex = partIndex;
					}

					currentInvalidSequence.Add(GeometryFactory.Clone(segment));
				}
				else
				{
					if (currentInvalidSequence != null)
					{
						errorCount +=
							ReportInvalidSequence(currentInvalidSequence, row, _invalidValue);
						currentInvalidSequence = null;
						currentPartIndex = -1;
					}
				}

				// release the segment, otherwise "pure virtual function call" occurs 
				// when there are certain circular arcs (IsLine == true ?)
				Marshal.ReleaseComObject(segment);

				enumSegment.Next(out segment, ref partIndex, ref segmentIndex);
			}

			if (currentInvalidSequence != null)
			{
				// report sequence at end of segment collection
				errorCount += ReportInvalidSequence(currentInvalidSequence, row, _invalidValue);
			}

			return errorCount;
		}

		private int ReportInvalidSequence([NotNull] ICollection<ISegment> invalidMSegments,
		                                  [NotNull] IReadOnlyRow row,
		                                  double invalidValue)
		{
			Assert.ArgumentNotNull(invalidMSegments, nameof(invalidMSegments));
			Assert.ArgumentCondition(invalidMSegments.Count > 0, "Empty sequence");

			const bool cloneSegments = false;
			IPolyline errorGeometry = MeasureUtils.GetErrorGeometry(invalidMSegments,
				cloneSegments);

			IssueCode issueCode;
			string errorDescription = GetErrorDescription(invalidValue, invalidMSegments,
			                                              out issueCode);

			return ReportError(
				errorDescription, InvolvedRowUtils.GetInvolvedRows(row),
				errorGeometry, issueCode, _shapeFieldName);
		}
	}
}
