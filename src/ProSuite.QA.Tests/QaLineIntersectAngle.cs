using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.SpatialRelations;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check if there are any crossing lines that have a too small angle 
	/// between each other
	/// </summary>
	[CLSCompliant(false)]
	[IntersectionParameterTest]
	[UsedImplicitly]
	public class QaLineIntersectAngle : QaSpatialRelationSelfBase
	{
		private readonly bool _is3D;
		private readonly double _limit;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string IntersectionAngleSmallerThanLimit =
				"IntersectionAngleSmallerThanLimit";

			public Code() : base("LineIntersectAngle") { }
		}

		#endregion

		#region Constructors

		[Doc("QaLineIntersectAngle_0")]
		public QaLineIntersectAngle(
			[Doc("QaLineIntersectAngle_polylineClasses")] [NotNull]
			IList<IFeatureClass>
				polylineClasses,
			[Doc("QaLineIntersectAngle_limit")] double limit,
			[Doc("QaLineIntersectAngle_is3D")] bool is3d)
			: base(polylineClasses, esriSpatialRelEnum.esriSpatialRelCrosses)
		{
			_limit = limit;
			_is3D = is3d;
		}

		[Obsolete(
			"Incorrect parameter name will be renamed in a future release, use other constructor"
		)]
		public QaLineIntersectAngle([NotNull] IFeatureClass table, double limit, bool is3d)
			: this(new[] {table}, limit, is3d) { }

		[Doc("QaLineIntersectAngle_0")]
		public QaLineIntersectAngle(
				[Doc("QaLineIntersectAngle_polylineClasses")] [NotNull]
				IList<IFeatureClass>
					polylineClasses,
				[Doc("QaLineIntersectAngle_limit")] double limit)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polylineClasses, limit, false) { }

		[Doc("QaLineIntersectAngle_0")]
		public QaLineIntersectAngle(
			[Doc("QaLineIntersectAngle_polylineClass")] [NotNull]
			IFeatureClass polylineClass,
			[Doc("QaLineIntersectAngle_limit")] double limit)
			: this(new[] {polylineClass}, limit) { }

		#endregion

		protected override int FindErrors(IRow row1, int tableIndex1,
		                                  IRow row2, int tableIndex2)
		{
			if (row1 == row2)
			{
				return NoError;
			}

			var polyline1 = (IPolyline) ((IFeature) row1).Shape;
			var polyline2 = (IPolyline) ((IFeature) row2).Shape;

			if (((IRelationalOperator) polyline1).Disjoint(polyline2))
			{
				return NoError;
			}

			var errorCount = 0;

			foreach (
				LineIntersection intersection in
				LineIntersectionUtils.GetIntersections(polyline1, polyline2, _is3D))
			{
				if (Math.Abs(intersection.DistanceAlongA) < double.Epsilon ||
				    Math.Abs(intersection.DistanceAlongA - 1.0) < double.Epsilon)
				{
					continue;
				}

				if (Math.Abs(intersection.DistanceAlongB) < double.Epsilon ||
				    Math.Abs(intersection.DistanceAlongB - 1.0) < double.Epsilon)
				{
					continue;
				}

				double angleRadians = intersection.Angle;

				if (angleRadians >= _limit)
				{
					// angle is allowed
					continue;
				}

				// The angle is smaller than limit. Report error
				string description = string.Format("Intersect angle {0} < {1}",
				                                   FormatAngle(angleRadians, "N2"),
				                                   FormatAngle(_limit, "N2"));

				errorCount += ReportError(description,
				                          GeometryFactory.Clone(intersection.At),
				                          Codes[Code.IntersectionAngleSmallerThanLimit],
				                          TestUtils.GetShapeFieldName(row1),
				                          new object[] {MathUtils.ToDegrees(angleRadians)},
				                          row1, row2);
			}

			return errorCount;
		}
	}
}