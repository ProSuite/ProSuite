using System;
using ProSuite.AGP.Editing.AdvancedReshape;
using ProSuite.AGP.Editing.Properties;
using ProSuite.AGP.Editing.Selection;
using ProSuite.Commons.AGP.Core.GeometryProcessing.AdvancedReshape;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.YReshape
{
	public class YReshapeToolBase : AdvancedReshapeToolBase
	{
		//protected string OptionsFileName => "YReshapeToolOptions.xml";
		// TODO: adapt Options from AdvancedReshape as soon as implemented

		[CanBeNull] private YReshapeFeedback _feedback;
		[CanBeNull] private SymbolizedSketchTypeBasedOnSelection _symbolizedSketch;

		protected YReshapeToolBase() {
			FireSketchEvents = true;

			// This is our property:
			RequiresSelection = true;

			SelectionCursor = ToolUtils.GetCursor(Resources.YReshapeToolCursor);
			SelectionCursorShift = ToolUtils.GetCursor(Resources.YReshapeToolCursorShift);

		}

		protected override SelectionSettings GetSelectionSettings()
		{
			throw new NotImplementedException();
		}

		protected override IAdvancedReshapeService MicroserviceClient { get; }
	}
}
