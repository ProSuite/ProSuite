using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Db;
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
			const string dsName1 = "SCHEMA.DATASET1";
			const string dsName2 = "SCHEMA.DATASET2";
			const string dsName3 = "SCHEMA.DATASET3";
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
		public void CanGetErrorDatasets()
		{
			const string dsPoints = "SCHEMA.ERROR_POINTS";
			const string dsLines = "SCHEMA.ERROR_LINE";
			const string dsPolys = "SCHEMA.ERROR_POLYGONS";
			const string dsPatches = "SCHEMA.ERROR_MULTIPATCHES";
			const string dsRows = "SCHEMA.ERROR_ROWS";

			DdxModel m = CreateModel();

			m.AddDataset(new ErrorMultipointDataset(dsPoints));
			m.AddDataset(new ErrorLineDataset(dsLines));
			m.AddDataset(new ErrorPolygonDataset(dsPolys));
			m.AddDataset(new ErrorMultiPatchDataset(dsPatches));
			m.AddDataset(new ErrorTableDataset(dsRows));

			CreateSchema(m);

			UnitOfWork.NewTransaction(
				delegate
				{
					Assert.IsFalse(UnitOfWork.HasChanges);

					CheckErrorDataset<ErrorMultipointDataset>(dsPoints);
					CheckErrorDataset<ErrorLineDataset>(dsLines);
					CheckErrorDataset<ErrorPolygonDataset>(dsPolys);
					CheckErrorDataset<ErrorMultiPatchDataset>(dsPatches);
					CheckErrorDataset<ErrorTableDataset>(dsRows);
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
			const string dsName = "SCHEMA.DATASET";
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
					Assert.AreEqual(2, new List<ObjectAttribute>(ds.GetAttributes()).Count);

					ObjectAttribute field1 = result.GetAttribute("field1");
					ObjectAttribute field2 = result.GetAttribute("field2");
					Assert.IsNotNull(field1, "field1");
					Assert.IsNotNull(field2, "field2");

					Assert.IsFalse(field1.ReadOnly);
					Assert.IsTrue(field2.ReadOnly);

					Assert.IsTrue(field1.IsPersistent);
					Assert.IsTrue(field2.IsPersistent);
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

			Assert.AreEqual(1, ds1.AssociationEnds.Count);
			Assert.AreEqual(1, new List<AssociationEnd>(ds1.GetAssociationEnds()).Count);

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
					Assert.AreEqual(1, new List<AssociationEnd>(result.GetAssociationEnds()).Count);
					AssociationEnd associationEnd = result.AssociationEnds[0];

					Assert.AreEqual(ds1, associationEnd.ObjectDataset);
					Assert.IsNotNull(associationEnd.Association);
					Assert.AreEqual(associationName, associationEnd.Association.Name);
					Assert.AreEqual(ds2, result.AssociationEnds[0].OppositeEnd.ObjectDataset);
				});
		}

		protected abstract ObjectDataset CreateObjectDataset(string name);

		protected abstract DdxModel CreateModel();

		private void CheckErrorDataset<T>(string datasetName) where T : Dataset, IErrorDataset
		{
			IList<Dataset> list = Repository.Get(datasetName);
			Assert.AreEqual(1, list.Count);

			var result = list[0] as T;

			Assert.IsNotNull(result);
			Assert.AreEqual(datasetName, result.Name);

			var resultErrorDataset =
				Repository.Get<T>(result.Model).Single();

			Assert.NotNull(resultErrorDataset);
			Assert.AreEqual(datasetName, result.Name);
			Assert.AreEqual(result, resultErrorDataset);
		}
	}
}
