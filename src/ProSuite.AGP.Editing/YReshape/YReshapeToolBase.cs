using System;
using System.Threading.Tasks;
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

			SelectionCursor = ToolUtils.GetCursor(Resources.YReshapeToolCursor);
			SelectionCursorShift = ToolUtils.GetCursor(Resources.YReshapeToolCursorShift);

		}

		protected string OptionsFileName => "YReshapeToolOptions.xml";

		[CanBeNull]
		protected virtual string CentralConfigDir => null;

		protected virtual string LocalConfigDir =>
			EnvironmentUtils.ConfigurationDirectoryProvider.GetDirectory(AppDataFolder.Roaming);

		protected override void OnUpdateCore() {
			Enabled = MicroserviceClient != null;

			if (MicroserviceClient == null)
				DisabledTooltip = ToolUtils.GetDisabledReasonNoGeometryMicroservice();
		}

		protected override void OnToolActivatingCore() {
			//InitializeOptions();
		   //TODO: implement Options in advanced reshape
			_feedback = new YReshapeFeedback();
		}

		//Make sure this is always true (settings in AdvancedReshape not implemented yet, so no effect atm)
		private bool allowOpenJawReshape = true;
	}


}

