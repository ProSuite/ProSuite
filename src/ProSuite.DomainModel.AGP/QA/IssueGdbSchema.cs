using System.Collections.Generic;

namespace ProSuite.DomainModel.AGP.QA;

public static class IssueGdbSchema
{
	public static readonly IList<string> IssueFeatureClassNames =
		new List<string>
		{
			"IssuePoints",
			"IssueLines",
			"IssuePolygons",
			"IssueMultiPatches",
			"IssueRows"
		};
}
