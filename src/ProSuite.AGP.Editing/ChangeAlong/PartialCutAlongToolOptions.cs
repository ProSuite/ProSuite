using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public class PartialCutAlongToolOptions : PartialOptionsBase
	{
		#region Overridable Settings

		// Target Selection
		public OverridableSetting<TargetFeatureSelection> TargetFeatureSelection { get; set; }

		// Basic settings
		public OverridableSetting<bool> InsertVertices { get; set; }

		// Display Performance Options
		public OverridableSetting<bool> DisplayExcludeCutLines { get; set; }
		public OverridableSetting<bool> DisplayRecalculateCutLines { get; set; }
		public OverridableSetting<bool> DisplayHideCutLines { get; set; }
		public OverridableSetting<double> DisplayHideCutLinesScale { get; set; }

		// Minimal Tolerance settings
		public OverridableSetting<bool> MinimalToleranceApply { get; set; }
		public OverridableSetting<double> MinimalTolerance { get; set; }

		// Buffer settings
		public OverridableSetting<bool> BufferTarget { get; set; }
		public OverridableSetting<double> BufferTolerance { get; set; }
		public OverridableSetting<bool> EnforceMinimumBufferSegmentLength { get; set; }
		public OverridableSetting<double> MinBufferSegmentLength { get; set; }

		// Z Value settings
		public OverridableSetting<ZValueSource> ZValueSource { get; set; }

		#endregion

		public override PartialOptionsBase Clone()
		{
			var result = new PartialCutAlongToolOptions
			             {
				             TargetFeatureSelection = TryClone(TargetFeatureSelection),
				             InsertVertices = TryClone(InsertVertices),

				             // Display Performance Options
				             DisplayExcludeCutLines = TryClone(DisplayExcludeCutLines),
				             DisplayRecalculateCutLines = TryClone(DisplayRecalculateCutLines),
				             DisplayHideCutLines = TryClone(DisplayHideCutLines),
				             DisplayHideCutLinesScale = TryClone(DisplayHideCutLinesScale),

				             // Minimal Tolerance settings
				             MinimalToleranceApply = TryClone(MinimalToleranceApply),
				             MinimalTolerance = TryClone(MinimalTolerance),

				             // Buffer settings
				             BufferTarget = TryClone(BufferTarget),
				             BufferTolerance = TryClone(BufferTolerance),
				             EnforceMinimumBufferSegmentLength =
					             TryClone(EnforceMinimumBufferSegmentLength),
				             MinBufferSegmentLength = TryClone(MinBufferSegmentLength),

				             // Z Value settings
				             ZValueSource = TryClone(ZValueSource)
			             };
			return result;
		}
	}
}
