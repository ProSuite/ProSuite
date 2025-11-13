using System.Collections.Generic;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.IssueFilters
{
	public class IfInvolvedRowsDefinition : AlgorithmDefinition
	{
		public string Constraint { get; }

		[DocIf(nameof(DocIfStrings.IfInvolvedRows_0))]
		public IfInvolvedRowsDefinition(
			[DocIf(nameof(DocIfStrings.IfInvolvedRows_constraint))]
			string constraint)
			: base(new ITableSchemaDef[] { })
		{
			Constraint = constraint;
		}

		[TestParameter]
		[DocIf(nameof(DocIfStrings.IfInvolvedRows_Tables))]
		public IList<ITableSchemaDef> Tables { get; set; }
	}
}
