using ProSuite.Commons.Db;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	// Use exactly the same name? Or append Def? -> QaConstraintDef?
	[AttributeTest]
	public class QaConstraintDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }
		public string Constraint { get; }

		[Doc(nameof(DocStrings.QaConstraint_0))]
		public QaConstraintDefinition(
				[Doc(nameof(DocStrings.QaConstraint_table))] [NotNull]
				ITableSchemaDef table,
				[Doc(nameof(DocStrings.QaConstraint_constraint))]
				string constraint)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, constraint, 0) { }

		// Internally used tests can be ignored, unless they are referenced by another constructor!?!
		//[Doc("nameof(DocStrings.QaConstraint_1)")]
		//[InternallyUsedTest]
		//public QaConstraint(
		//		[Doc("nameof(DocStrings.QaConstraint_table)")] [NotNull]
		//		IObjectDataset table,
		//		[Doc("nameof(DocStrings.QaConstraint_constraints)")] [NotNull]
		//		IList<ConstraintNode> constraints)
		//	// ReSharper disable once IntroduceOptionalParameters.Global
		//	: this(table, constraints, 0) { }

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

		//[Doc("nameof(DocStrings.QaConstraint_1)")]
		//[InternallyUsedTest]
		//public QaConstraint(
		//	[Doc(nameof(DocStrings.QaConstraint_table))] [NotNull]
		//	IObjectDataset table,
		//	[Doc(nameof(DocStrings.QaConstraint_constraints))] [NotNull]
		//	IList<ConstraintNode> constraints,
		//	int errorDescriptionVersion)
		//	: base(table)
		//{
		//	Assert.ArgumentNotNull(table, nameof(table));
		//	Assert.ArgumentNotNull(constraints, nameof(constraints));

		//	_table = table;
		//	_constraintNodes = constraints;
		//	_usesSimpleConstraint = false;
		//	_errorDescriptionVersion = errorDescriptionVersion;
		//}
	}
}
