using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Misc;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.AO.QA
{
	public interface IQualityConditionContextAware : IContextAware
	{
		[CanBeNull]
		[Browsable(false)]
		ITestParameterDatasetProvider DatasetProvider { get; set; }

		[CanBeNull]
		[Browsable(false)]
		QualityCondition QualityCondition { get; set; }
	}
}
