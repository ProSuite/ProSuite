namespace ProSuite.Commons.AGP.Core.GeometryProcessing.Cracker;

public interface ICrackerToolOptions
{
	TargetFeatureSelection TargetFeatureSelection { get; }
	bool RespectMinimumSegmentLength { get; }
	double MinimumSegmentLength { get; }
	bool SnapToTargetVertices { get; }
	double SnapTolerance { get; }
	bool UseSourceZs { get; }
}
