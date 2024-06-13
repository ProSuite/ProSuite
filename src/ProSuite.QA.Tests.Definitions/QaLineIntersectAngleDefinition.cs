using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.ParameterTypes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check if there are any crossing lines that have a too small angle 
	/// between each other
	/// </summary>
	[IntersectionParameterTest]
	[UsedImplicitly]
	public class QaLineIntersectAngleDefinition : AlgorithmDefinition
	{
		private const AngleUnit _defaultAngularUnit = DefaultAngleUnit;

		private readonly bool _is3D;
		private readonly double _limitCstr;

		public IList<IFeatureClassSchemaDef> PolylineClasses { get; }
		public IFeatureClassSchemaDef PolylineClass { get; }

		public double Limit { get; }
		public bool Is3d { get; }

		#region Constructors

		[Doc(nameof(DocStrings.QaLineIntersectAngle_0))]
		public QaLineIntersectAngleDefinition(
			[Doc(nameof(DocStrings.QaLineIntersectAngle_polylineClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> polylineClasses,
			[Doc(nameof(DocStrings.QaLineIntersectAngle_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaLineIntersectAngle_is3D))]
			bool is3d)
			: base(polylineClasses)
		{
			PolylineClasses = polylineClasses;
			Limit = limit;
			Is3d = is3d;

			_limitCstr = limit;

			_is3D = is3d;
		}

		[Obsolete(
			"Incorrect parameter name will be renamed in a future release, use other constructor"
		)]
		public QaLineIntersectAngleDefinition([NotNull] IFeatureClassSchemaDef table, double limit,
		                                      bool is3d)
			: this((new[] { table }), limit, is3d) { }

		[Doc(nameof(DocStrings.QaLineIntersectAngle_0))]
		public QaLineIntersectAngleDefinition(
				[Doc(nameof(DocStrings.QaLineIntersectAngle_polylineClasses))] [NotNull]
				IList<IFeatureClassSchemaDef> polylineClasses,
				[Doc(nameof(DocStrings.QaLineIntersectAngle_limit))]
				double limit)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polylineClasses, limit, false) { }

		[Doc(nameof(DocStrings.QaLineIntersectAngle_0))]
		public QaLineIntersectAngleDefinition(
			[Doc(nameof(DocStrings.QaLineIntersectAngle_polylineClass))] [NotNull]
			IFeatureClassSchemaDef polylineClass,
			[Doc(nameof(DocStrings.QaLineIntersectAngle_limit))]
			double limit)
			: this(new[] { polylineClass }, limit)
		{
			PolylineClass = polylineClass;
			Limit = limit;
		}

		#endregion

		[TestParameter(_defaultAngularUnit)]
		[Doc(nameof(DocStrings.QaLineIntersectAngle_AngularUnit))]
		public AngleUnit AngularUnit { get; set; } = _defaultAngularUnit;
	}
}
