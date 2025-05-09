using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Misc;
using ProSuite.DomainModel.Core.QA;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.Microservices.Client.AGP.QA;

public interface IIssueStore
{
	/// <summary>
	/// Deletes the errors in the specified perimeter and the given conditions / verified objects.
	/// </summary>
	/// <param name="deleteForConditionIds"></param>
	/// <param name="verifiedPerimeter"></param>
	/// <param name="verifiedObjects"></param>
	void DeleteErrors([CanBeNull] IList<int> deleteForConditionIds,
	                  [CanBeNull] Geometry verifiedPerimeter,
	                  [CanBeNull] IList<GdbObjectReference> verifiedObjects);

	/// <summary>
	/// Saves the provided issue messages in the error tables.
	/// </summary>
	/// <param name="issueMessages"></param>
	/// <param name="verifiedConditionIds"></param>
	/// <returns></returns>
	int SaveIssues([NotNull] IList<IssueMsg> issueMessages,
	               IEnumerable<int> verifiedConditionIds);

	/// <summary>
	/// Returns the datasets that are affected when storing the issue messages specified.
	/// </summary>
	/// <param name="issueMessages"></param>
	/// <returns></returns>
	IEnumerable<Dataset> GetReferencedIssueTables(IList<IssueMsg> issueMessages);

	/// <summary>
	/// Deletes the specified allowed errors.
	/// </summary>
	/// <param name="invalidAllowedErrorReferences"></param>
	/// <returns></returns>
	void DeleteInvalidAllowedErrors(IList<GdbObjectReference> invalidAllowedErrorReferences);

	/// <summary>
	/// Sets the verified specification or its ID. This will be used to get the relevant conditions.
	/// </summary>
	/// <param name="specification"></param>
	void SetVerifiedSpecification(Either<QualitySpecification, int> specification);

	/// <summary>
	/// Prepares the conditions in async method (within the edit transaction we have no async lambda available).
	/// </summary>
	/// <param name="allConditionsRequired"></param>
	/// <returns></returns>
	Task PrepareVerifiedConditions(bool allConditionsRequired);
}
