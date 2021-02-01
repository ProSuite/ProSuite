using ProSuite.AGP.Editing.AdvancedReshape;
using ProSuite.Microservices.Client.AGP;

namespace ProSuite.AGP.Solution.Editing
{
	public class AdvancedReshapeTool : AdvancedReshapeToolBase
	{
		protected override GeometryProcessingClient MicroserviceClient =>
			ProSuiteToolsModule.Current.ToolMicroserviceClient;
	}
}
