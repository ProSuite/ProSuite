using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public class PartialChangeAlongToolOptions : PartialOptionsBase
	{
		#region Overridable Settings

		public OverridableSetting<bool> InsertVertices { get; set; }

		public OverridableSetting<bool> ExcludeCutLines { get; set; }


		#endregion

		public override PartialOptionsBase Clone()
		{
			var result = new PartialChangeAlongToolOptions
			             {
				             InsertVertices = TryClone(InsertVertices),
				             ExcludeCutLines = TryClone(ExcludeCutLines)
			             };
			return result;
		}
	}
}


