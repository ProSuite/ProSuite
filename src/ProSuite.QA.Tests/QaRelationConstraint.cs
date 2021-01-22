using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Constraints;
using ProSuite.QA.Tests.Documentation;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
	[UsedImplicitly]
	[InternallyUsedTest]
	public class QaRelationConstraint : QaConstraint
	{
		[Doc("QaRelationConstraint_0")]
		public QaRelationConstraint(
			[Doc("QaRelationConstraint_table")] ITable table,
			[Doc("QaRelationConstraint_constraint")]
			string constraint,
			[Doc("QaRelationConstraint_relatedTables")]
			IList<ITable> relatedTables)
			: base(table, constraint)
		{
			AddRelatedTables(table, relatedTables);
		}

		[Doc("QaRelationConstraint_1")]
		public QaRelationConstraint(
			[Doc("QaRelationConstraint_table")] ITable table,
			[Doc("QaRelationConstraint_constraints")]
			IList<ConstraintNode> constraints,
			[Doc("QaRelationConstraint_relatedTables")]
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
