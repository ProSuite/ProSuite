using NUnit.Framework;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.Test.DataModel
{
	public abstract class AttributeTypeRepositoryTestBase :
		RepositoryTestBase<IAttributeTypeRepository>
	{
		[Test]
		public void CanSaveAndReadObjectAttributeType()
		{
			var type = new ObjectAttributeType("uuid")
			           {
				           AttributeRole = AttributeRole.UUID,
				           ReadOnly = true,
				           IsObjectDefining = true
			           };

			CreateSchema();

			UnitOfWork.NewTransaction(() => Repository.Save(type));

			UnitOfWork.NewTransaction(
				delegate
				{
					Assert.IsFalse(UnitOfWork.HasChanges);

					var reread = (ObjectAttributeType) Repository.GetAll()[0];

					Assert.IsNotNull(reread);
					Assert.AreEqual("uuid", reread.Name);
					Assert.AreEqual(AttributeRole.UUID, reread.AttributeRole);
					Assert.IsTrue(reread.ReadOnly);
					Assert.IsTrue(reread.IsObjectDefining);
				});
		}
	}
}
