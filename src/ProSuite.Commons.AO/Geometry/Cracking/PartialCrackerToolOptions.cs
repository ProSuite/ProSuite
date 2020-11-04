﻿using ProSuite.Commons.ManagedOptions;

namespace ProSuite.Commons.AO.Geometry.Cracking
{
	public class PartialCrackerToolOptions : PartialOptionsBase
	{
		#region Overridable Settings

		public OverridableSetting<TargetFeatureSelection> TargetFeatureSelection { get; set; }

		public OverridableSetting<bool> RespectMinimumSegmentLength { get; set; }
		public OverridableSetting<double> MinimumSegmentLength { get; set; }

		public OverridableSetting<bool> SnapToTargetVertices { get; set; }
		public OverridableSetting<double> SnapTolerance { get; set; }

		public OverridableSetting<bool> RemoveUnnecessaryVertices { get; set; }

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
				             UseSourceZs = TryClone(UseSourceZs),
				             RemoveUnnecessaryVertices = TryClone(RemoveUnnecessaryVertices)
			             };

			return result;
		}
	}
}
