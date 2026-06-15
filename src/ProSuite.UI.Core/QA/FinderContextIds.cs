using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.UI.Core.QA
{
	public static class FinderContextIds
	{
		[NotNull]
		public static string GetId([CanBeNull] DataQualityCategory category)
		{
			return category == null
				       ? "root"
				       : string.Format("catid_{0}", category.Id);
		}
	}
}
