using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.Erase;

public class PartialEraseOptions : PartialOptionsBase
{
	#region Overridable Settings

	public OverridableSetting<bool> AllowPolylineErasing { get; set; }

	public OverridableSetting<bool> AllowMultipointErasing { get; set; }

	public OverridableSetting<bool> PreventMultipartResults { get; set; }

	#endregion

	public override PartialOptionsBase Clone()
	{
		var result = new PartialEraseOptions
		             {
			             AllowPolylineErasing = TryClone(AllowPolylineErasing),
			             AllowMultipointErasing = TryClone(AllowMultipointErasing),
			             PreventMultipartResults = TryClone(PreventMultipartResults)
		             };
		return result;
	}
}
