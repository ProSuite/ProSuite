using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options;

namespace ProSuite.DomainServices.AO.Test.QA.Standalone.XmlBased
{
	[TestFixture]
	public class KeyFieldLookupTest
	{
		[Test]
		public void CanLookupKeyFields()
		{
			var xmlKeyFields =
				new XmlKeyFields
				{
					DefaultKeyField = "DEFAULT_ID",
					DataSourceKeyFields =
						new[]
						{
							new XmlDataSourceKeyFields
							{
								ModelName = "MODEL1",
								DefaultKeyField = "MODEL1_ID",
								DatasetKeyFields =
									new[]
									{
										new XmlDatasetKeyField
										{
											DatasetName = "DATASET_1_1",
											KeyField = "MODEL1_DATASET1_ID"
										},
										new XmlDatasetKeyField
										{
											DatasetName = "DATASET_1_2"
										}
									}.ToList()
							},
							new XmlDataSourceKeyFields
							{
								ModelName = "MODEL2",
								DefaultKeyField = "MODEL2_ID",
								DatasetKeyFields =
									new[]
									{
										new XmlDatasetKeyField
										{
											DatasetName = "DATASET_2_1",
											KeyField = "MODEL2_DATASET1_ID"
										}
									}.ToList()
							}
						}.ToList()
				};

			var model1 = new TestModel("model1"); // case should not matter
			var model2 = new TestModel("model2");
			var model3 = new TestModel("model3");

			TestDataset dataset11 = model1.AddDataset(new TestDataset("dataset_1_1"));
			TestDataset dataset12 = model1.AddDataset(new TestDataset("dataset_1_2"));
			TestDataset dataset13 = model1.AddDataset(new TestDataset("dataset_1_3"));

			TestDataset dataset21 = model2.AddDataset(new TestDataset("dataset_2_1"));
			TestDataset dataset22 = model2.AddDataset(new TestDataset("dataset_2_2"));
			TestDataset dataset31 = model3.AddDataset(new TestDataset("dataset_3_1"));

			var lookup = new KeyFieldLookup(xmlKeyFields);

			// dataset configured with alternate key
			Assert.AreEqual("MODEL1_DATASET1_ID", lookup.GetKeyField(dataset11));

			// dataset configured with null (--> use OBJECTID)
			Assert.IsNull(lookup.GetKeyField(dataset12));

			// model configured, but not dataset --> model default
			Assert.AreEqual("MODEL1_ID", lookup.GetKeyField(dataset13));

			// dataset configured with alternate key
			Assert.AreEqual("MODEL2_DATASET1_ID", lookup.GetKeyField(dataset21));

			// model configured, but not dataset --> model default
			Assert.AreEqual("MODEL2_ID", lookup.GetKeyField(dataset22));

			// not configured --> global default
			Assert.AreEqual("DEFAULT_ID", lookup.GetKeyField(dataset31));
		}

		[Test]
		public void CanDetectNonUniqueModelNames()
		{
			var xmlKeyFields =
				new XmlKeyFields
				{
					DefaultKeyField = "DEFAULT_ID",
					DataSourceKeyFields =
						new[]
						{
							new XmlDataSourceKeyFields { ModelName = "MODEL1" },
							new XmlDataSourceKeyFields { ModelName = "MODEL2" },
							new XmlDataSourceKeyFields { ModelName = "MODEL1" }
						}.ToList()
				};

			var e = Assert.Throws<InvalidConfigurationException>(
				delegate { new KeyFieldLookup(xmlKeyFields); });
			Assert.AreEqual("Duplicate data source name: MODEL1", e.Message);
		}

		[Test]
		public void CanDetectUndefinedModelName()
		{
			var xmlKeyFields =
				new XmlKeyFields
				{
					DefaultKeyField = "DEFAULT_ID",
					DataSourceKeyFields = new[] { new XmlDataSourceKeyFields() }.ToList()
				};

			var e = Assert.Throws<InvalidConfigurationException>(
				delegate { new KeyFieldLookup(xmlKeyFields); });
			Assert.AreEqual("Data source name not defined", e.Message);
		}

		[Test]
		public void CanDetectNonUniqueDatasetNames()
		{
			var xmlKeyFields =
				new XmlKeyFields
				{
					DefaultKeyField = "DEFAULT_ID",
					DataSourceKeyFields =
						new[]
						{
							new XmlDataSourceKeyFields
							{
								ModelName = "MODEL1",
								DatasetKeyFields =
									new[]
									{
										new XmlDatasetKeyField
										{
											DatasetName = "DATASET_1_1"
										},
										new XmlDatasetKeyField
										{
											DatasetName = "DATASET_1_1"
										}
									}.ToList()
							}
						}.ToList()
				};

			var e = Assert.Throws<InvalidConfigurationException>(
				delegate { new KeyFieldLookup(xmlKeyFields); });

			Assert.AreEqual("Duplicate dataset name: DATASET_1_1", e.Message);
		}

		[Test]
		public void CanDetectUndefinedDatasetName()
		{
			var xmlKeyFields =
				new XmlKeyFields
				{
					DefaultKeyField = "DEFAULT_ID",
					DataSourceKeyFields =
						new[]
						{
							new XmlDataSourceKeyFields
							{
								ModelName = "MODEL1",
								DatasetKeyFields =
									new[]
									{
										new XmlDatasetKeyField()
									}.ToList()
							}
						}.ToList()
				};

			var e = Assert.Throws<InvalidConfigurationException>(
				delegate { new KeyFieldLookup(xmlKeyFields); });

			Assert.AreEqual("Dataset name not defined", e.Message);
		}

		private class TestModel : ProductionModel, IModelMasterDatabase
		{
			public TestModel(string name) : base(name) { }

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
		}

		private class TestDataset : VectorDataset
		{
			public TestDataset([NotNull] string name) : base(name) { }
		}
	}
}
