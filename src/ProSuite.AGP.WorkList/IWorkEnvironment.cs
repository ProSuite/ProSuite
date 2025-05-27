using System.Threading.Tasks;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList;

public interface IWorkEnvironment
{
	Task<IWorkList> CreateWorkListAsync(string uniqueName);

	Task<IWorkList> CreateWorkListAsync(string uniqueName, string path);

	/// <summary>
	/// Loads the work list layer, containing the navigable items based on the plugin
	/// datasource, into the map.
	/// </summary>
	/// <param name="worklist"></param>
	/// <param name="workListDefinitionFilePath"></param>
	void LoadWorkListLayer(IWorkList worklist,
	                       string workListDefinitionFilePath);

	/// <summary>
	/// Loads associated layers of the work list layer into the map, if there are any.
	/// Typically, associated layers come with DB-status work lists, such as the layers of
	/// the issue feature classes.
	/// </summary>
	void LoadAssociatedLayers(IWorkList worklist);

	string GetDisplayName();

	bool DefinitionFileExistsInProjectFolder(out string definitionFile);
}
