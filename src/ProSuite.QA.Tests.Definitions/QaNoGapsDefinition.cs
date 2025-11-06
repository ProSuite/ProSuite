using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaNoGapsDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> PolygonClasses { get; }
		public double SliverLimit { get; }
		public double MaxArea { get; }
		public double SubtileWidth { get; }
		public bool FindGapsBelowTolerance { get; }
		public IList<IFeatureClassSchemaDef> AreaOfInterestClasses { get; }


		[Doc(nameof(DocStrings.QaNoGaps_0))]
		public QaNoGapsDefinition(
				[Doc(nameof(DocStrings.QaNoGaps_polygonClass))] [NotNull]
				IFeatureClassSchemaDef polygonClass,
				[Doc(nameof(DocStrings.QaNoGaps_sliverLimit))]
				double sliverLimit,
				[Doc(nameof(DocStrings.QaNoGaps_maxArea))]
				double maxArea)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polygonClass, sliverLimit, maxArea, 0d, false) { }

		[Doc(nameof(DocStrings.QaNoGaps_1))]
		public QaNoGapsDefinition(
				[Doc(nameof(DocStrings.QaNoGaps_polygonClasses))] [NotNull]
				IList<IFeatureClassSchemaDef> polygonClasses,
				[Doc(nameof(DocStrings.QaNoGaps_sliverLimit))]
				double sliverLimit,
				[Doc(nameof(DocStrings.QaNoGaps_maxArea))]
				double maxArea)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polygonClasses, sliverLimit, maxArea, 0d, false) { }

		[Doc(nameof(DocStrings.QaNoGaps_2))]
		public QaNoGapsDefinition(
			[Doc(nameof(DocStrings.QaNoGaps_polygonClass))] [NotNull]
			IFeatureClassSchemaDef polygonClass,
			[Doc(nameof(DocStrings.QaNoGaps_sliverLimit))]
			double sliverLimit,
			[Doc(nameof(DocStrings.QaNoGaps_maxArea))]
			double maxArea,
			[Doc(nameof(DocStrings.QaNoGaps_subtileWidth))]
			double subtileWidth,
			[Doc(nameof(DocStrings.QaNoGaps_findGapsBelowTolerance))]
			bool findGapsBelowTolerance)
			: this(new List<IFeatureClassSchemaDef> { polygonClass }, sliverLimit, maxArea,
			       subtileWidth, 0,
			       findGapsBelowTolerance, new List<IFeatureClassSchemaDef>(0)) { }

		[Doc(nameof(DocStrings.QaNoGaps_3))]
		public QaNoGapsDefinition(
			[Doc(nameof(DocStrings.QaNoGaps_polygonClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> polygonClasses,
			[Doc(nameof(DocStrings.QaNoGaps_sliverLimit))]
			double sliverLimit,
			[Doc(nameof(DocStrings.QaNoGaps_maxArea))]
			double maxArea,
			[Doc(nameof(DocStrings.QaNoGaps_subtileWidth))]
			double subtileWidth,
			[Doc(nameof(DocStrings.QaNoGaps_findGapsBelowTolerance))]
			bool findGapsBelowTolerance)
			: this(polygonClasses, sliverLimit, maxArea,
			       subtileWidth, 0,
			       findGapsBelowTolerance, new List<IFeatureClassSchemaDef>(0)) { }

		[Doc(nameof(DocStrings.QaNoGaps_4))]
		public QaNoGapsDefinition(
			[Doc(nameof(DocStrings.QaNoGaps_polygonClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> polygonClasses,
			[Doc(nameof(DocStrings.QaNoGaps_sliverLimit))]
			double sliverLimit,
			[Doc(nameof(DocStrings.QaNoGaps_maxArea))]
			double maxArea,
			[Doc(nameof(DocStrings.QaNoGaps_areaOfInterestClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> areaOfInterestClasses)
			: this(polygonClasses, sliverLimit, maxArea, 0d, false, areaOfInterestClasses) { }

		[Doc(nameof(DocStrings.QaNoGaps_5))]
		public QaNoGapsDefinition(
			[Doc(nameof(DocStrings.QaNoGaps_polygonClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> polygonClasses,
			[Doc(nameof(DocStrings.QaNoGaps_sliverLimit))]
			double sliverLimit,
			[Doc(nameof(DocStrings.QaNoGaps_maxArea))]
			double maxArea,
			[Doc(nameof(DocStrings.QaNoGaps_subtileWidth))]
			double subtileWidth,
			[Doc(nameof(DocStrings.QaNoGaps_findGapsBelowTolerance))]
			bool findGapsBelowTolerance,
			[Doc(nameof(DocStrings.QaNoGaps_areaOfInterestClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> areaOfInterestClasses)
			: this(polygonClasses, sliverLimit, maxArea,
			       subtileWidth, 0,
			       findGapsBelowTolerance, areaOfInterestClasses) { }

		[Obsolete]
		public QaNoGapsDefinition([NotNull] IFeatureClassSchemaDef polygonClass,
		                          double sliverLimit,
		                          double maxArea,
		                          int tileSubdivisionCount)
			: this(
				new List<IFeatureClassSchemaDef> { polygonClass }, sliverLimit, maxArea,
				0, tileSubdivisionCount, false, new List<IFeatureClassSchemaDef>()) { }

		[Obsolete]
		public QaNoGapsDefinition([NotNull] IList<IFeatureClassSchemaDef> polygonClasses,
		                          double sliverLimit,
		                          double maxArea,
		                          int tileSubdivisionCount)
			: this(polygonClasses, sliverLimit, maxArea, 0,
			       tileSubdivisionCount, false, new List<IFeatureClassSchemaDef>()) { }

		private QaNoGapsDefinition([NotNull] IList<IFeatureClassSchemaDef> polygonClasses,
		                           double sliverLimit, double maxArea,
		                           double subtileWidth, int tileSubdivisionCount,
		                           bool findGapsBelowTolerance,
		                           [NotNull] IList<IFeatureClassSchemaDef> areaOfInterestClasses)
			: base(CastToTables(polygonClasses, areaOfInterestClasses))
		{
			Assert.ArgumentNotNull(polygonClasses, nameof(polygonClasses));

			PolygonClasses = polygonClasses;
			SliverLimit = sliverLimit;
			MaxArea = maxArea;
			SubtileWidth = subtileWidth;
			FindGapsBelowTolerance = findGapsBelowTolerance;
			AreaOfInterestClasses = areaOfInterestClasses;
		}
	}
}
