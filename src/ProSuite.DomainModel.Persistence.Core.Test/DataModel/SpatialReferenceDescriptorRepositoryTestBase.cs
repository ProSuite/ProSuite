using NUnit.Framework;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.Test.DataModel
{
	public abstract class SpatialReferenceDescriptorRepositoryTestBase
		: RepositoryTestBase<ISpatialReferenceDescriptorRepository>
	{
		protected abstract SpatialReferenceDescriptor CreateSpatialReferenceDescriptor();

		protected abstract DdxModel CreateModel();

		[Test]
		public void CanGetByName()
		{
			DdxModel m = CreateModel();
			SpatialReferenceDescriptor s = CreateSpatialReferenceDescriptor();
			m.SpatialReferenceDescriptor = s;

			CreateSchema(m, s);

			UnitOfWork.NewTransaction(
				delegate
				{
					Assert.IsFalse(UnitOfWork.HasChanges);

					SpatialReferenceDescriptor result =
						Repository.Get(m.SpatialReferenceDescriptor.Name);

					Assert.IsNotNull(result);
					Assert.AreEqual(s.Name, result.Name);
				});
		}
	}
}
