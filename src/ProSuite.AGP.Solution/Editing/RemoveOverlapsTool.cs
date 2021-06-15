using ArcGIS.Desktop.Framework;
using ProSuite.AGP.Editing.RemoveOverlaps;
using ProSuite.AGP.Solution.EditingOptionsUI;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Client.AGP;

namespace ProSuite.AGP.Solution.Editing
{
	[UsedImplicitly]
	public class RemoveOverlapsTool : RemoveOverlapsToolBase
	{
		protected override GeometryProcessingClient MicroserviceClient =>
			ProSuiteToolsModule.Current.ToolMicroserviceClient;

		protected override string ViewModelXamlId =>
			RemoveOverlapsOptionsViewModel.GetId();

		protected override void ShowOptionsPane()
		{
			var view = RemoveOverlapsOptionsViewModel.GetInstance();
				
			view.Options = RemoveOverlapsOptions;

			view.Activate();
		}
	}
}
