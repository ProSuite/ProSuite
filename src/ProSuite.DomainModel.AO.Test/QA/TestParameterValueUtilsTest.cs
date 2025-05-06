using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.Geodatabase;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.AO.Test.QA
{
	[TestFixture]
	public class TestParameterValueUtilsTest
	{
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

		[Test]
		public void CanSyncParameterValuesWithEmptyListParameter_TOP5941()
		{
			// Arrange
			VectorDataset dataset = GetVectorDataset();

			var classDesc = new ClassDescriptor(typeof(BaseTest));
			var testDesc = new TestDescriptor("BaseTest", classDesc, 2);

			var condition = new QualityCondition("BaseCondition", testDesc);
			TestParameterValueUtils.AddParameterValue(condition, "table", dataset);
			TestParameterValueUtils.AddParameterValue(condition, "Number", 2.71828);

			// Act
			bool result = TestParameterValueUtils.SyncParameterValues(condition);

			// Assert
			Assert.IsTrue(result);

			TestParameterValue listParameter =
				condition.ParameterValues.FirstOrDefault(p => p.TestParameterName == "intList");

			// TOP-5941: Never add a default scalar to the list! It should be empty.
			Assert.IsNull(listParameter);
		}

		[NotNull]
		private static VectorDataset GetVectorDataset()
		{
			const string tableName = "Strassen";
			var fcStrassen = new FeatureClassMock(tableName,
			                                      esriGeometryType.esriGeometryPolyline, 1);
			var workspaceMock = new WorkspaceMock();
			workspaceMock.AddDataset(fcStrassen);

			var model = new TestModel
			            {
				            UserConnectionProvider =
					            new OpenWorkspaceConnectionProvider((IWorkspace) workspaceMock),
				            UseDefaultDatabaseOnlyForSchema = false
			            };

			var dataset = new TestVectorDataset(tableName);
			model.AddDataset(dataset);
			return dataset;
		}

		private class TestVectorDataset : VectorDataset
		{
			public TestVectorDataset(string name) : base(name) { }
		}

		private class TestModel : ProductionModel
		{
			protected override IWorkspaceContext CreateMasterDatabaseWorkspaceContext()
			{
				return CreateDefaultMasterDatabaseWorkspaceContext();
			}
		}
	}
}
