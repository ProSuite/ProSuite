using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check if there are two crossing lines within several different line layers
	/// </summary>
	[UsedImplicitly]
	[TopologyTest]
	[LinearNetworkTest]
	public class QaLineIntersectDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> PolylineClasses { get; }
		public string ValidRelationConstraint { get; }
		public AllowedEndpointInteriorIntersections AllowedEndpointInteriorIntersections { get; }
		
		public bool ReportOverlaps { get; }
		private const AllowedLineInteriorIntersections _defaultAllowedInteriorIntersections
			= AllowedLineInteriorIntersections.None;

		[Doc(nameof(DocStrings.QaLineIntersect_0))]
		public QaLineIntersectDefinition(
				[Doc(nameof(DocStrings.QaLineIntersect_polylineClasses))] [NotNull]
				IList<IFeatureClassSchemaDef>
					polylineClasses)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polylineClasses, null, AllowedEndpointInteriorIntersections.All, false) { }

		[Doc(nameof(DocStrings.QaLineIntersect_1))]
		public QaLineIntersectDefinition(
				[Doc(nameof(DocStrings.QaLineIntersect_polylineClass))] [NotNull]
				IFeatureClassSchemaDef polylineClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polylineClass, null) { }

		[Doc(nameof(DocStrings.QaLineIntersect_2))]
		public QaLineIntersectDefinition(
			[Doc(nameof(DocStrings.QaLineIntersect_polylineClasses))] [NotNull]
			IList<IFeatureClassSchemaDef>
				polylineClasses,
			[Doc(nameof(DocStrings.QaLineIntersect_validRelationConstraint))] [CanBeNull]
			string
				validRelationConstraint)
			: this(
				polylineClasses, validRelationConstraint,
				// ReSharper disable once IntroduceOptionalParameters.Global
				AllowedEndpointInteriorIntersections.All, false)
		{ }

		[Doc(nameof(DocStrings.QaLineIntersect_3))]
		public QaLineIntersectDefinition(
			[Doc(nameof(DocStrings.QaLineIntersect_polylineClass))] [NotNull]
			IFeatureClassSchemaDef polylineClass,
			[Doc(nameof(DocStrings.QaLineIntersect_validRelationConstraint))] [CanBeNull]
			string
				validRelationConstraint)
			: this(new[] { polylineClass }, validRelationConstraint) { }

		[Doc(nameof(DocStrings.QaLineIntersect_4))]
		public QaLineIntersectDefinition(
			[Doc(nameof(DocStrings.QaLineIntersect_polylineClasses))] [NotNull]
			IList<IFeatureClassSchemaDef>
				polylineClasses,
			[Doc(nameof(DocStrings.QaLineIntersect_validRelationConstraint))] [CanBeNull]
			string
				validRelationConstraint,
			[Doc(nameof(DocStrings.QaLineIntersect_allowedEndpointInteriorIntersections))]
			AllowedEndpointInteriorIntersections allowedEndpointInteriorIntersections,
			[Doc(nameof(DocStrings.QaLineIntersect_reportOverlaps))]
			bool reportOverlaps)
			: base(polylineClasses)
		{
			Assert.ArgumentNotNull(polylineClasses, nameof(polylineClasses));
			PolylineClasses = polylineClasses;
			ValidRelationConstraint = validRelationConstraint;
			AllowedEndpointInteriorIntersections = allowedEndpointInteriorIntersections;
			ReportOverlaps = reportOverlaps;
		}

		[TestParameter(_defaultAllowedInteriorIntersections)]
		[Doc(nameof(DocStrings.QaLineIntersect_AllowedInteriorIntersections))]
		public AllowedLineInteriorIntersections AllowedInteriorIntersections { get; set; }
	}
}
