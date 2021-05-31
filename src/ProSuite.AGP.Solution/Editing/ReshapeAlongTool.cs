using ProSuite.AGP.Editing.ChangeAlong;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Client.AGP;

namespace ProSuite.AGP.Solution.Editing
{
	[UsedImplicitly]
	public class ReshapeAlongTool : ChangeGeometryAlongToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected override GeometryProcessingClient MicroserviceClient =>
			ProSuiteToolsModule.Current.ToolMicroserviceClient;

		protected override void LogUsingCurrentSelection()
		{
			_msg.Info("Using current selection");
		}

		protected override void LogPromptForSelection()
		{
			_msg.Info("Prompt for selection");
		}
	}
}
