using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.ParameterTypes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.SpatialRelations;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check if there are any crossing lines that have a too small angle 
	/// between each other
	/// </summary>
	[IntersectionParameterTest]
	[UsedImplicitly]
	public class QaLineIntersectAngle : QaSpatialRelationSelfBase
	{
		private const AngleUnit _defaultAngularUnit = DefaultAngleUnit;

		private readonly bool _is3D;
		private readonly double _limitCstr;

		private double _limitRad;
		private AngleUnit _angularUnit;

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

		[Doc(nameof(DocStrings.QaLineIntersectAngle_0))]
		public QaLineIntersectAngle(
			[Doc(nameof(DocStrings.QaLineIntersectAngle_polylineClasses))] [NotNull]
			IList<IReadOnlyFeatureClass>
				polylineClasses,
			[Doc(nameof(DocStrings.QaLineIntersectAngle_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaLineIntersectAngle_is3D))]
			bool is3d)
			: base(polylineClasses, esriSpatialRelEnum.esriSpatialRelCrosses)
		{
			_limitCstr = limit;

			_is3D = is3d;
			AngularUnit = _defaultAngularUnit;
		}

		[Obsolete(
			"Incorrect parameter name will be renamed in a future release, use other constructor"
		)]
		public QaLineIntersectAngle([NotNull] IReadOnlyFeatureClass table, double limit, bool is3d)
			: this(new[] { table }, limit, is3d) { }

		[Doc(nameof(DocStrings.QaLineIntersectAngle_0))]
		public QaLineIntersectAngle(
				[Doc(nameof(DocStrings.QaLineIntersectAngle_polylineClasses))] [NotNull]
				IList<IReadOnlyFeatureClass>
					polylineClasses,
				[Doc(nameof(DocStrings.QaLineIntersectAngle_limit))]
				double limit)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polylineClasses, limit, false) { }

		[Doc(nameof(DocStrings.QaLineIntersectAngle_0))]
		public QaLineIntersectAngle(
			[Doc(nameof(DocStrings.QaLineIntersectAngle_polylineClass))] [NotNull]
			IReadOnlyFeatureClass polylineClass,
			[Doc(nameof(DocStrings.QaLineIntersectAngle_limit))]
			double limit)
			: this(new[] { polylineClass }, limit) { }

		#endregion

		[InternallyUsedTest]
		public QaLineIntersectAngle([NotNull] QaLineIntersectAngleDefinition definition)
			: this(definition.PolylineClasses.Cast<IReadOnlyFeatureClass>().ToList(),
			       definition.Limit, definition.Is3d)
		{
			AngularUnit = definition.AngularUnit;
		}

		[TestParameter(_defaultAngularUnit)]
		[Doc(nameof(DocStrings.QaLineIntersectAngle_AngularUnit))]
		public AngleUnit AngularUnit
		{
			get { return AngleUnit; }
			set
			{
				AngleUnit = value;
				_limitRad = -1; // gets initialized in next FindErrors()
			}
		}

		protected override int FindErrors(IReadOnlyRow row1, int tableIndex1,
		                                  IReadOnlyRow row2, int tableIndex2)
		{
			if (_limitRad <= 0)
			{
				_limitRad = FormatUtils.AngleInUnits2Radians(_limitCstr, AngleUnit);
			}

			if (row1 == row2)
			{
				return NoError;
			}

			var polyline1 = (IPolyline) ((IReadOnlyFeature) row1).Shape;
			var polyline2 = (IPolyline) ((IReadOnlyFeature) row2).Shape;

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

				if (angleRadians >= _limitRad)
				{
					// angle is allowed
					continue;
				}

				// The angle is smaller than limit. Report error
				string description = string.Format("Intersect angle {0} < {1}",
				                                   FormatAngle(angleRadians, "N2"),
				                                   FormatAngle(_limitRad, "N2"));

				errorCount += ReportError(
					description, InvolvedRowUtils.GetInvolvedRows(row1, row2),
					GeometryFactory.Clone(intersection.At),
					Codes[Code.IntersectionAngleSmallerThanLimit],
					TestUtils.GetShapeFieldName(row1),
					values: new object[] { MathUtils.ToDegrees(angleRadians) });
			}

			return errorCount;
		}
	}
}
