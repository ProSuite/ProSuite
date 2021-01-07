using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.Test.DataModel
{
	public abstract class DatasetRepositoryTestBase :
		RepositoryTestBase<IDatasetRepository>
	{
		[Test]
		public void CanGetByName()
		{
			const string dsName1 = "TOPGIS.TLM_DATASET1";
			const string dsName2 = "TOPGIS.TLM_DATASET2";
			const string dsName3 = "TOPGIS.TLM_DATASET3";
			DdxModel m = CreateModel();

			m.AddDataset(CreateObjectDataset(dsName1));
			m.AddDataset(CreateObjectDataset(dsName2));
			m.AddDataset(CreateObjectDataset(dsName3));

			CreateSchema(m);

			UnitOfWork.NewTransaction(
				delegate
				{
					Assert.IsFalse(UnitOfWork.HasChanges);

					IList<Dataset> list = Repository.Get(dsName2);
					Assert.AreEqual(1, list.Count);

					var result = list[0] as VectorDataset;

					Assert.IsNotNull(result);
					Assert.AreEqual(dsName2, result.Name);
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

					IList<Dataset> list = Repository.Get("UKNOWN.DATASET.NAME");
					Assert.AreEqual(0, list.Count);
				});
		}

		[Test]
		public void CanGetAttributes()
		{
			const string dsName = "TOPGIS.TLM_DATASET";
			DdxModel m = CreateModel();

			ObjectDataset ds = m.AddDataset(CreateObjectDataset(dsName));

			ObjectAttribute oa1 = ds.AddAttribute(
				new ObjectAttribute("field1", FieldType.Text));
			ObjectAttribute oa2 = ds.AddAttribute(
				new ObjectAttribute("field2", FieldType.Text));

			oa1.ReadOnlyOverride = false;
			oa2.ReadOnlyOverride = true;

			CreateSchema(m);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();

					IList<Dataset> list = Repository.Get(dsName);
					Assert.AreEqual(1, list.Count);
					var result = list[0] as VectorDataset;

					Assert.IsNotNull(result);
					Assert.AreEqual(dsName, result.Name);
					Assert.AreEqual(2, ds.Attributes.Count);
					Assert.AreEqual(2, (new List<ObjectAttribute>(ds.GetAttributes())).Count);

					ObjectAttribute field1 = ds.GetAttribute("field1");
					ObjectAttribute field2 = ds.GetAttribute("field2");
					Assert.IsNotNull(field1, "field1");
					Assert.IsNotNull(field2, "field2");

					Assert.IsFalse(field1.ReadOnly);
					Assert.IsTrue(field2.ReadOnly);
				});
		}

		[Test]
		public void CanGetAssociationEnds()
		{
			const string associationName = "relClass";
			const string dsName1 = "ds1";
			const string dsName2 = "ds2";
			DdxModel m = CreateModel();

			ObjectDataset ds1 = m.AddDataset(CreateObjectDataset(dsName1));
			ObjectDataset ds2 = m.AddDataset(CreateObjectDataset(dsName2));

			ObjectAttribute fk = ds1.AddAttribute(
				new ObjectAttribute("fk", FieldType.Text));
			ObjectAttribute pk = ds2.AddAttribute(
				new ObjectAttribute("pk", FieldType.Text));

			m.AddAssociation(new ForeignKeyAssociation(associationName,
			                                           AssociationCardinality.OneToMany,
			                                           fk, pk));

			CreateSchema(m);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();

					IList<Dataset> list = Repository.Get(dsName1);
					Assert.AreEqual(1, list.Count);
					var result = list[0] as VectorDataset;

					Assert.IsNotNull(result);
					Assert.AreEqual(1, result.AssociationEnds.Count);
					Assert.AreEqual(
						1, (new List<AssociationEnd>(result.GetAssociationEnds())).Count);
					AssociationEnd associationEnd = result.AssociationEnds[0];

					Assert.AreEqual(ds1, associationEnd.ObjectDataset);
					Assert.IsNotNull(associationEnd.Association);
					Assert.AreEqual(associationName, associationEnd.Association.Name);
					Assert.AreEqual(ds2, result.AssociationEnds[0].OppositeEnd.ObjectDataset);
				});
		}

		protected abstract ObjectDataset CreateObjectDataset(string name);

		protected abstract DdxModel CreateModel();
	}
}
