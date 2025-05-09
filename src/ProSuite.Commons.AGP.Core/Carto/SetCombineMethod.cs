namespace ProSuite.Commons.AGP.Core.Carto;

/// <summary>How to combine with an existing selection</summary>
/// <remarks>
/// Like Pro SDK's SelectionCombinationMethod, but have our
/// own type to avoid the dependency on ArcGIS.Desktop.Mapping.
/// </remarks>
public enum SetCombineMethod
{
	New, // create a new selection
	Add, // add to the current selection (OR)
	Remove, // remove from the current selection
	Xor, // unselect if selected, select if not (XOR)
	And // select from the current selection (AND)
}
