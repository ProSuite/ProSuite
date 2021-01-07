using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Persistence.Core.Test
{
	public interface IRepositoryTestController
	{
		void Configure();

		void CheckOutLicenses();

		void CreateSchema(params Entity[] entities);

		[NotNull]
		IUnitOfWork UnitOfWork { get; }

		void ReleaseLicenses();
	}
}
