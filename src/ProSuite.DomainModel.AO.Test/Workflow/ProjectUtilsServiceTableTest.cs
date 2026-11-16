using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.Workflow;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.Test.Workflow
{
	/// <summary>
	/// Matching of feature service tables to the datasets of a project's model, on the server.
	/// The client does the same in ModelDatasetUtils (see ModelDatasetUtilsTest).
	/// </summary>
	[TestFixture]
	public class ProjectUtilsServiceTableTest
	{
		private const string _serviceUrl =
			"https://host/server/rest/services/Topo/Transport/FeatureServer";

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		// The model was harvested with qualified names, the service layers were published with
		// the unqualified table name.
		[Test]
		public void FindsQualifiedModelDatasetForUnqualifiedServiceTable()
		{
			var model = new TestModel(elementNamesAreQualified: true);
			Dataset roadSegment = model.AddDataset(new TestVectorDataset("topo.geodb.RoadSegment"));
			model.AddDataset(new TestVectorDataset("topo.geodb.RailwayLine"));

			List<WorkspaceDataset> found = GetWorkspaceDatasets(
				model, "L10RoadSegment", "L11RailwayLine");

			Assert.AreEqual(2, found.Count);
			Assert.AreSame(roadSegment, found.Single(wd => wd.Name == "L10RoadSegment").Dataset);
		}

		[Test]
		public void FindsUnqualifiedModelDatasetForUnqualifiedServiceTable()
		{
			var model = new TestModel(elementNamesAreQualified: false);
			Dataset roadSegment = model.AddDataset(new TestVectorDataset("RoadSegment"));

			List<WorkspaceDataset> found = GetWorkspaceDatasets(model, "L10RoadSegment");

			Assert.AreEqual(1, found.Count);
			Assert.AreSame(roadSegment, found[0].Dataset);
		}

		[Test]
		public void FindsQualifiedModelDatasetForQualifiedServiceTable()
		{
			var model = new TestModel(elementNamesAreQualified: true);
			Dataset roadSegment = model.AddDataset(new TestVectorDataset("topo.geodb.RoadSegment"));

			List<WorkspaceDataset> found =
				GetWorkspaceDatasets(model, "L10topo_geodb_RoadSegment");

			Assert.AreEqual(1, found.Count);
			Assert.AreSame(roadSegment, found[0].Dataset);
		}

		[TestCase(true)]
		[TestCase(false)]
		public void DoesNotMatchServiceTableThatIsNotInModel(bool elementNamesAreQualified)
		{
			var model = new TestModel(elementNamesAreQualified);
			model.AddDataset(new TestVectorDataset(
				                 elementNamesAreQualified
					                 ? "topo.geodb.RoadSegment"
					                 : "RoadSegment"));

			List<WorkspaceDataset> found = GetWorkspaceDatasets(
				model, "L12Unknown", "L13RoadSegmentOld", "L14topo_other_RoadSegment");

			Assert.IsEmpty(found, string.Join(", ", found.Select(wd => wd.Name)));
		}

		[NotNull]
		private static List<WorkspaceDataset> GetWorkspaceDatasets(
			[NotNull] TestModel model,
			[NotNull] params string[] featureClassNames)
		{
			var container = new GdbTableContainer();

			GdbWorkspace workspace = GdbWorkspace.CreateForFeatureService(_serviceUrl, container);

			var objectClassId = 1;
			foreach (string featureClassName in featureClassNames)
			{
				container.TryAdd(new GdbFeatureClass(objectClassId++, featureClassName,
				                                     esriGeometryType.esriGeometryPolyline,
				                                     workspace: workspace));
			}

			var project = new TestProject(model);

			return ProjectUtils.GetWorkspaceDatasets<TestProject, TestModel>(workspace, project)
			                   .ToList();
		}

		private class TestProject : Project<TestModel>
		{
			public TestProject([NotNull] TestModel model) : base("Test")
			{
				ProductionModel = model;
			}
		}

		private class TestModel : ProductionModel, IModelMasterDatabase
		{
			// Without a master database workspace context, as on a server whose master database
			// is a different database than the feature service.
			public TestModel(bool elementNamesAreQualified) : base("Topo_Transport")
			{
				ElementNamesAreQualified = elementNamesAreQualified;
				DefaultDatabaseName = "topo";
				DefaultDatabaseSchemaOwner = "geodb";
			}

			public override string QualifyModelElementName(string modelElementName)
			{
				return ModelUtils.QualifyModelElementName(this, modelElementName);
			}

			public override string TranslateToModelElementName(string masterDatabaseDatasetName)
			{
				return ModelUtils.TranslateToModelElementName(this, masterDatabaseDatasetName);
			}

			IWorkspaceContext IModelMasterDatabase.CreateMasterDatabaseWorkspaceContext()
			{
				return null;
			}
		}

		private class TestVectorDataset : VectorDataset
		{
			public TestVectorDataset([NotNull] string name) : base(name)
			{
				GeometryType = new GeometryTypeShape("Polyline", ProSuiteGeometryType.Polyline);
			}
		}
	}
}
