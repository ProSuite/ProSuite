using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.QA
{
	public interface IErrorObject
	{
		int? QualityConditionId { get; }

		string QualityConditionName { get; }

		string ErrorDescription { get; }

		[CanBeNull]
		string AffectedComponent { get; }

		ErrorType ErrorType { get; }

		string RawInvolvedObjects { get; }
	}
}
