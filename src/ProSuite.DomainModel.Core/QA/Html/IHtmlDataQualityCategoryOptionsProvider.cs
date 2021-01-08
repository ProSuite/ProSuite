using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.Html
{
	public interface IHtmlDataQualityCategoryOptionsProvider
	{
		[CanBeNull]
		HtmlDataQualityCategoryOptions GetCategoryOptions([NotNull] string uuid);
	}
}