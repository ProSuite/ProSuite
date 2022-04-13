using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public interface IQualitySpecificationContainerItem
	{
		void AddNewQualitySpecificationItem();

		void CreateCopy([NotNull] QualitySpecificationItem item);

		bool AssignToCategory([NotNull] ICollection<QualitySpecificationItem> items,
		                      [NotNull] IWin32Window owner,
		                      [CanBeNull] out DataQualityCategory category);

		[NotNull]
		IEnumerable<QualitySpecification> GetQualitySpecifications(
			bool includeSubCategories = false);

		[CanBeNull]
		QualitySpecification GetQualitySpecification([NotNull] QualitySpecificationItem item);
	}
}
