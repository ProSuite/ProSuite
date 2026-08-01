using System.Collections.Generic;
using System.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.AGP.QA;

/// <summary>
/// Provides the necessary schema context for storing issues in model error datasets.
/// Previously known as IVerificationContext.
/// </summary>
public interface IIssueStoreContext
{
	bool CanWriteIssues { get; }

	[CanBeNull]
	ErrorLineDataset LineIssueDataset { get; }

	[CanBeNull]
	ErrorPolygonDataset PolygonIssueDataset { get; }

	[CanBeNull]
	ErrorMultipointDataset MultipointIssueDataset { get; }

	[CanBeNull]
	ErrorMultiPatchDataset MultiPatchIssueDataset { get; }

	[CanBeNull]
	ErrorTableDataset NoGeometryIssueDataset { get; }

	[NotNull]
	Task<QualitySpecification> GetQualitySpecification(int ddxId);

	/// <summary>
	/// Gets the fully populated quality conditions with the given data dictionary ids.
	/// Conditions for unknown ids are silently omitted from the result.
	/// </summary>
	/// <param name="conditionIds"></param>
	/// <returns></returns>
	[NotNull]
	Task<IList<QualityCondition>> GetQualityConditions([NotNull] IList<int> conditionIds);
}
