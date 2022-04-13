using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors.CreateQualityConditions
{
	internal interface ICreateQualityConditionsFillDown
	{
		void FillDown([NotNull] CellSelection cellSelection);
	}
}
