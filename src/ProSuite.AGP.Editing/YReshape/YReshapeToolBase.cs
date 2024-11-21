using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ProSuite.AGP.Editing.AdvancedReshape;
using ProSuite.AGP.Editing.Cracker;
using ProSuite.AGP.Editing.Properties;
using ProSuite.AGP.Editing.Selection;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Core.GeometryProcessing.AdvancedReshape;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.YReshape
{
	public abstract class YReshapeToolBase : AdvancedReshapeToolBase
	{
		//protected string OptionsFileName => "YReshapeToolOptions.xml";
		// TODO: adapt Options from AdvancedReshape as soon as implemented

		[CanBeNull] private YReshapeFeedback _feedback;
		[CanBeNull] private SymbolizedSketchTypeBasedOnSelection _symbolizedSketch;

		protected YReshapeToolBase() {
			FireSketchEvents = true;

			RequiresSelection = true;
		}

		protected new static string OptionsFileName => "YReshapeToolOptions.xml";
		

		protected override string LocalConfigDir =>
			EnvironmentUtils.ConfigurationDirectoryProvider.GetDirectory(AppDataFolder.Roaming);

		protected override void OnUpdateCore() {
			Enabled = MicroserviceClient != null;

			if (MicroserviceClient == null)
				DisabledTooltip = ToolUtils.GetDisabledReasonNoGeometryMicroservice();
		}

		protected override Task OnToolActivatingCoreAsync() {

			_feedback = new YReshapeFeedback();

			return base.OnToolActivatingCoreAsync();
		}

		//Make sure this is always true (settings in AdvancedReshape not implemented yet, so no effect atm)
		private bool allowOpenJawReshape = true;


		protected override Cursor GetSelectionCursor()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.YReshapeOverlay, null);
		}

		protected override Cursor GetSelectionCursorShift()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.YReshapeOverlay,
			                              Resources.Shift);
		}

		protected override Cursor GetSelectionCursorLasso()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.YReshapeOverlay,
			                              Resources.Lasso);
		}

		protected override Cursor GetSelectionCursorLassoShift()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.YReshapeOverlay,
			                              Resources.Lasso,
			                              Resources.Shift);
		}

		protected override Cursor GetSelectionCursorPolygon()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.YReshapeOverlay,
			                              Resources.Polygon);
		}

		protected override Cursor GetSelectionCursorPolygonShift()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.YReshapeOverlay,
			                              Resources.Polygon,
			                              Resources.Shift);
		}
	}


}

