using ProSuite.Commons.AGP.Core.Geodatabase.PluginDatasources.WireFrame;

namespace ProSuite.Commons.AGP.Core.Geodatabase.PluginDatasources
{
	/// <summary>
	/// Registry for interface implementations that provide the context for specific plugin data
	/// sources. For example, the IWireFrameClasses implementer is registered when the module is
	/// initialized and can subsequently be used by the <see cref="WireFrameTable"/>.
	/// Alternatively, an IoC implementation, such as <see cref="IoC.IoCContainer"/> could be used.
	/// </summary>
	public static class PluginContextRegistry
	{
		/// <summary>
		/// Returns the <see cref="IWireFrameSourceLayers"/> implementation that provides the visible
		/// feature classes of the current project.
		/// </summary>
		public static IWireFrameSourceLayers WireFrameSourceLayers { get; set; }

		// Other contexts, such as transformed feature classes, work list environments, etc.
		// could go here.
	}
}
