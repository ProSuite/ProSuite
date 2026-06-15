namespace ProSuite.Commons.AGP.Core.GeometryProcessing.ChangeAlong;

public enum ReshapeAlongCurveUsability
{
	Undefined = 0,
	NoSource = 1,
	NoTarget = 2,
	AlreadyCongruent = 3,
	NoReshapeCurves = 4,
	InsufficientOrAmbiguousReshapeCurves = 5,
	CanReshape = 6
}
