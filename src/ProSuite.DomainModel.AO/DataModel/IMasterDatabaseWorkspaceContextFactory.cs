using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public interface IMasterDatabaseWorkspaceContextFactory
	{
		[NotNull]
		IWorkspaceContext Create([NotNull] DdxModel model);
	}
}
