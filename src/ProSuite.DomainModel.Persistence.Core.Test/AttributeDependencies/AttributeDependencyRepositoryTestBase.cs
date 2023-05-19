using System.Collections.Generic;
using NUnit.Framework;
using ProSuite.Commons.Db;
using ProSuite.DomainModel.Core.AttributeDependencies;
using ProSuite.DomainModel.Core.AttributeDependencies.Repositories;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Persistence.Core.Test.AttributeDependencies
{
	public abstract class AttributeDependencyRepositoryTestBase
		: RepositoryTestBase<IAttributeDependencyRepository>
	{
		protected abstract DdxModel CreateModel();

		protected abstract ObjectDataset CreateObjectDataset(string name);

		[Test]
		public void CanGetAttributeDependency()
		{
			DdxModel model = CreateModel();

			ObjectDataset dataset = model.AddDataset(CreateObjectDataset("Lines"));
			dataset.AddAttribute(new ObjectAttribute("sourceField", FieldType.Text));
			dataset.AddAttribute(new ObjectAttribute("targetField", FieldType.Text));

			var ad = new AttributeDependency(dataset);
			ad.SourceAttributes.Add(dataset.GetAttribute("sourceField"));
			ad.TargetAttributes.Add(dataset.GetAttribute("targetField"));

			var mapping = new AttributeValueMapping("SourceText", "TargetText", "Description");
			ad.AttributeValueMappings.Add(mapping);

			CreateSchema(model, ad);

			int id = ad.Id;

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();

					AttributeDependency result = Repository.Get(id);

					Assert.IsNotNull(result);

					Assert.AreEqual(true, result.CanReverse);
					Assert.AreEqual(dataset, result.Dataset);

					Assert.AreEqual(1, ad.SourceAttributes.Count);
					Assert.AreEqual(1, ad.TargetAttributes.Count);

					Assert.AreEqual(1, ad.AttributeValueMappings.Count);
				});
		}

		[Test]
		public void CanGetByModel()
		{
			DdxModel model = CreateModel();

			ObjectDataset dataset = model.AddDataset(CreateObjectDataset("Lines"));

			AttributeDependency ad = new AttributeDependency(dataset);

			CreateSchema(model, ad);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();

					IList<AttributeDependency> results = Repository.GetByModelId(model.Id);

					Assert.AreEqual(1, results.Count);

					AttributeDependency result = results[0];

					Assert.IsNotNull(result);

					Assert.AreEqual(true, result.CanReverse);
					Assert.AreEqual(dataset, result.Dataset);
				});
		}

		[Test]
		public void CanGetByDataset()
		{
			DdxModel model = CreateModel();

			ObjectDataset dataset = model.AddDataset(CreateObjectDataset("Lines"));

			AttributeDependency ad = new AttributeDependency(dataset);

			CreateSchema(model, ad);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();

					AttributeDependency result = Repository.Get(dataset);

					Assert.IsNotNull(result);

					Assert.AreEqual(true, result.CanReverse);
					Assert.AreEqual(dataset, result.Dataset);
				});
		}
	}
}
