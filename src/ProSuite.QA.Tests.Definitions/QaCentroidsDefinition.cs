using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Container.PolygonGrower;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check if there is exactly one vertex 
	/// within each polygon. 
	/// Polygons are derived out of polylines.
	/// </summary>
	[UsedImplicitly]
	[TopologyTest]
	[PolygonNetworkTest]
	public class QaCentroidsDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> PolylineClasses { get; set; }
		public IList<IFeatureClassSchemaDef> PointClasses { get; set; }
		public string Constraint { get; }
		public IFeatureClassSchemaDef PolylineClass { get; }
		public IFeatureClassSchemaDef PointClass { get; }

		private List<IReadOnlyRow> _centroids;

		private string _constraint;
		private MultiTableView _constraintHelper;
		private RingGrower<DirectedRow> _grower;
		private List<LineList<DirectedRow>> _innerRings;
		private List<LineList<DirectedRow>> _outerRings;

		[Doc(nameof(DocStrings.QaCentroids_0))]
		public QaCentroidsDefinition(
				[Doc(nameof(DocStrings.QaCentroids_polylineClass))]
				IFeatureClassSchemaDef polylineClass,
				[Doc(nameof(DocStrings.QaCentroids_pointClass))]
				IFeatureClassSchemaDef pointClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polylineClass, pointClass, null) { }

		[Doc(nameof(DocStrings.QaCentroids_0))]
		public QaCentroidsDefinition(
			[Doc(nameof(DocStrings.QaCentroids_polylineClass))]
			IFeatureClassSchemaDef polylineClass,
			[Doc(nameof(DocStrings.QaCentroids_pointClass))]
			IFeatureClassSchemaDef pointClass,
			[Doc(nameof(DocStrings.QaCentroids_constraint))]
			string constraint = null)
			: base(polylineClass)
		{
			PolylineClass = polylineClass;
			PointClass = pointClass;
			Constraint = constraint;
			PolylineClasses = new List<IFeatureClassSchemaDef> { polylineClass };
			PointClasses = new List<IFeatureClassSchemaDef> { pointClass };
		}

		[Doc(nameof(DocStrings.QaCentroids_2))]
		public QaCentroidsDefinition(
				[Doc(nameof(DocStrings.QaCentroids_polylineClasses))]
				IList<IFeatureClassSchemaDef> polylineClasses,
				[Doc(nameof(DocStrings.QaCentroids_pointClasses))]
				IList<IFeatureClassSchemaDef> pointClasses)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polylineClasses, pointClasses, null) { }

		[Doc(nameof(DocStrings.QaCentroids_2))]
		public QaCentroidsDefinition(
			[Doc(nameof(DocStrings.QaCentroids_polylineClasses))]
			IList<IFeatureClassSchemaDef> polylineClasses,
			[Doc(nameof(DocStrings.QaCentroids_pointClasses))]
			IList<IFeatureClassSchemaDef> pointClasses,
			[Doc(nameof(DocStrings.QaCentroids_constraint))]
			string constraint = null)
			: base(polylineClasses.Union(pointClasses))
		{
			PolylineClasses = polylineClasses;
			PointClasses = pointClasses;
			Constraint = constraint;
		}
	}
}
