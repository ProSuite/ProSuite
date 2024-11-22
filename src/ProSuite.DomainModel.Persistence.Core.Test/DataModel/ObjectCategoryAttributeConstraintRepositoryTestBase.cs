using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.GeoDb;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.Test.DataModel
{
	public abstract class ObjectCategoryAttributeConstraintRepositoryTestBase
		: RepositoryTestBase<IObjectCategoryAttributeConstraintRepository>
	{
		protected abstract DdxModel CreateModel(string name);

		protected abstract VectorDataset CreateVectorDataset(string name);

		[Test]
		public void CanReadObjectCategoryAttributeConstraints()
		{
			DdxModel model = SetupSchema(out ObjectAttribute objectAttribute,
			                             out ObjectType objectType);

			VectorDataset datasetWithConstraints = (VectorDataset) model.Datasets.First(
				ds => ds is ObjectDataset ods && ods.Attributes.Count > 0 &&
				      ods.ObjectTypes.Count > 0);

			UnitOfWork.NewTransaction(
				delegate
				{
					Assert.IsFalse(UnitOfWork.HasChanges);

					UnitOfWork.Reattach(objectType);
					UnitOfWork.Reattach(objectAttribute);

					IList<ObjectCategoryAttributeConstraint> allConstraints =
						Repository.Get(datasetWithConstraints);

					Assert.IsNotNull(allConstraints);

					Assert.AreEqual(2, allConstraints.Count);

					IList<ObjectCategoryNonApplicableAttribute> nonApplicableConstraints =
						Repository
							.Get<ObjectCategoryNonApplicableAttribute>(datasetWithConstraints);

					Assert.AreEqual(1, nonApplicableConstraints.Count);

					ObjectCategoryNonApplicableAttribute nonApplicableConstraint =
						nonApplicableConstraints[0];
					Assert.AreEqual(nonApplicableConstraint.ObjectCategory, objectType);
					Assert.AreEqual(nonApplicableConstraint.ObjectAttribute, objectAttribute);
				});
		}

		[Test]
		public void CanReadObjectCategoryAttributeConstraintsByModel()
		{
			DdxModel model = SetupSchema(out ObjectAttribute objectAttribute,
			                             out ObjectType objectType);

			VectorDataset ds = (VectorDataset) model.Datasets.First();

			UnitOfWork.NewTransaction(
				delegate
				{
					Assert.IsFalse(UnitOfWork.HasChanges);

					UnitOfWork.Reattach(objectType);
					UnitOfWork.Reattach(objectAttribute);

					IList<ObjectCategoryAttributeConstraint> constraintsForModel =
						Repository.Get<ObjectCategoryAttributeConstraint>(model);

					Assert.IsNotNull(constraintsForModel);

					Assert.AreEqual(2, constraintsForModel.Count);

					IList<ObjectCategoryNonApplicableAttribute> nonApplicableConstraints =
						Repository.Get<ObjectCategoryNonApplicableAttribute>(model);

					Assert.AreEqual(1, nonApplicableConstraints.Count);

					ObjectCategoryNonApplicableAttribute nonApplicableConstraint =
						nonApplicableConstraints[0];
					Assert.AreEqual(nonApplicableConstraint.ObjectCategory, objectType);
					Assert.AreEqual(nonApplicableConstraint.ObjectAttribute, objectAttribute);

					Assert.AreEqual(model, nonApplicableConstraint.ObjectAttribute.Model);
				});
		}

		private DdxModel SetupSchema(out ObjectAttribute oa1, out ObjectType objectType)
		{
			DdxModel m = CreateModel("model");

			// Make sure the dataset with the constraints does not have the same id as the model, otherwise the test incorrectly succeeds!
			m.AddDataset(CreateVectorDataset("ds0"));
			VectorDataset ds = m.AddDataset(CreateVectorDataset("ds1"));

			oa1 = ds.AddAttribute(
				new ObjectAttribute("field1", FieldType.Text));
			ObjectAttribute oa2 = ds.AddAttribute(
				new ObjectAttribute("field2", FieldType.Text));

			objectType = ds.AddObjectType(1, "ObjCat1");
			ObjectType objCat2 = ds.AddObjectType(2, "ObjCat2");

			CreateSchema(m);

			var nonApplicable = new ObjectCategoryNonApplicableAttribute(objectType, oa1);

			var condition = new ObjectCategoryAttributeCondition(objCat2, oa2, "1=1");

			ObjectType objCat1 = objectType;

			UnitOfWork.NewTransaction(
				delegate
				{
					UnitOfWork.Reattach(objCat1);
					UnitOfWork.Reattach(objCat2);

					Repository.Save(nonApplicable);
					Repository.Save(condition);
				});

			return m;
		}
	}
}
