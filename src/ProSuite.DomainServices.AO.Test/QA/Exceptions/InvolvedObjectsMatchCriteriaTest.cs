using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainServices.AO.QA.Exceptions;

namespace ProSuite.DomainServices.AO.Test.QA.Exceptions
{
	[TestFixture]
	public class InvolvedObjectsMatchCriteriaTest
	{
		[Test]
		public void CanIgnoreSpecificDataset()
		{
			var ignoredDatasets = new List<XmlInvolvedObjectsMatchCriterionIgnoredDatasets>
			                      {
				                      new XmlInvolvedObjectsMatchCriterionIgnoredDatasets
				                      {
					                      ModelName = "MODEL1",
					                      DatasetNames = new[] { "Dataset1", "Dataset2" }.ToList()
				                      }
			                      };

			var criteria = new InvolvedObjectsMatchCriteria(ignoredDatasets);

			var model1 = new DummyModel("model1");
			DummyTableDataset dataset11 = model1.AddDataset(new DummyTableDataset("dataset1"));
			DummyTableDataset dataset12 = model1.AddDataset(new DummyTableDataset("dataset2"));
			DummyTableDataset dataset13 = model1.AddDataset(new DummyTableDataset("dataset3"));

			var model2 = new DummyModel("model2");
			DummyTableDataset dataset21 = model2.AddDataset(new DummyTableDataset("dataset1"));

			Assert.True(criteria.IgnoreDataset(dataset11));
			Assert.True(criteria.IgnoreDataset(dataset12));
			Assert.False(criteria.IgnoreDataset(dataset13));
			Assert.False(criteria.IgnoreDataset(dataset21));
		}

		[Test]
		public void CanIgnoreAllDatasetsForDataSource()
		{
			var ignoredDatasets =
				new List<XmlInvolvedObjectsMatchCriterionIgnoredDatasets>
				{
					new XmlInvolvedObjectsMatchCriterionIgnoredDatasets
					{
						ModelName = "MODEL1"
					}
				};

			var criteria = new InvolvedObjectsMatchCriteria(ignoredDatasets);

			var model1 = new DummyModel("model1");
			DummyTableDataset dataset11 = model1.AddDataset(new DummyTableDataset("dataset1"));
			DummyTableDataset dataset12 = model1.AddDataset(new DummyTableDataset("dataset2"));
			DummyTableDataset dataset13 = model1.AddDataset(new DummyTableDataset("dataset3"));

			var model2 = new DummyModel("model2");
			DummyTableDataset dataset21 = model2.AddDataset(new DummyTableDataset("dataset1"));

			Assert.True(criteria.IgnoreDataset(dataset11));
			Assert.True(criteria.IgnoreDataset(dataset12));
			Assert.True(criteria.IgnoreDataset(dataset13));
			Assert.False(criteria.IgnoreDataset(dataset21));
		}

		private class DummyModel : DdxModel, IModelMasterDatabase
		{
			public DummyModel(string name) : base(name) { }

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
				return ModelUtils.CreateDefaultMasterDatabaseWorkspaceContext(this);
			}

			protected override void CheckAssignSpecialDatasetCore(Dataset dataset) { }
		}

		private class DummyTableDataset : TableDataset
		{
			public DummyTableDataset([NotNull] string name) : base(name) { }
		}
	}
}
