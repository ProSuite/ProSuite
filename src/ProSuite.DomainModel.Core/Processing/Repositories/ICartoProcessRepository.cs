using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.Processing.Repositories
{
	public interface ICartoProcessRepository : IRepository<CartoProcess>
	{
		[NotNull]
		IList<CartoProcess> GetAll([NotNull] CartoProcessType cartoProcessType);

		[NotNull]
		IList<CartoProcess> GetAll([NotNull] DdxModel model);
	}
}
