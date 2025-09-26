using ProSuite.AGP.Editing.AdvancedReshape;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.YReshape
{
	public class PartialYReshapeToolOptions : PartialOptionsBase
	{
		#region Overridable Settings

		public OverridableSetting<bool> ShowPreview { get; set; }

		public OverridableSetting<bool> MoveOpenJawEndJunction { get; set; } 

		public OverridableSetting<bool> RemainInSketchMode { get; set; }

		public OverridableSetting<bool> UseTopologyTypeSelection { get; set; }

		#endregion

		public override PartialOptionsBase Clone()
		{
			var result = new PartialAdvancedReshapeOptions
			             {
				MoveOpenJawEndJunction = new OverridableSetting<bool>(true, false), // YReshape: toggle with M
				ShowPreview = TryClone(ShowPreview),
				RemainInSketchMode = TryClone(RemainInSketchMode),
				UseTopologyTypeSelection = TryClone(UseTopologyTypeSelection)
			};
			return result;
		}
	}
}
