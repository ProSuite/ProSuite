using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.Microservices.Client.AGP.QA;

public interface IIssueStore
{
	void DeleteErrors([CanBeNull] IList<int> deleteForConditions,
	                  [NotNull] Geometry verifiedPerimeter,
	                  [CanBeNull] IList<GdbObjectReference> verifiedObjects);

	int SaveIssues([NotNull] IList<IssueMsg> issueMessages,
	               IList<int> verifiedConditionIds);

	IEnumerable<Dataset> GetReferencedIssueTables(IList<IssueMsg> issueMessages);

	int DeleteInvalidAllowedErrors(IList<GdbObjectReference> invalidAllowedErrorReferences);
}
