using System.Collections.Generic;

namespace ProSuite.Commons.AGP.Core.Geodatabase.PluginDatasources.WireFrame
{
	public interface IWireFrameSourceLayers
	{
		IEnumerable<IWireFrameSourceLayer> Get(string forMapUri);
	}
}
