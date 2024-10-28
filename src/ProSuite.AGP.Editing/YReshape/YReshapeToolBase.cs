using ProSuite.AGP.Editing.AdvancedReshape;
using ProSuite.AGP.Editing.Selection;
using ProSuite.Commons.AGP.Core.GeometryProcessing.AdvancedReshape;

namespace ProSuite.AGP.Editing.YReshape;

public class YReshapeToolBase : AdvancedReshapeToolBase
{
	protected override SelectionSettings GetSelectionSettings()
	{
		throw new System.NotImplementedException();
	}

	protected override IAdvancedReshapeService MicroserviceClient { get; }
}
