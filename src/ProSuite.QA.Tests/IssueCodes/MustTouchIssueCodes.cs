namespace ProSuite.QA.Tests.IssueCodes
{
	internal class MustTouchIssueCodes : LocalTestIssueCodes
	{
		public const string NoTouchingFeature = "NoTouchingFeature";

		public const string NoTouchingFeature_WithFulfilledConstraint =
			"NoTouchingFeature.WithFulfilledConstraint";

		public MustTouchIssueCodes() : base("MustTouch") { }
	}
}
