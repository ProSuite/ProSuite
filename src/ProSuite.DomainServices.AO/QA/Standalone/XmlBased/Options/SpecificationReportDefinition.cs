using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA.Html;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options
{
	public class SpecificationReportDefinition :
		IHtmlDataQualityCategoryOptionsProvider
	{
		[NotNull] private readonly IDictionary<string, HtmlDataQualityCategoryOptions>
			_categoryOptionsByUuid;

		public SpecificationReportDefinition(
			[NotNull] string templatePath,
			[NotNull] string fileName,
			[NotNull] IEnumerable<HtmlDataQualityCategoryOptions> categoryOptions)
		{
			Assert.ArgumentNotNullOrEmpty(templatePath, nameof(templatePath));
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));
			Assert.ArgumentNotNull(categoryOptions, nameof(categoryOptions));

			TemplatePath = templatePath;
			FileName = fileName;

			_categoryOptionsByUuid =
				categoryOptions.ToDictionary(options => options.CategoryUuid);
		}

		[NotNull]
		public string TemplatePath { get; }

		[NotNull]
		public string FileName { get; }

		HtmlDataQualityCategoryOptions IHtmlDataQualityCategoryOptionsProvider.
			GetCategoryOptions(string uuid)
		{
			Assert.ArgumentNotNullOrEmpty(uuid, nameof(uuid));
			HtmlDataQualityCategoryOptions options;
			return _categoryOptionsByUuid.TryGetValue(uuid, out options) ? options : null;
		}
	}
}
