using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.FillHole
{
	public class PartialHoleToolOptions : PartialOptionsBase
	{
		#region Overridable Settings

		public OverridableSetting<bool> ShowPreview { get; set; }

		public OverridableSetting<bool> LimitPreviewToExtent { get; set; }

		#endregion

		public override PartialOptionsBase Clone()
		{
			var result = new PartialHoleToolOptions()
			{
				ShowPreview = TryClone(ShowPreview),
				LimitPreviewToExtent = TryClone(LimitPreviewToExtent)
			};

			return result;
		}
	}
}
