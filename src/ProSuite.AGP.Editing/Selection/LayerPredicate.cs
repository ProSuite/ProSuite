using ArcGIS.Desktop.Mapping;

namespace ProSuite.AGP.Editing.Selection
{
	/// <summary>
	/// Definition of the layer filtering delegate
	/// function.
	/// </summary>
	/// <param name="layer">The layer to check</param>
	/// <returns>TRUE if the layer should be included,
	/// FALSE otherwise</returns>
	public delegate bool LayerPredicate(Layer layer);
}
