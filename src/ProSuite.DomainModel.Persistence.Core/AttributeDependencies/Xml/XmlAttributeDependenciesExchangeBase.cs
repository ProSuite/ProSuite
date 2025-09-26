using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.AttributeDependencies.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.AttributeDependencies.Xml
{
	public abstract class XmlAttributeDependenciesExchangeBase
	{
		protected XmlAttributeDependenciesExchangeBase(
			[NotNull] IAttributeDependencyRepository repository,
			[NotNull] IUnitOfWork unitOfWork)
		{
			Assert.ArgumentNotNull(repository, nameof(repository));
			Assert.ArgumentNotNull(unitOfWork, nameof(unitOfWork));

			Repository = repository;
			UnitOfWork = unitOfWork;
		}

		[NotNull]
		protected IAttributeDependencyRepository Repository { get; }

		[NotNull]
		protected IUnitOfWork UnitOfWork { get; }
	}
}
