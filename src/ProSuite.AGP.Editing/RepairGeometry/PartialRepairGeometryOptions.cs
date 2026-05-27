using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.RepairGeometry;

public class PartialRepairGeometryOptions : PartialOptionsBase
{
	[CanBeNull]
	public OverridableSetting<bool> EnforceMinimumSegmentLength { get; set; }

	[CanBeNull]
	public OverridableSetting<double> MinimumSegmentLength { get; set; }

	[CanBeNull]
	public OverridableSetting<bool> AllowLoops { get; set; }

	[CanBeNull]
	public OverridableSetting<bool> AllowLinearSelfIntersections { get; set; }

	[CanBeNull]
	public OverridableSetting<bool> AddCrackPointsBetweenParts { get; set; }

	[CanBeNull]
	public OverridableSetting<double> CrackPointTolerance { get; set; }

	[CanBeNull]
	public OverridableSetting<bool> Use2D { get; set; }

	#region Overrides of PartialOptionsBase

	public override PartialOptionsBase Clone()
	{
		var result = new PartialRepairGeometryOptions();

		result.EnforceMinimumSegmentLength = TryClone(EnforceMinimumSegmentLength);
		result.MinimumSegmentLength = TryClone(MinimumSegmentLength);
		result.AllowLoops = TryClone(AllowLoops);
		result.AllowLinearSelfIntersections = TryClone(AllowLinearSelfIntersections);
		result.AddCrackPointsBetweenParts = TryClone(AddCrackPointsBetweenParts);
		result.CrackPointTolerance = TryClone(CrackPointTolerance);
		result.Use2D = TryClone(Use2D);

		return result;
	}

	#endregion
}
