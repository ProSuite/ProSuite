using ProSuite.AGP.Editing.RemoveOverlaps;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Client.AGP;

namespace ProSuite.AGP.Solution.Editing
{
	[UsedImplicitly]
	public class RemoveOverlapsTool : RemoveOverlapsToolBase
	{
		protected override GeometryProcessingClient MicroserviceClient =>
			ProSuiteToolsModule.Current.ToolMicroserviceClient;
	}
}
