using System.Collections.Generic;
using NUnit.Framework;
using ProSuite.Commons.GeoDb;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.Test.DataModel
{
	public abstract class AssociationRepositoryTestBase :
		RepositoryTestBase<IAssociationRepository>
	{
		protected abstract DdxModel CreateModel();

		protected abstract VectorDataset CreateVectorDataset(string name);

		[Test]
		public void CanGetByName()
		{
			const string dsName1 = "ds1";
			const string dsName2 = "ds2";
			DdxModel m = CreateModel();

			m.AddDataset(CreateVectorDataset("any1"));
			m.AddDataset(CreateVectorDataset("any2"));
			VectorDataset ds1 = m.AddDataset(CreateVectorDataset(dsName1));
			VectorDataset ds2 = m.AddDataset(CreateVectorDataset(dsName2));

			var uuidType = new ObjectAttributeType("uuid", AttributeRole.UUID);
			ObjectAttribute pk1 =
				ds1.AddAttribute(new ObjectAttribute("pk1", FieldType.Text, uuidType));
			ObjectAttribute pk2 =
				ds2.AddAttribute(new ObjectAttribute("pk2", FieldType.Text, uuidType));

			const string asoName = "aso1";

			const string fk1Name = "fk1";
			const string fk2Name = "fk2";

			m.AddAssociation(
				new AttributedAssociation(
					asoName, AssociationCardinality.ManyToMany,
					fk1Name,
					FieldType.Text, pk1,
					fk2Name,
					FieldType.Text, pk2));

			CreateSchema(uuidType, m);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();

					IList<Association> list = Repository.Get(asoName);
					Assert.AreEqual(1, list.Count);
					Association result = list[0] as AttributedAssociation;

					Assert.IsNotNull(result);
					Assert.AreEqual(asoName, result.Name);
					Assert.AreEqual(fk1Name, result.End1.ForeignKey.Name);
					Assert.AreEqual(fk2Name, result.End2.ForeignKey.Name);
					Assert.AreEqual(dsName1, result.End1.ObjectDataset.Name);
					Assert.AreEqual(dsName2, result.End2.ObjectDataset.Name);
				});
		}

		[Test]
		public void CanGetByUnknownName()
		{
			DdxModel m = CreateModel();

			CreateSchema(m);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();

					IList<Association> result = Repository.Get("UKNOWN.ASSOCIATION.NAME");

					Assert.AreEqual(0, result.Count);
				});
		}

		[Test]
		public void CanGetByReferencedDatasetId()
		{
			const string dsName1 = "ds1";
			const string dsName2 = "ds2";
			DdxModel m = CreateModel();

			m.AddDataset(CreateVectorDataset("any1"));
			m.AddDataset(CreateVectorDataset("any2"));
			VectorDataset ds1 = m.AddDataset(CreateVectorDataset(dsName1));
			VectorDataset ds2 = m.AddDataset(CreateVectorDataset(dsName2));

			var uuidType = new ObjectAttributeType("uuid", AttributeRole.UUID);
			ObjectAttribute pk1 =
				ds1.AddAttribute(new ObjectAttribute("pk1", FieldType.Text, uuidType));
			ObjectAttribute pk2 =
				ds2.AddAttribute(new ObjectAttribute("pk2", FieldType.Text, uuidType));

			const string asoName = "aso1";

			const string fk1Name = "fk1";
			const string fk2Name = "fk2";

			Association association = m.AddAssociation(
				new AttributedAssociation(
					asoName, AssociationCardinality.ManyToMany,
					fk1Name,
					FieldType.Text, pk1,
					fk2Name,
					FieldType.Text, pk2));

			CreateSchema(uuidType, m);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();

					IList<Association> list =
						Repository.GetByReferencedDatasetIds(new List<int> { ds1.Id });
					Expect(association, list);

					list = Repository.GetByReferencedDatasetIds(new List<int> { ds2.Id });
					Expect(association, list);

					list = Repository.GetByReferencedDatasetIds(new List<int> { ds1.Id, ds2.Id });
					Expect(association, list);
				});
		}

		private static void Expect(Association association, IList<Association> inList)
		{
			Assert.AreEqual(1, inList.Count);
			Association result = inList[0] as AttributedAssociation;

			Assert.IsNotNull(result);
			Assert.AreEqual(association.Name, result.Name);
			Assert.AreEqual(association.End1.ForeignKey.Name, result.End1.ForeignKey.Name);
			Assert.AreEqual(association.End2.ForeignKey.Name, result.End2.ForeignKey.Name);
			Assert.AreEqual(association.End1.ObjectDataset.Name, result.End1.ObjectDataset.Name);
			Assert.AreEqual(association.End2.ObjectDataset.Name, result.End2.ObjectDataset.Name);
		}
	}
}
