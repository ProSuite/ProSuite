using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.DataModel.Xml
{
	public abstract class XmlLinearNetworksExchangeBase
	{
		protected XmlLinearNetworksExchangeBase([NotNull] ILinearNetworkRepository repository,
		                                        [NotNull] IUnitOfWork unitOfWork)
		{
			Assert.ArgumentNotNull(repository, nameof(repository));
			Assert.ArgumentNotNull(unitOfWork, nameof(unitOfWork));

			Repository = repository;
			UnitOfWork = unitOfWork;
		}

		[NotNull]
		protected ILinearNetworkRepository Repository { get; }

		[NotNull]
		protected IUnitOfWork UnitOfWork { get; }
	}
}
