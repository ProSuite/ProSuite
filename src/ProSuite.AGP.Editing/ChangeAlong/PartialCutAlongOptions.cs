using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.ChangeAlong;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.ChangeAlong;

public class PartialCutAlongOptions : PartialOptionsBase
{
	#region Overridable Settings

	// Target Selection
	public OverridableSetting<TargetFeatureSelection> TargetFeatureSelection { get; set; }

	// Basic settings
	public OverridableSetting<bool> InsertVerticesInTargets { get; set; }

	// Display Performance Options
	public OverridableSetting<bool> DisplayLinesInVisibleExtentOnly { get; set; }
	public OverridableSetting<bool> RecalculateLinesOnExtentChange { get; set; }
	public OverridableSetting<bool> HideLinesBeyondMaxScale { get; set; }
	public OverridableSetting<double> HideLinesMaxScaleDenominator { get; set; }

	// Custom Tolerance settings
	public OverridableSetting<bool> UseCustomTolerance { get; set; }
	public OverridableSetting<double> CustomTolerance { get; set; }

	// Buffer settings
	public OverridableSetting<bool> BufferTarget { get; set; }
	public OverridableSetting<double> TargetBufferDistance { get; set; }
	public OverridableSetting<bool> EnforceMinimumBufferSegmentLength { get; set; }
	public OverridableSetting<double> MinBufferSegmentLength { get; set; }

	// Z Value settings
	public OverridableSetting<ZValueSource> ZValueSource { get; set; }

	#endregion

	public override PartialOptionsBase Clone()
	{
		var result = new PartialCutAlongOptions
		             {
			             TargetFeatureSelection = TryClone(TargetFeatureSelection),
			             InsertVerticesInTargets = TryClone(InsertVerticesInTargets),

			             // Display Performance Options
			             DisplayLinesInVisibleExtentOnly =
				             TryClone(DisplayLinesInVisibleExtentOnly),
			             RecalculateLinesOnExtentChange = TryClone(RecalculateLinesOnExtentChange),
			             HideLinesBeyondMaxScale = TryClone(HideLinesBeyondMaxScale),
			             HideLinesMaxScaleDenominator = TryClone(HideLinesMaxScaleDenominator),

			             // Minimal Tolerance settings
			             UseCustomTolerance = TryClone(UseCustomTolerance),
			             CustomTolerance = TryClone(CustomTolerance),

			             // Buffer settings
			             BufferTarget = TryClone(BufferTarget),
			             TargetBufferDistance = TryClone(TargetBufferDistance),
			             EnforceMinimumBufferSegmentLength =
				             TryClone(EnforceMinimumBufferSegmentLength),
			             MinBufferSegmentLength = TryClone(MinBufferSegmentLength),

			             // Z Value settings
			             ZValueSource = TryClone(ZValueSource)
		             };
		return result;
	}
}
