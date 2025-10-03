using System.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList;

public interface IWorkEnvironment
{
	Task<IWorkList> CreateWorkListAsync(string uniqueName);

	Task<IWorkList> CreateWorkListAsync(string uniqueName, string path);

	/// <summary>
	/// Loads the work list layer, containing the navigable items based on the plugin
	/// datasource, into the specified map view.
	/// </summary>
	/// <param name="mapView"></param>
	/// <param name="worklist"></param>
	/// <param name="workListDefinitionFilePath"></param>
	void LoadWorkListLayer([NotNull] MapView mapView,
	                       IWorkList worklist,
	                       string workListDefinitionFilePath);

	/// <summary>
	/// Loads associated layers of the work list layer into the specified map view.
	/// Typically, associated layers come with DB-status work lists, such as the layers of
	/// the issue feature classes.
	/// </summary>
	void LoadAssociatedLayers([NotNull] MapView mapView,
	                          [NotNull] IWorkList worklist);

	string GetDisplayName();

	bool WorkListFileExistsInProjectFolder(out string worklistFilePath);

	/// <summary>
	/// Indication whether this work environment supports loading the work list in the background
	/// when the map is opened.
	/// </summary>
	bool AllowBackgroundLoading { get; }
}
