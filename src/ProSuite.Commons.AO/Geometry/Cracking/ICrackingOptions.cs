using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.Cracking
{
	public interface ICrackingOptions
	{
		TargetFeatureSelection TargetFeatureSelection { get; }

		bool RespectMinimumSegmentLength { get; }
		double MinimumSegmentLength { get; }

		bool SnapToTargetVertices { get; }
		double SnapTolerance { get; }

		bool UseSourceZs { get; }

		[CanBeNull]
		string GetLocalOverridesMessage();
	}
}
