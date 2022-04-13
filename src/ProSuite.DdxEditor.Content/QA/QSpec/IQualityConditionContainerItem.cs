using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.QCon;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public interface IQualityConditionContainerItem
	{
		void AddNewQualityConditionItem();

		void CreateCopy([NotNull] QualityConditionItem item);

		bool AssignToCategory([NotNull] ICollection<QualityConditionItem> items,
		                      [NotNull] IWin32Window owner,
		                      [CanBeNull] out DataQualityCategory category);

		[CanBeNull]
		QualityCondition GetQualityCondition([NotNull] QualityConditionItem item);
	}
}
