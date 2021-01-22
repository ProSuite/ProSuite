using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Constraints;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Licensing;

namespace ProSuite.QA.Tests.Test.Constraints
{
	[TestFixture]
	public class GdbConstraintUtilsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);

			_testWs = TestWorkspaceUtils.CreateTestFgdbWorkspace(
				"GdbConstraintUtilsTest");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanCreateIntegerCodedValueDomainConstraint()
		{
			ICodedValueDomain integerCvDomain = DomainUtils.CreateCodedValueDomain(
				"CanCreateIntegerCodedValueDomainConstraint",
				esriFieldType.esriFieldTypeInteger, null,
				esriSplitPolicyType.esriSPTDuplicate,
				esriMergePolicyType.esriMPTDefaultValue,
				new CodedValue(1, "Value 1"),
				new CodedValue(2, "Value 2"),
				new CodedValue(3, "Value 3"));
			DomainUtils.AddDomain(_testWs, integerCvDomain);

			IField integerField = FieldUtils.CreateIntegerField("IntegerField");
			((IFieldEdit) integerField).Domain_2 = (IDomain) integerCvDomain;

			ITable table = DatasetUtils.CreateTable(_testWs,
			                                        "CanCreateIntegerCodedValueDomainConstraint",
			                                        FieldUtils.CreateOIDField(),
			                                        integerField);

			List<ConstraintNode> constraints =
				GdbConstraintUtils.GetGdbConstraints(table);

			Assert.NotNull(constraints);

			foreach (ConstraintNode constraint in constraints)
			{
				Console.WriteLine(constraint);
			}

			Assert.AreEqual(2, constraints.Count);

			Assert.AreEqual("IntegerField IS NULL OR (IntegerField IN (1, 2, 3))",
			                constraints[0].Condition);
			Assert.AreEqual("OBJECTID >= 0", constraints[1].Condition);
		}

		[Test]
		public void CanCreateDoubleCodedValueDomainConstraint()
		{
			ICodedValueDomain doubleCvDomain = DomainUtils.CreateCodedValueDomain(
				"CanCreateDoubleCodedValueDomainConstraint",
				esriFieldType.esriFieldTypeDouble, null,
				esriSplitPolicyType.esriSPTDuplicate,
				esriMergePolicyType.esriMPTDefaultValue,
				new CodedValue(1.00000000001, "Value 1.00000000001"),
				new CodedValue(2.00000000002, "Value 2.00000000002"),
				new CodedValue(3.00000000003, "Value 3.00000000003"));
			DomainUtils.AddDomain(_testWs, doubleCvDomain);

			IField doubleField = FieldUtils.CreateDoubleField("DoubleField");
			((IFieldEdit) doubleField).Domain_2 = (IDomain) doubleCvDomain;

			ITable table = DatasetUtils.CreateTable(_testWs,
			                                        "CanCreateDoubleCodedValueDomainConstraint",
			                                        FieldUtils.CreateOIDField(),
			                                        doubleField);

			List<ConstraintNode> constraints =
				GdbConstraintUtils.GetGdbConstraints(table);

			Assert.NotNull(constraints);

			foreach (ConstraintNode constraint in constraints)
			{
				Console.WriteLine(constraint);
			}

			Assert.AreEqual(2, constraints.Count);

			Assert.AreEqual(
				"DoubleField IS NULL OR (Convert(DoubleField, 'System.Single') IN " +
				"(Convert(1.00000000001, 'System.Single'), " +
				"Convert(2.00000000002, 'System.Single'), " +
				"Convert(3.00000000003, 'System.Single')))",
				constraints[0].Condition);
			Assert.AreEqual("OBJECTID >= 0", constraints[1].Condition);
		}

		[Test]
		public void CanCreateStringCodedValueDomainConstraint()
		{
			ICodedValueDomain stringCvDomain = DomainUtils.CreateCodedValueDomain(
				"CanCreateStringCodedValueDomainConstraint",
				esriFieldType.esriFieldTypeString, null,
				esriSplitPolicyType.esriSPTDuplicate,
				esriMergePolicyType.esriMPTDefaultValue,
				new CodedValue("a", "Value a"),
				new CodedValue("b", "Value b"),
				new CodedValue("c", "Value c"));
			DomainUtils.AddDomain(_testWs, stringCvDomain);

			IField textField = FieldUtils.CreateTextField("TextField", 100);
			((IFieldEdit) textField).Domain_2 = (IDomain) stringCvDomain;

			ITable table = DatasetUtils.CreateTable(_testWs,
			                                        "CanCreateStringCodedValueDomainConstraint",
			                                        FieldUtils.CreateOIDField(),
			                                        textField);

			List<ConstraintNode> constraints =
				GdbConstraintUtils.GetGdbConstraints(table);

			Assert.NotNull(constraints);

			foreach (ConstraintNode constraint in constraints)
			{
				Console.WriteLine(constraint);
			}

			Assert.AreEqual(2, constraints.Count);

			Assert.AreEqual("TextField IS NULL OR (TextField IN ('a', 'b', 'c'))",
			                constraints[0].Condition);
			Assert.AreEqual("OBJECTID >= 0", constraints[1].Condition);
		}

		[Test]
		public void CanCreateStringCodedValueDomainConstraintNotNull()
		{
			ICodedValueDomain stringCvDomain = DomainUtils.CreateCodedValueDomain(
				"CanCreateStringCodedValueDomainConstraintNotNull",
				esriFieldType.esriFieldTypeString, null,
				esriSplitPolicyType.esriSPTDuplicate,
				esriMergePolicyType.esriMPTDefaultValue,
				new CodedValue("a", "Value a"),
				new CodedValue("b", "Value b"),
				new CodedValue("c", "Value c"));
			DomainUtils.AddDomain(_testWs, stringCvDomain);

			IField textField = FieldUtils.CreateTextField("TextField", 100);
			((IFieldEdit) textField).Domain_2 = (IDomain) stringCvDomain;

			ITable table = DatasetUtils.CreateTable(_testWs,
			                                        "CanCreateStringCodedValueDomainConstraintNotNull",
			                                        FieldUtils.CreateOIDField(),
			                                        textField);

			List<ConstraintNode> constraints =
				GdbConstraintUtils.GetGdbConstraints(
					table, allowNullForCodedValueDomains: false);

			Assert.NotNull(constraints);

			foreach (ConstraintNode constraint in constraints)
			{
				Console.WriteLine(constraint);
			}

			Assert.AreEqual(2, constraints.Count);

			Assert.AreEqual("TextField IN ('a', 'b', 'c')",
			                constraints[0].Condition);
			Assert.AreEqual("OBJECTID >= 0", constraints[1].Condition);
		}

		[Test]
		public void CanCreateIntegerRangeDomainConstraint()
		{
			IRangeDomain integerRangeDomain = DomainUtils.CreateRangeDomain(
				"CanCreateIntegerRangeDomainConstraint",
				esriFieldType.esriFieldTypeInteger, 0, 100);
			DomainUtils.AddDomain(_testWs, integerRangeDomain);

			IField integerField = FieldUtils.CreateIntegerField("IntegerField");
			((IFieldEdit) integerField).Domain_2 = (IDomain) integerRangeDomain;

			ITable table = DatasetUtils.CreateTable(_testWs,
			                                        "CanCreateIntegerRangeDomainConstraint",
			                                        FieldUtils.CreateOIDField(),
			                                        integerField);

			List<ConstraintNode> constraints =
				GdbConstraintUtils.GetGdbConstraints(table);

			Assert.NotNull(constraints);

			foreach (ConstraintNode constraint in constraints)
			{
				Console.WriteLine(constraint);
			}

			Assert.AreEqual(2, constraints.Count);

			Assert.AreEqual(
				"IntegerField IS NULL OR (IntegerField >= 0 AND IntegerField <= 100)",
				constraints[0].Condition);
			Assert.AreEqual("OBJECTID >= 0", constraints[1].Condition);
		}

		[Test]
		public void CanCreateIntegerRangeDomainConstraintNotNull()
		{
			IRangeDomain integerRangeDomain = DomainUtils.CreateRangeDomain(
				"CanCreateIntegerRangeDomainConstraintNotNull",
				esriFieldType.esriFieldTypeInteger, 0, 100);
			DomainUtils.AddDomain(_testWs, integerRangeDomain);

			IField integerField = FieldUtils.CreateIntegerField("IntegerField");
			((IFieldEdit) integerField).Domain_2 = (IDomain) integerRangeDomain;

			ITable table = DatasetUtils.CreateTable(_testWs,
			                                        "CanCreateIntegerRangeDomainConstraintNotNull",
			                                        FieldUtils.CreateOIDField(),
			                                        integerField);

			List<ConstraintNode> constraints =
				GdbConstraintUtils.GetGdbConstraints(
					table, allowNullForRangeDomains: false);

			Assert.NotNull(constraints);

			foreach (ConstraintNode constraint in constraints)
			{
				Console.WriteLine(constraint);
			}

			Assert.AreEqual(2, constraints.Count);

			Assert.AreEqual("IntegerField >= 0 AND IntegerField <= 100",
			                constraints[0].Condition);
			Assert.AreEqual("OBJECTID >= 0", constraints[1].Condition);
		}

		[Test]
		public void CanCreateDateTimeRangeDomainConstraintNotNull()
		{
			IRangeDomain dateRangeDomain = DomainUtils.CreateRangeDomain(
				"CanCreateDateTimeRangeDomainConstraintNotNull",
				esriFieldType.esriFieldTypeDate,
				new DateTime(2011, 12, 31),
				new DateTime(2012, 1, 31, 23, 55, 59));
			DomainUtils.AddDomain(_testWs, dateRangeDomain);

			IField dateField = FieldUtils.CreateDateField("DateField");
			((IFieldEdit) dateField).Domain_2 = (IDomain) dateRangeDomain;

			ITable table = DatasetUtils.CreateTable(_testWs,
			                                        "CanCreateDateTimeRangeDomainConstraintNotNull",
			                                        FieldUtils.CreateOIDField(),
			                                        dateField);

			List<ConstraintNode> constraints =
				GdbConstraintUtils.GetGdbConstraints(
					table, allowNullForRangeDomains: false);

			Assert.NotNull(constraints);

			foreach (ConstraintNode constraint in constraints)
			{
				Console.WriteLine(constraint);
			}

			Assert.AreEqual(2, constraints.Count);

			Assert.AreEqual(
				"DateField >= #12/31/2011 00:00:00# AND DateField <= #01/31/2012 23:55:59#",
				constraints[0].Condition);
			Assert.AreEqual("OBJECTID >= 0", constraints[1].Condition);
		}
	}
}
