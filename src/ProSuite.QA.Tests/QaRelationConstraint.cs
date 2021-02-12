using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Constraints;
using ProSuite.QA.Tests.Documentation;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[InternallyUsedTest]
	public class QaRelationConstraint : QaConstraint
	{
		[Doc(nameof(DocStrings.QaRelationConstraint_0))]
		public QaRelationConstraint(
			[Doc(nameof(DocStrings.QaRelationConstraint_table))] ITable table,
			[Doc(nameof(DocStrings.QaRelationConstraint_constraint))]
			string constraint,
			[Doc(nameof(DocStrings.QaRelationConstraint_relatedTables))]
			IList<ITable> relatedTables)
			: base(table, constraint)
		{
			AddRelatedTables(table, relatedTables);
		}

		[Doc(nameof(DocStrings.QaRelationConstraint_1))]
		public QaRelationConstraint(
			[Doc(nameof(DocStrings.QaRelationConstraint_table))] ITable table,
			[Doc(nameof(DocStrings.QaRelationConstraint_constraints))]
			IList<ConstraintNode> constraints,
			[Doc(nameof(DocStrings.QaRelationConstraint_relatedTables))]
			IList<ITable> relatedTables)
			: base(table, constraints)
		{
			AddRelatedTables(table, relatedTables);
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

			ITable tbl = InvolvedTables[0];
			if (tbl.HasOID) // TODO intention?
			{
				return false;
			}

			if (tbl is IFeatureClass)
			{
				return true;
			}

			return false;
		}
	}
}
