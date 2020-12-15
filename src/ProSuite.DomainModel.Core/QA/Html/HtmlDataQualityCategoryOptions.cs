using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.Html
{
	public class HtmlDataQualityCategoryOptions
	{
		public HtmlDataQualityCategoryOptions([NotNull] string categoryUuid,
		                                      bool ignoreCategoryLevel = false,
		                                      [CanBeNull] string aliasName = null)
		{
			Assert.ArgumentNotNullOrEmpty(categoryUuid, nameof(categoryUuid));

			CategoryUuid = categoryUuid;
			IgnoreCategoryLevel = ignoreCategoryLevel;
			AliasName = aliasName;
		}

		[NotNull]
		public string CategoryUuid { get; private set; }

		public bool IgnoreCategoryLevel { get; private set; }

		[CanBeNull]
		public string AliasName { get; private set; }
	}
}
