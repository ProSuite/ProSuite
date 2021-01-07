using NUnit.Framework;
using ProSuite.Commons.DomainModels;

namespace ProSuite.DomainModel.Persistence.Core.Test
{
	[TestFixture]
	public abstract class RepositoryTestBase<T>
	{
		private IRepositoryTestController _controller;

		[OneTimeSetUp]
		public virtual void TestFixtureSetUp()
		{
			_controller = GetController();
			_controller.CheckOutLicenses();
			_controller.Configure();
		}

		[OneTimeTearDown]
		public virtual void TestFixtureTearDown()
		{
			_controller.ReleaseLicenses();
		}

		protected abstract IRepositoryTestController GetController();

		protected abstract T Repository { get; }

		protected virtual K Resolve<K>() where K : class
		{
			return null;
		}

		protected IUnitOfWork UnitOfWork
		{
			get { return _controller.UnitOfWork; }
		}

		protected void CreateSchema(params Entity[] entities)
		{
			_controller.CreateSchema(entities);
		}

		protected void AssertUnitOfWorkHasNoChanges()
		{
			Assert.IsFalse(UnitOfWork.HasChanges);
		}
	}
}
