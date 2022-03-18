using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.DataModel
{
	public interface IMasterDatabaseWorkspaceContextFactory
	{
		IWorkspaceContext Create([NotNull] Model model);
	}
}
