namespace ProSuite.QA.Tests.IssueCodes
{
	internal class MustIntersectIssueCodes : LocalTestIssueCodes
	{
		public const string NoIntersectingFeature = "NoIntersectingFeature";

		public const string NoIntersectingFeature_WithFulfilledConstraint =
			"NoIntersectingFeature.WithFulfilledConstraint";

		public MustIntersectIssueCodes() : base("MustIntersect") { }
	}
}
