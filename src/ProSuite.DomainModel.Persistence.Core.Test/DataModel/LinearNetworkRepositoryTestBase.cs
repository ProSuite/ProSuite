using System.Collections.Generic;
using NUnit.Framework;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.Test.DataModel
{
	public abstract class LinearNetworkRepositoryTestBase
		: RepositoryTestBase<ILinearNetworkRepository>
	{
		protected abstract DdxModel CreateModel();

		protected abstract VectorDataset CreateVectorDataset(string name);

		[Test]
		public void CanGetById()
		{
			DdxModel model = CreateModel();

			VectorDataset lineDataset = model.AddDataset(CreateVectorDataset("Lines"));
			VectorDataset pointDataset = model.AddDataset(CreateVectorDataset("Points"));

			var lineNetworkDataset = new LinearNetworkDataset(lineDataset)
			                         { WhereClause = "1 = 1" };
			var pointNetworkDataset = new LinearNetworkDataset(pointDataset)
			                          { IsDefaultJunction = true };
			LinearNetwork linearNetwork =
				new LinearNetwork("TestNetwork", new[] { lineNetworkDataset, pointNetworkDataset })
				{
					EnforceFlowDirection = true,
					CustomTolerance = 0.1234
				};

			CreateSchema(model, linearNetwork);

			int id = linearNetwork.Id;

			UnitOfWork.NewTransaction(
				delegate
				{
					Assert.IsFalse(UnitOfWork.HasChanges);

					LinearNetwork result = Repository.Get(id);

					Assert.IsNotNull(result);

					Assert.AreEqual(true, result.EnforceFlowDirection);
					Assert.AreEqual(0.1234, result.CustomTolerance);
					Assert.AreEqual(pointNetworkDataset, result.DefaultJunctionDataset);
				});
		}

		[Test]
		public void CanGetByModel()
		{
			DdxModel model = CreateModel();

			VectorDataset lineDataset = model.AddDataset(CreateVectorDataset("Lines"));
			VectorDataset pointDataset = model.AddDataset(CreateVectorDataset("Points"));

			var lineNetworkDataset = new LinearNetworkDataset(lineDataset)
			                         { WhereClause = "1 = 1" };
			var pointNetworkDataset = new LinearNetworkDataset(pointDataset)
			                          { IsDefaultJunction = true };

			LinearNetwork linearNetwork =
				new LinearNetwork("TestNetwork", new[] { lineNetworkDataset, pointNetworkDataset });

			linearNetwork.EnforceFlowDirection = true;

			CreateSchema(model, linearNetwork);

			UnitOfWork.NewTransaction(
				delegate
				{
					Assert.IsFalse(UnitOfWork.HasChanges);

					IList<LinearNetwork> results = Repository.GetByModelId(model.Id);

					Assert.AreEqual(1, results.Count);

					LinearNetwork result = results[0];

					Assert.IsNotNull(result);

					Assert.AreEqual(true, result.EnforceFlowDirection);
					Assert.AreEqual(0, result.CustomTolerance);
					Assert.AreEqual(pointNetworkDataset, result.DefaultJunctionDataset);
				});
		}

		[Test]
		public void CanGetByDatasets()
		{
			DdxModel model = CreateModel();

			VectorDataset lineDataset = model.AddDataset(CreateVectorDataset("Lines"));
			VectorDataset pointDataset = model.AddDataset(CreateVectorDataset("Points"));

			var lineNetworkDataset = new LinearNetworkDataset(lineDataset)
			                         { WhereClause = "1 = 1" };
			var pointNetworkDataset = new LinearNetworkDataset(pointDataset)
			                          { IsDefaultJunction = true };

			LinearNetwork linearNetwork =
				new LinearNetwork("TestNetwork", new[] { lineNetworkDataset, pointNetworkDataset });

			linearNetwork.EnforceFlowDirection = true;

			CreateSchema(model, linearNetwork);

			UnitOfWork.NewTransaction(
				delegate
				{
					Assert.IsFalse(UnitOfWork.HasChanges);

					IList<LinearNetwork> results =
						Repository.GetByDatasets(new[] { (Dataset) lineDataset });

					Assert.AreEqual(1, results.Count);

					LinearNetwork result = results[0];

					Assert.IsNotNull(result);

					Assert.AreEqual(true, result.EnforceFlowDirection);
					Assert.AreEqual(0, result.CustomTolerance);
					Assert.AreEqual(pointNetworkDataset, result.DefaultJunctionDataset);
				});
		}
	}
}
