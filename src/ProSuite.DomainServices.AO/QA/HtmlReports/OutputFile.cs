using System.IO;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.HtmlReports
{
	public class OutputFile
	{
		public OutputFile([NotNull] string filePath)
		{
			Assert.ArgumentNotNullOrEmpty(filePath, nameof(filePath));

			FilePath = filePath;
			FileName = Path.GetFileName(filePath);
			Url = HtmlReportUtils.GetRelativeUrl(FileName);
		}

		[UsedImplicitly]
		[NotNull]
		public string FileName { get; private set; }

		[UsedImplicitly]
		[NotNull]
		public string FilePath { get; private set; }

		[UsedImplicitly]
		[NotNull]
		public string Url { get; private set; }

		public override string ToString()
		{
			return FileName;
		}
	}
}
