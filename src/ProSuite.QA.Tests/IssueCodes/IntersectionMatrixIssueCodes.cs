namespace ProSuite.QA.Tests.IssueCodes
{
	internal class IntersectionMatrixIssueCodes : LocalTestIssueCodes
	{
		public const string GeometriesIntersectWithMatrix = "GeometriesIntersectWithMatrix";

		public const string GeometriesIntersectWithMatrix_ConstraintNotFulfilled =
			"GeometriesIntersectWithMatrix.ConstraintNotFulfilled";

		public const string NoIntersection = "NoIntersection";

		public IntersectionMatrixIssueCodes() : base("IntersectionMatrix") { }
	}
}
