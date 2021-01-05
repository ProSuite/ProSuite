using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.HtmlReports
{
	public class HtmlReportAreaOfInterest
	{
		public HtmlReportAreaOfInterest([NotNull] string type)
		{
			Assert.ArgumentNotNullOrEmpty(type, nameof(type));

			Type = type;
		}

		[UsedImplicitly]
		[CanBeNull]
		public string FeatureSource { get; set; }

		[UsedImplicitly]
		[NotNull]
		public string Type { get; private set; }

		[UsedImplicitly]
		[CanBeNull]
		public string Description { get; set; }

		[UsedImplicitly]
		[CanBeNull]
		public string WhereClause { get; set; }

		[UsedImplicitly]
		public double BufferDistance { get; set; }

		[UsedImplicitly]
		public double GeneralizationTolerance { get; set; }

		[UsedImplicitly]
		public bool UsesClipExtent { get; set; }

		[CanBeNull]
		[UsedImplicitly]
		public string ExtentString { get; set; }

		[CanBeNull]
		[UsedImplicitly]
		public string ClipExtentString { get; set; }
	}
}
