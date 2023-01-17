using System.Collections.Generic;
using NUnit.Framework;
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
			DdxModel m = CreateModel("model");
			VectorDataset ds = m.AddDataset(CreateVectorDataset("ds"));

			ObjectAttribute oa1 = ds.AddAttribute(
				new ObjectAttribute("field1", FieldType.Text));
			ObjectAttribute oa2 = ds.AddAttribute(
				new ObjectAttribute("field2", FieldType.Text));

			ObjectType objCat1 = ds.AddObjectType(1, "ObjCat1");
			ObjectType objCat2 = ds.AddObjectType(2, "ObjCat2");

			CreateSchema(m);

			var nonApplicable = new ObjectCategoryNonApplicableAttribute(objCat1, oa1);

			var condition = new ObjectCategoryAttributeCondition(objCat2, oa2, "1=1");

			UnitOfWork.NewTransaction(
				delegate
				{
					UnitOfWork.Reattach(objCat1);
					UnitOfWork.Reattach(objCat2);

					Repository.Save(nonApplicable);
					Repository.Save(condition);
				});

			UnitOfWork.NewTransaction(
				delegate
				{
					Assert.IsFalse(UnitOfWork.HasChanges);

					UnitOfWork.Reattach(objCat1);
					UnitOfWork.Reattach(oa1);

					IList<ObjectCategoryAttributeConstraint> allConstraints = Repository.Get(ds);

					Assert.IsNotNull(allConstraints);

					Assert.AreEqual(2, allConstraints.Count);

					IList<ObjectCategoryNonApplicableAttribute> nonApplicableConstraints =
						Repository.Get<ObjectCategoryNonApplicableAttribute>(ds);

					Assert.AreEqual(1, nonApplicableConstraints.Count);

					ObjectCategoryNonApplicableAttribute nonApplicableConstraint =
						nonApplicableConstraints[0];
					Assert.AreEqual(nonApplicableConstraint.ObjectCategory, objCat1);
					Assert.AreEqual(nonApplicableConstraint.ObjectAttribute, oa1);
				});
		}
	}
}
