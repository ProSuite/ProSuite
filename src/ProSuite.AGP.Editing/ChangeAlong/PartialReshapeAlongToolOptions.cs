using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.ChangeAlong;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public class PartialReshapeAlongToolOptions : PartialOptionsBase
	{
		#region Overridable Settings

		// Target Selection
		public OverridableSetting<TargetFeatureSelection> TargetFeatureSelection { get; set; }

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

		// Reshape line filter settings
		public OverridableSetting<bool> ExcludeLinesOutsideSource { get; set; }
		public OverridableSetting<double> ExcludeLinesTolerance { get; set; }
		public OverridableSetting<bool> ExcludeLinesDisplay { get; set; }
		public OverridableSetting<bool> ExcludeLinesShowOnlyRemove { get; set; }
		public OverridableSetting<bool> ExcludeLinesOverlaps { get; set; }

		// Z Value settings
		public OverridableSetting<ZValueSource> ZValueSource { get; set; }

		#endregion

		public override PartialOptionsBase Clone()
		{
			var result = new PartialReshapeAlongToolOptions
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

				             // Reshape line filter settings
				             ExcludeLinesOutsideSource = TryClone(ExcludeLinesOutsideSource),
				             ExcludeLinesTolerance = TryClone(ExcludeLinesTolerance),
				             ExcludeLinesDisplay = TryClone(ExcludeLinesDisplay),
				             ExcludeLinesShowOnlyRemove = TryClone(ExcludeLinesShowOnlyRemove),
				             ExcludeLinesOverlaps = TryClone(ExcludeLinesOverlaps),

				             // Z Value settings
				             ZValueSource = TryClone(ZValueSource)
			             };
			return result;
		}
	}
}
