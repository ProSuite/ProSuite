using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.QA.Core.TestCategories;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	[ZValuesTest]
	public class Qa3dConstantZ : ContainerTest
	{
		private readonly IEnvelope _envelopeTemplate = new EnvelopeClass();

		private readonly double _tolerance;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string ZDifferenceOutsideTolerance_NotAllValuesUnique =
				"ZDifferenceOutsideTolerance.NotAllValuesUnique";

			public const string ZDifferenceOutsideTolerance_AllValuesUnique =
				"ZDifferenceOutsideTolerance.AllValuesUnique";

			public Code() : base("ThreeDConstantZ") { }
		}

		#endregion

		[Doc(nameof(DocStrings.Qa3dConstantZ_0))]
		public Qa3dConstantZ(
			[Doc(nameof(DocStrings.Qa3dConstantZ_featureClass))] IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.Qa3dConstantZ_tolerance))] double tolerance)
			: base((IReadOnlyTable) featureClass)
		{
			Assert.ArgumentCondition(tolerance >= 0, "tolerance must be >= 0");

			_tolerance = tolerance;
		}

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
			IGeometry shape = ((IReadOnlyFeature) row).Shape;
			if (! ((IZAware) shape).ZAware)
			{
				// Not z aware, consider as constant
				return 0;
			}

			var points = shape as IPointCollection4;
			if (points == null)
			{
				return 0;
			}

			shape.QueryEnvelope(_envelopeTemplate);

			double zMin = _envelopeTemplate.ZMin;
			double zMax = _envelopeTemplate.ZMax;
			double zDifference = zMax - zMin;

			if (zDifference <= _tolerance)
			{
				// z difference is ok
				return 0;
			}

			// z difference is not ok.

			// Get the most frequent Z value
			int modalZPointCount;
			double modalZ = GetMostFrequentZValue(points, out modalZPointCount);

			double dZ = _tolerance / 2;
			double minAllowedZ = modalZ - dZ;
			double maxAllowedZ = modalZ + dZ;

			// report consecutive points that are further away than half the tolerance from the most frequent Z value
			// - if more than half the points are off: report the entire geometry once as error
			IEnumerable<List<WKSPointZ>> errorPointSequences = GetErrorPointSequences(shape,
			                                                                          minAllowedZ,
			                                                                          maxAllowedZ);

			int totalPointCount = points.PointCount;
			IList<List<WKSPointZ>> errorPointSequencesList =
				errorPointSequences as IList<List<WKSPointZ>> ?? errorPointSequences.ToList();

			int errorPointCount = GetErrorPointCount(errorPointSequencesList);

			string errorMessage = GetErrorMessage(zDifference, modalZ, modalZPointCount,
			                                      totalPointCount, errorPointCount,
			                                      zMin, zMax);
			IssueCode issueCode = GetIssueCode(modalZPointCount);

			if (errorPointCount > points.PointCount / 2)
			{
				// more than half the points are errors --> don't report individually	
				IGeometry errorGeometry = GeometryFactory.Clone(shape);

				return ReportError(errorMessage, errorGeometry, issueCode,
				                   TestUtils.GetShapeFieldName(row), row);
			}

			return errorPointSequencesList.Sum(
				errorPoints => ReportError(errorMessage, GetErrorGeometry(errorPoints),
				                           issueCode, TestUtils.GetShapeFieldName(row), row));
		}

		private static IssueCode GetIssueCode(int modalZPointCount)
		{
			return modalZPointCount > 1
				       ? Codes[Code.ZDifferenceOutsideTolerance_NotAllValuesUnique]
				       : Codes[Code.ZDifferenceOutsideTolerance_AllValuesUnique];
		}

		private string GetErrorMessage(double zDifference, double modalZ,
		                               int modalZPointCount,
		                               int totalPointCount, int errorPointCount,
		                               double zMin, double zMax)
		{
			var sb = new StringBuilder();

			sb.AppendFormat("Z difference on feature is larger than {0} " +
			                "(Min: {1:N2} Max: {2:N2} Difference: {3:N3})",
			                _tolerance, zMin, zMax, zDifference);

			if (modalZPointCount > 1)
			{
				sb.AppendLine();
				sb.AppendFormat(
					"Most frequent Z value on feature: {0:N3} (on {1} of {2} point{3})",
					modalZ, modalZPointCount, totalPointCount, totalPointCount == 1
						                                           ? string.Empty
						                                           : "s");
				sb.AppendLine();
				sb.AppendFormat(
					"A total of {0} point{1} more than half the tolerance off that value",
					errorPointCount, errorPointCount == 1
						                 ? " is"
						                 : "s are");
			}
			else if (totalPointCount > 1)
			{
				sb.AppendLine();
				sb.AppendFormat("All {0} points have distinct Z values", totalPointCount);
			}

			return sb.ToString();
		}

		[NotNull]
		private static IGeometry GetErrorGeometry([NotNull] List<WKSPointZ> errPoints)
		{
			IGeometry result = new MultipointClass();

			WKSPointZ[] errArray = errPoints.ToArray();
			GeometryUtils.SetWKSPointZs((IPointCollection4) result, errArray);

			return result;
		}

		private static int GetErrorPointCount(
			[NotNull] IEnumerable<List<WKSPointZ>> errorPointSequences)
		{
			int count = 0;

			foreach (List<WKSPointZ> errorPointSequence in errorPointSequences)
			{
				count = count + errorPointSequence.Count;
			}

			return count;
		}

		[NotNull]
		private static IEnumerable<List<WKSPointZ>> GetErrorPointSequences(
			[NotNull] IGeometry geometry,
			double minAllowedZ,
			double maxAllowedZ)
		{
			var result = new List<List<WKSPointZ>>();

			if (geometry is IMultipoint || geometry is IMultiPatch)
			{
				result.Add(GetErrorPoints((IPointCollection4) geometry, minAllowedZ, maxAllowedZ));
			}
			else if (geometry is IPolycurve)
			{
				foreach (IGeometry part in GeometryUtils.GetParts((IGeometryCollection) geometry))
				{
					AddErrorPointSequences((IPointCollection4) part, minAllowedZ, maxAllowedZ,
					                       result);

					Marshal.ReleaseComObject(part);
				}
			}
			else
			{
				Assert.Fail("Unsupported geometry type: {0}", geometry.GeometryType);
			}

			return result;
		}

		private static void AddErrorPointSequences([NotNull] IPointCollection4 curvePoints,
		                                           double minAllowedZ,
		                                           double maxAllowedZ,
		                                           [NotNull] ICollection<List<WKSPointZ>>
			                                           result)
		{
			WKSPointZ[] wksPoints = GetPointArray(curvePoints);

			List<WKSPointZ> currentErrorSequence = null;

			foreach (WKSPointZ wksPointZ in wksPoints)
			{
				if (IsError(wksPointZ.Z, minAllowedZ, maxAllowedZ))
				{
					if (currentErrorSequence == null)
					{
						currentErrorSequence = new List<WKSPointZ>();
					}

					currentErrorSequence.Add(wksPointZ);
				}
				else
				{
					if (currentErrorSequence != null)
					{
						result.Add(currentErrorSequence);
						currentErrorSequence = null;
					}
				}
			}

			// last may have been error
			if (currentErrorSequence != null)
			{
				result.Add(currentErrorSequence);
			}
		}

		[NotNull]
		private static List<WKSPointZ> GetErrorPoints([NotNull] IPointCollection4 points,
		                                              double minAllowedZ,
		                                              double maxAllowedZ)
		{
			WKSPointZ[] wksPoints = GetPointArray(points);

			var result = new List<WKSPointZ>();

			foreach (WKSPointZ wksPointZ in wksPoints)
			{
				if (IsError(wksPointZ.Z, minAllowedZ, maxAllowedZ))
				{
					result.Add(wksPointZ);
				}
			}

			return result;
		}

		private static bool IsError(double z, double minAllowedZ, double maxAllowedZ)
		{
			return z < minAllowedZ || z > maxAllowedZ;
		}

		private static double GetMostFrequentZValue([NotNull] IPointCollection4 points,
		                                            out int mostFrequentZValuePointCount)
		{
			Assert.ArgumentNotNull(points, nameof(points));

			int pointCount = points.PointCount;

			WKSPointZ[] wks = GetPointArray(points);

			var zFrequency = new Dictionary<double, int>();

			int maxFrequency = int.MinValue;
			double maxFrequencyZ = double.NaN;

			for (int pointIndex = 0; pointIndex < pointCount; pointIndex++)
			{
				double z = wks[pointIndex].Z;

				int count;
				count = zFrequency.TryGetValue(z, out count)
					        ? count + 1
					        : 1;

				zFrequency[z] = count;

				if (count > maxFrequency)
				{
					maxFrequency = count;
					maxFrequencyZ = z;
				}
			}

			mostFrequentZValuePointCount = maxFrequency;
			return maxFrequencyZ;
		}

		[NotNull]
		private static WKSPointZ[] GetPointArray([NotNull] IPointCollection4 points)
		{
			var pointArray = new WKSPointZ[points.PointCount];

			GeometryUtils.QueryWKSPointZs(points, pointArray);

			return pointArray;
		}
	}
}
