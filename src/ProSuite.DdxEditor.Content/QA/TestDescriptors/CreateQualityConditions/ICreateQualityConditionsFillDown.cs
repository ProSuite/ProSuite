using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors.CreateQualityConditions
{
	public interface ICreateQualityConditionsFillDown
	{
		void FillDown([NotNull] CellSelection cellSelection);
	}
}
