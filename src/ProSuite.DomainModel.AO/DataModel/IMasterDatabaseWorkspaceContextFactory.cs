using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.DataModel
{
	public interface IMasterDatabaseWorkspaceContextFactory
	{
		[NotNull]
		IWorkspaceContext Create([NotNull] Model model);
	}
}
