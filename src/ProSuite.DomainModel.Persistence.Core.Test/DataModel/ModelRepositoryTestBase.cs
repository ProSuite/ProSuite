using NUnit.Framework;
using ProSuite.Commons.GeoDb;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.Test.DataModel
{
	public abstract class ModelRepositoryTestBase : RepositoryTestBase<IModelRepository>
	{
		protected abstract DdxModel CreateModel(string name);

		protected abstract VectorDataset CreateVectorDataset(string name);

		[Test]
		public void CanReattach()
		{
			InitializeModel();

			Dataset ds1 = null;
			DdxModel model = null;
			UnitOfWork.NewTransaction(
				delegate
				{
					model = Repository.Get("model");
					Assert.IsNotNull(model);

					Assert.IsFalse(UnitOfWork.IsInitialized(model.Datasets));

					ds1 = model.GetDatasetByModelName("ds1");

					Assert.IsTrue(UnitOfWork.IsInitialized(model.Datasets));
				});

			UnitOfWork.NewTransaction(
				delegate
				{
					UnitOfWork.Reattach(model);

					model = Repository.Get("model");
					Assert.IsNotNull(model);

					Assert.IsTrue(UnitOfWork.IsInitialized(model.Datasets));

					// will be satisfied from the dictionary:
					ds1 = model.GetDatasetByModelName("ds1");
					Assert.IsNotNull(ds1);

					Assert.IsTrue(UnitOfWork.IsInitialized(model.Datasets));

					AreDatasetsConsistent(model);

					Assert.AreEqual(1, ((ObjectDataset) ds1).Attributes.Count);
				});
		}

		[Test]
		public void CanRefreshAfterReattach()
		{
			InitializeModel();

			Dataset ds1 = null;
			DdxModel model = null;
			UnitOfWork.NewTransaction(
				delegate
				{
					model = Repository.Get("model");
					Assert.IsNotNull(model);

					Assert.IsFalse(UnitOfWork.IsInitialized(model.Datasets));

					ds1 = model.GetDatasetByModelName("ds1");

					Assert.IsTrue(UnitOfWork.IsInitialized(model.Datasets));
				});

			UnitOfWork.NewTransaction(
				delegate
				{
					UnitOfWork.Reattach(model);

					Repository.Refresh(model);

					// after a refresh, persistent collections are again not initialized
					Assert.IsFalse(UnitOfWork.IsInitialized(model.Datasets));

					// will be satisfied from the dictionary:
					ds1 = model.GetDatasetByModelName("ds1");
					Assert.IsNotNull(ds1);

					// the persistent collection is still not initialized
					Assert.IsFalse(UnitOfWork.IsInitialized(model.Datasets));

					AreDatasetsConsistent(model);

					Assert.AreEqual(1, ((ObjectDataset) ds1).Attributes.Count);
				});
		}

		[Test]
		public void CanGetDatasetsWithDeletedDuplicate()
		{
			// Repro for DPS#185

			InitializeModel();

			Dataset ds1 = null;
			DdxModel model = null;

			VectorDataset duplicate = null;

			UnitOfWork.NewTransaction(
				delegate
				{
					model = Repository.Get("model");
					Assert.IsNotNull(model);

					Assert.IsFalse(UnitOfWork.IsInitialized(model.Datasets));

					ds1 = model.GetDatasetByModelName("ds1");
					Assert.NotNull(ds1);

					// Trick the system into a duplicate dataset name...
					duplicate = model.AddDataset(CreateVectorDataset("duplicate"));

					// ... by renaming the original dataset (It is not clear how this happens in the
					// real world).
					ds1.RegisterDeleted();
					ds1.Name = "duplicate";
				});

			UnitOfWork.NewTransaction(
				delegate
				{
					UnitOfWork.Reattach(model);

					Repository.Refresh(model);

					// after a refresh, persistent collections are again not initialized
					Assert.IsFalse(UnitOfWork.IsInitialized(model.Datasets));

					// will be satisfied from the dictionary:
					var duplicateDataset = model.GetDatasetByModelName("duplicate");
					Assert.IsNotNull(duplicateDataset);
					Assert.AreNotEqual(ds1.Id, duplicateDataset.Id);
					Assert.AreEqual(duplicate.Id, duplicateDataset.Id);
				});
		}

		private void InitializeModel()
		{
			const string dsName1 = "ds1";
			const string dsName2 = "ds2";
			DdxModel modelInit = CreateModel("model");

			VectorDataset ds1 = modelInit.AddDataset(CreateVectorDataset(dsName1));
			VectorDataset ds2 = modelInit.AddDataset(CreateVectorDataset(dsName2));

			var uuidType = new ObjectAttributeType("uuid", AttributeRole.UUID);
			ds1.AddAttribute(new ObjectAttribute("pk1", FieldType.Text, uuidType));
			ds2.AddAttribute(new ObjectAttribute("pk2", FieldType.Text, uuidType));

			UnitOfWork.Start();
			CreateSchema(uuidType, modelInit);
			UnitOfWork.Commit();
			UnitOfWork.Stop();
		}

		private static void AreDatasetsConsistent(DdxModel model)
		{
			foreach (Dataset fromCollection in model.Datasets)
			{
				Dataset fromCache = model.GetDatasetByModelName(fromCollection.Name);

				Assert.IsTrue(ReferenceEquals(fromCache, fromCollection));
			}
		}
	}
}
