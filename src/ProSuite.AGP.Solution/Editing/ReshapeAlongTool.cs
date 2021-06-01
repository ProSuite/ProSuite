using ProSuite.AGP.Editing.ChangeAlong;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Client.AGP;

namespace ProSuite.AGP.Solution.Editing
{
	[UsedImplicitly]
	public class ReshapeAlongTool : ReshapeAlongToolBase
	{
		protected override GeometryProcessingClient MicroserviceClient =>
			ProSuiteToolsModule.Current.ToolMicroserviceClient;
	}
}
