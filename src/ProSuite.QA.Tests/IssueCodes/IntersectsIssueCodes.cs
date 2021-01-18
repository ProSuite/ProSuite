namespace ProSuite.QA.Tests.IssueCodes
{
	internal class IntersectsIssueCodes : LocalTestIssueCodes
	{
		public const string GeometriesIntersect = "GeometriesIntersect";

		public const string GeometriesIntersect_ConstraintNotFulfilled =
			"GeometriesIntersect.ConstraintNotFulfilled";

		public IntersectsIssueCodes() : base("Intersects") { }
	}
}
