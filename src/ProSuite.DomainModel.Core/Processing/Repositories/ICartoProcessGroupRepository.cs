using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.Commands;

namespace ProSuite.DomainModel.Core.Processing.Repositories
{
	public interface ICartoProcessGroupRepository : IRepository<CartoProcessGroup>
	{
		[CanBeNull]
		CartoProcessGroup Get([NotNull] string name);

		[NotNull]
		IList<CartoProcessGroup> Get([NotNull] CartoProcess cartoProcess);

		[CanBeNull]
		CartoProcessGroup Get([NotNull] ICommandDescriptor commandDescriptor);
	}
}
