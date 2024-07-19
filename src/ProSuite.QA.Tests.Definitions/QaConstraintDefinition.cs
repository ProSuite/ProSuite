using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaConstraintDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }

		[SqlExpression(nameof(Table))]
		public string Constraint { get; }

		// Internally used by factories:
		public int ErrorDescriptionVersion { get; }
		public IList<ConstraintNodeDefinition> ConstraintNodes { get; }

		[Doc(nameof(DocStrings.QaConstraint_0))]
		public QaConstraintDefinition(
				[Doc(nameof(DocStrings.QaConstraint_table))] [NotNull]
				ITableSchemaDef table,
				[Doc(nameof(DocStrings.QaConstraint_constraint))]
				string constraint)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, constraint, 0) { }

		// Presumably absolutely all constructors must be available in order to avoid
		// constructor index mix-ups.
		[Doc(nameof(DocStrings.QaConstraint_1))]
		[InternallyUsedTest]
		public QaConstraintDefinition(
				[Doc(nameof(DocStrings.QaConstraint_table))] [NotNull]
				ITableSchemaDef table,
				[Doc(nameof(DocStrings.QaConstraint_constraints))] [NotNull]
				IList<ConstraintNodeDefinition> constraints)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, constraints, 0) { }

		[Doc(nameof(DocStrings.QaConstraint_0))]
		[InternallyUsedTest]
		public QaConstraintDefinition(
			[Doc(nameof(DocStrings.QaConstraint_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaConstraint_constraint))]
			string constraint,
			int errorDescriptionVersion)
			: base(table)
		{
			Table = table;
			Constraint = constraint;
		}

		[Doc(nameof(DocStrings.QaConstraint_1))]
		[InternallyUsedTest]
		public QaConstraintDefinition(
			[Doc(nameof(DocStrings.QaConstraint_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaConstraint_constraints))] [NotNull]
			IList<ConstraintNodeDefinition> constraints,
			int errorDescriptionVersion)
			: base(table)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(constraints, nameof(constraints));

			Table = table;
			ConstraintNodes = constraints;
			ErrorDescriptionVersion = errorDescriptionVersion;
		}
	}
}
