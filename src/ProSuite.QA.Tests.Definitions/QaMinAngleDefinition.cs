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
	[UsedImplicitly]
	[IntersectionParameterTest]
	public class QaMinAngleDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef PolylineClass { get; }
		public IList<IFeatureClassSchemaDef> PolylineClasses { get; }
		public double Limit { get; }
		public bool Is3D { get; }

		private const AngleUnit _defaultAngularUnit = DefaultAngleUnit;

		[Doc(nameof(DocStrings.QaMinAngle_0))]
		public QaMinAngleDefinition(
			[Doc(nameof(DocStrings.QaMinAngle_polylineClass))]
			IFeatureClassSchemaDef polylineClass,
			[Doc(nameof(DocStrings.QaMinAngle_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaMinAngle_is3D))]
			bool is3D)
			: base(polylineClass)
		{
			PolylineClass = polylineClass ?? throw new ArgumentNullException(nameof(polylineClass));
			Limit = limit;
			Is3D = is3D;
			PolylineClasses = new List<IFeatureClassSchemaDef> { polylineClass };
		}

		[Doc(nameof(DocStrings.QaMinAngle_1))]
		public QaMinAngleDefinition(
				[Doc(nameof(DocStrings.QaMinAngle_polylineClasses))]
				IList<IFeatureClassSchemaDef> polylineClasses,
				[Doc(nameof(DocStrings.QaMinAngle_limit))]
				double limit,
				[Doc(nameof(DocStrings.QaMinAngle_is3D))]
				bool is3D)
			// ReSharper disable once PossiblyMistakenUseOfParamsMethod
			: base(CastToTables(polylineClasses))
		{
			PolylineClasses = polylineClasses ??
			                  throw new ArgumentNullException(nameof(polylineClasses));
			Limit = limit;
			Is3D = is3D;
		}

		[Doc(nameof(DocStrings.QaMinAngle_1))]
		public QaMinAngleDefinition(
				[Doc(nameof(DocStrings.QaMinAngle_polylineClasses))]
				IList<IFeatureClassSchemaDef> polylineClasses,
				[Doc(nameof(DocStrings.QaMinAngle_limit))]
				double limit)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polylineClasses, limit, false) { }

		[TestParameter(DefaultAngleUnit)]
		[Doc(nameof(DocStrings.QaLineIntersectAngle_AngularUnit))]
		public AngleUnit AngularUnit { get; set; } = DefaultAngleUnit;
	}
}
