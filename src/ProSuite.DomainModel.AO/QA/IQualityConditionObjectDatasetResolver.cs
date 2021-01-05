using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.AO.QA
{
	[CLSCompliant(false)]
	public interface IQualityConditionObjectDatasetResolver
	{
		[CanBeNull]
		IObjectDataset GetDatasetByInvolvedRowTableName(
			[NotNull] string involvedRowTableName,
			[NotNull] QualityCondition qualityCondition);

		[CanBeNull]
		IObjectDataset GetDatasetByGdbTableName(
			[NotNull] string gdbTableName,
			[NotNull] QualityCondition qualityCondition);
	}
}
