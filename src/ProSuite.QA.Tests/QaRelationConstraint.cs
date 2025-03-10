using System;
using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Constraints;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[InternallyUsedTest]
	public class QaRelationConstraint : QaConstraint
	{
		[Doc(nameof(DocStrings.QaRelationConstraint_0))]
		public QaRelationConstraint(
			[Doc(nameof(DocStrings.QaRelationConstraint_table))]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaRelationConstraint_constraint))]
			string constraint,
			[Doc(nameof(DocStrings.QaRelationConstraint_relatedTables))]
			IList<IReadOnlyTable> relatedTables)
			: base(table, constraint)
		{
		}

		[Doc(nameof(DocStrings.QaRelationConstraint_1))]
		public QaRelationConstraint(
			[Doc(nameof(DocStrings.QaRelationConstraint_table))]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaRelationConstraint_constraints))]
			IList<ConstraintNode> constraints,
			[Doc(nameof(DocStrings.QaRelationConstraint_relatedTables))]
			IList<IReadOnlyTable> relatedTables)
			: base(table, constraints)
		{
		}

		public override bool IsGeometryUsedTable(int tableIndex)
		{
			if (tableIndex != 0)
			{
				throw new NotSupportedException(string.Format("Invalid table index {0}",
				                                              tableIndex));
			}

			if (AreaOfInterest != null)
			{
				return true;
			}

			IReadOnlyTable tbl = InvolvedTables[0];
			if (tbl.HasOID) // TODO intention?
			{
				return false;
			}

			if (tbl is IReadOnlyFeatureClass)
			{
				return true;
			}

			return false;
		}
	}
}
