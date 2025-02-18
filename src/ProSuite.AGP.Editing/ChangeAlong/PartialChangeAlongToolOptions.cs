using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.AGP.Core.GeometryProcessing;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public class PartialChangeAlongToolOptions : PartialOptionsBase
	{
		#region Overridable Settings

		// Target Selection
		public OverridableSetting<TargetFeatureSelection> TargetFeatureSelection { get; set; }

		public OverridableSetting<bool> InsertVertices { get; set; }
		public OverridableSetting<bool> ExcludeCutLines { get; set; }
		
		// Display Performance Options
		public OverridableSetting<bool> DisplayExcludeCutLines { get; set; }
		public OverridableSetting<bool> DisplayRecalculateCutLines { get; set; }
		public OverridableSetting<bool> DisplayHideCutLines { get; set; }
		public OverridableSetting<double> DisplayHideCutLinesScale { get; set; }

		// Adjust settings
		public OverridableSetting<bool> Adjust { get; set; }
		public OverridableSetting<double> AdjustTolerance { get; set; }
		public OverridableSetting<bool> AdjustExcludeCurves { get; set; }
		public OverridableSetting<bool> AdjustShowTolerance { get; set; }

		// Buffer settings
		public OverridableSetting<bool> BufferTarget { get; set; }
		public OverridableSetting<double> BufferTolerance { get; set; }
		public OverridableSetting<bool> EnforceMinimumBufferSegmentLength { get; set; }
		public OverridableSetting<double> MinBufferSegmentLength { get; set; }

		// Reshape line filter settings
		public OverridableSetting<bool> ExcludeLines { get; set; }
		public OverridableSetting<double> ExcludeLinesTolerance { get; set; }
		public OverridableSetting<bool> ExcludeLinesDisplay { get; set; }
		public OverridableSetting<bool> ExcludeLinesShowOnlyRemove { get; set; }
		public OverridableSetting<bool> ExcludeLinesOverlaps { get; set; }

		// Z Value settings
		public OverridableSetting<ZValueSource> ZValueSource { get; set; }

		#endregion

		public override PartialOptionsBase Clone()
		{
			var result = new PartialChangeAlongToolOptions
			{
				TargetFeatureSelection = TryClone(TargetFeatureSelection),
				InsertVertices = TryClone(InsertVertices),
				ExcludeCutLines = TryClone(ExcludeCutLines),
				
				// Display Performance Options
				DisplayExcludeCutLines = TryClone(DisplayExcludeCutLines),
				DisplayRecalculateCutLines = TryClone(DisplayRecalculateCutLines),
				DisplayHideCutLines = TryClone(DisplayHideCutLines),
				DisplayHideCutLinesScale = TryClone(DisplayHideCutLinesScale),

				// Adjust settings
				Adjust = TryClone(Adjust),
				AdjustTolerance = TryClone(AdjustTolerance),
				AdjustExcludeCurves = TryClone(AdjustExcludeCurves),
				AdjustShowTolerance = TryClone(AdjustShowTolerance),

				// Buffer settings
				BufferTarget = TryClone(BufferTarget),
				BufferTolerance = TryClone(BufferTolerance),
				EnforceMinimumBufferSegmentLength = TryClone(EnforceMinimumBufferSegmentLength),
				MinBufferSegmentLength = TryClone(MinBufferSegmentLength),

				// Reshape line filter settings
				ExcludeLines = TryClone(ExcludeLines),
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
