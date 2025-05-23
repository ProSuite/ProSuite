using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.Cracker
{
	public class PartialCrackerToolOptions : PartialOptionsBase
	{
		#region Overridable Settings

		public OverridableSetting<TargetFeatureSelection> TargetFeatureSelection { get; set; }

		public OverridableSetting<bool> RespectMinimumSegmentLength { get; set; }
		public OverridableSetting<double> MinimumSegmentLength { get; set; }

		public OverridableSetting<bool> SnapToTargetVertices { get; set; }
		public OverridableSetting<double> SnapTolerance { get; set; }
		
		public OverridableSetting<bool> UseSourceZs { get; set; }

		#endregion

		public override PartialOptionsBase Clone()
		{
			var result = new PartialCrackerToolOptions
			             {
				             TargetFeatureSelection = TryClone(TargetFeatureSelection),
				             RespectMinimumSegmentLength = TryClone(RespectMinimumSegmentLength),
				             MinimumSegmentLength = TryClone(MinimumSegmentLength),
				             SnapToTargetVertices = TryClone(SnapToTargetVertices),
				             SnapTolerance = TryClone(SnapTolerance),
				             UseSourceZs = TryClone(UseSourceZs)
			             };

			return result;
		}
	}
}
