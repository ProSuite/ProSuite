using System;
using System.Collections.Generic;
using System.Diagnostics;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Constraints;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaConstraintTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanUseCaseSensitivityForSimpleCondition()
		{
			const string textFieldName = "TextField";
			const string value = "aaa";

			int textFieldIndex;
			ObjectClassMock objectClass = CreateObjectClass(textFieldName, out textFieldIndex);

			IObject rowUpperCase = CreateRow(objectClass, 1, textFieldIndex, value.ToUpper());
			IObject rowLowerCase = CreateRow(objectClass, 2, textFieldIndex, value.ToLower());

			string constraint = string.Format("{0} = '{1}'", textFieldName, value.ToUpper());

			var caseSensitiveTest = new QaConstraint(objectClass, constraint);
			caseSensitiveTest.SetSqlCaseSensitivity(0, true);

			var caseSensitiveRunner = new QaTestRunner(caseSensitiveTest);

			Assert.AreEqual(0, caseSensitiveRunner.Execute(rowUpperCase));
			Assert.AreEqual(1, caseSensitiveRunner.Execute(rowLowerCase)); // incorrect case
			Assert.AreEqual(
				"testtable,2: TEXTFIELD = 'aaa' [Constraints.ConstraintNotFulfilled] {TextField}",
				caseSensitiveRunner.Errors[0].ToString());
			caseSensitiveRunner.ClearErrors();

			var caseInsensitiveTest = new QaConstraint(objectClass, constraint);

			var caseInsensitiveRunner = new QaTestRunner(caseInsensitiveTest);

			Assert.AreEqual(0, caseInsensitiveRunner.Execute(rowUpperCase));
			Assert.AreEqual(0, caseInsensitiveRunner.Execute(rowLowerCase));
		}

		[Test]
		public void CanOverrideCaseSensitivityForSimpleCondition()
		{
			const string textFieldName = "TextField";
			const string value = "aaa";

			int textFieldIndex;
			ObjectClassMock objectClass = CreateObjectClass(textFieldName, out textFieldIndex);

			IObject rowUpperCase = CreateRow(objectClass, 1, textFieldIndex, value.ToUpper());
			IObject rowLowerCase = CreateRow(objectClass, 2, textFieldIndex, value.ToLower());

			string constraint = string.Format("{0} = '{1}'", textFieldName, value.ToUpper());

			var caseSensitiveTest = new QaConstraint(objectClass,
			                                         constraint +
			                                         ExpressionUtils.IgnoreCaseHint);
			caseSensitiveTest.SetSqlCaseSensitivity(0, true);

			var caseSensitiveRunner = new QaTestRunner(caseSensitiveTest);

			Assert.AreEqual(0, caseSensitiveRunner.Execute(rowUpperCase));
			// differing case, but ignored based on hint:
			Assert.AreEqual(0, caseSensitiveRunner.Execute(rowLowerCase));

			var caseInsensitiveTest = new QaConstraint(objectClass,
			                                           constraint +
			                                           ExpressionUtils.CaseSensitivityHint);

			var caseInsensitiveRunner = new QaTestRunner(caseInsensitiveTest);

			Assert.AreEqual(0, caseInsensitiveRunner.Execute(rowUpperCase));
			// differing case, not ignored based on hint:
			Assert.AreEqual(1, caseInsensitiveRunner.Execute(rowLowerCase));
			Assert.AreEqual(
				"testtable,2: TEXTFIELD = 'aaa' [Constraints.ConstraintNotFulfilled] {TextField}",
				caseInsensitiveRunner.Errors[0].ToString());
		}

		[Test]
		public void CanOverrideCaseSensitivityWithoutPerformanceImpact()
		{
			const string textFieldName = "TEXTFIELD";
			const string value = "aaa";
			const int count = 50000;

			int textFieldIndex;
			ObjectClassMock objectClass = CreateObjectClass(textFieldName, out textFieldIndex);

			// expect upper case for oids <= 2
			string mustBeUpper = string.Format("{0} = '{1}'", textFieldName, value.ToUpper());
			string mustBeEqualIgnoreCase = string.Format("{0} = '{1}'##IGNORECASE",
			                                             textFieldName, value.ToLower());

			IObject row = CreateRow(objectClass, 1, textFieldIndex, value.ToUpper());

			double referencePerformance = GetReferencePerformance(textFieldName, value, row,
			                                                      count);

			var constraintNodes = new List<ConstraintNode>
			                      {
				                      new ConstraintNode(mustBeUpper),
				                      new ConstraintNode(mustBeEqualIgnoreCase)
			                      };

			var caseSensitiveTest = new QaConstraint(objectClass, constraintNodes);
			caseSensitiveTest.SetSqlCaseSensitivity(0, true);

			var caseSensitiveRunner = new QaTestRunner(caseSensitiveTest);

			Stopwatch stopWatch = Stopwatch.StartNew();

			for (int i = 0; i < count; i++)
			{
				Assert.AreEqual(0, caseSensitiveRunner.Execute(row));
			}

			stopWatch.Stop();

			double milliSecondsPerRow = stopWatch.ElapsedMilliseconds / (double) count;
			Console.WriteLine(@"{0:N0} ms ({1:N3} ms per row)",
			                  stopWatch.ElapsedMilliseconds, milliSecondsPerRow);
			Console.WriteLine(@"Reference (no overrides): {0:N3} ms per row",
			                  referencePerformance);
			Assert.Less(milliSecondsPerRow, referencePerformance * 1.5);
			// allow for some variation
		}

		[Test]
		public void CanUseCaseSensitivityForComplexCondition()
		{
			const string textFieldName = "TextField";
			const string value = "aaa";

			int textFieldIndex;
			ObjectClassMock objectClass = CreateObjectClass(textFieldName, out textFieldIndex);

			// expect upper case for oids <= 2
			string mustBeUpper = string.Format("{0} = '{1}'", textFieldName, value.ToUpper());
			var selection1 = new ConstraintNode("OBJECTID <= 2");
			selection1.Nodes.Add(new ConstraintNode(mustBeUpper));

			IObject row1 = CreateRow(objectClass, 1, textFieldIndex, value.ToUpper());
			IObject row2 = CreateRow(objectClass, 2, textFieldIndex, value.ToLower());
			// incorrect case

			// expect lower case for oids > 2
			string mustBeLower = string.Format("{0} = '{1}'", textFieldName, value.ToLower());
			var selection2 = new ConstraintNode("OBJECTID > 2");
			selection2.Nodes.Add(new ConstraintNode(mustBeLower));

			IObject row3 = CreateRow(objectClass, 3, textFieldIndex, value.ToLower());
			IObject row4 = CreateRow(objectClass, 4, textFieldIndex, value.ToUpper());
			// incorrect case
			IObject row5 = CreateRow(objectClass, 5, textFieldIndex, value.ToUpper());
			// incorrect case

			var constraintNodes = new List<ConstraintNode> {selection1, selection2};

			var caseSensitiveTest = new QaConstraint(objectClass, constraintNodes);
			caseSensitiveTest.SetSqlCaseSensitivity(0, true);

			var caseSensitiveRunner = new QaTestRunner(caseSensitiveTest);

			Assert.AreEqual(0, caseSensitiveRunner.Execute(row1));
			Assert.AreEqual(1, caseSensitiveRunner.Execute(row2));
			Assert.AreEqual(
				"testtable,2: OID = 2: Invalid value combination: OBJECTID = 2; TEXTFIELD = 'aaa' [Constraints.ConstraintNotFulfilled]",
				caseSensitiveRunner.Errors[0].ToString());
			caseSensitiveRunner.ClearErrors();

			Assert.AreEqual(0, caseSensitiveRunner.Execute(row3));
			Assert.AreEqual(1, caseSensitiveRunner.Execute(row4));
			Assert.AreEqual(
				"testtable,4: OID = 4: Invalid value combination: OBJECTID = 4; TEXTFIELD = 'AAA' [Constraints.ConstraintNotFulfilled]",
				caseSensitiveRunner.Errors[0].ToString());
			caseSensitiveRunner.ClearErrors();

			Assert.AreEqual(1, caseSensitiveRunner.Execute(row5));
			Assert.AreEqual(
				"testtable,5: OID = 5: Invalid value combination: OBJECTID = 5; TEXTFIELD = 'AAA' [Constraints.ConstraintNotFulfilled]",
				caseSensitiveRunner.Errors[0].ToString());
			caseSensitiveRunner.ClearErrors();

			var caseInsensitiveTest = new QaConstraint(objectClass, constraintNodes);

			var caseInsensitiveRunner = new QaTestRunner(caseInsensitiveTest);

			Assert.AreEqual(0, caseInsensitiveRunner.Execute(row1));
			Assert.AreEqual(0, caseInsensitiveRunner.Execute(row2));
			Assert.AreEqual(0, caseInsensitiveRunner.Execute(row3));
			Assert.AreEqual(0, caseInsensitiveRunner.Execute(row4));
			Assert.AreEqual(0, caseInsensitiveRunner.Execute(row5));
		}

		[Test]
		public void CanOverrideCaseSensitivityForComplexCondition()
		{
			const string textFieldName = "TextField";
			const string value = "aaa";

			int textFieldIndex;
			ObjectClassMock objectClass = CreateObjectClass(textFieldName, out textFieldIndex);

			string ignoreCaseOverride = string.Format("{0} = '{1}'##IGNORECASE", textFieldName,
			                                          value.ToUpper());
			string caseSensitiveOverride = string.Format("{0} = '{1}'##CASESENSITIVE",
			                                             textFieldName, value.ToLower());

			// expect upper case for oids <= 2
			string mustBeUpper = string.Format("{0} = '{1}'", textFieldName, value.ToUpper());
			var selection1 = new ConstraintNode("OBJECTID <= 2");
			selection1.Nodes.Add(new ConstraintNode(mustBeUpper));
			selection1.Nodes.Add(new ConstraintNode(ignoreCaseOverride));

			IObject row1 = CreateRow(objectClass, 1, textFieldIndex, value.ToUpper());
			IObject row2 = CreateRow(objectClass, 2, textFieldIndex, value.ToLower());
			// incorrect case

			// expect lower case for oids > 2
			string mustBeLower = string.Format("{0} = '{1}'", textFieldName, value.ToLower());
			var selection2 = new ConstraintNode("OBJECTID > 2");
			selection2.Nodes.Add(new ConstraintNode(mustBeLower));
			selection2.Nodes.Add(new ConstraintNode(ignoreCaseOverride));

			IObject row3 = CreateRow(objectClass, 3, textFieldIndex, value.ToLower());
			IObject row4 = CreateRow(objectClass, 4, textFieldIndex, value.ToUpper());
			// incorrect case
			IObject row5 = CreateRow(objectClass, 5, textFieldIndex, value.ToUpper());
			// incorrect case

			var constraintNodes = new List<ConstraintNode> {selection1, selection2};

			var caseSensitiveTest = new QaConstraint(objectClass, constraintNodes);
			caseSensitiveTest.SetSqlCaseSensitivity(0, true);

			var caseSensitiveRunner = new QaTestRunner(caseSensitiveTest);

			Assert.AreEqual(0, caseSensitiveRunner.Execute(row1));
			Assert.AreEqual(1, caseSensitiveRunner.Execute(row2));
			Assert.AreEqual(
				"testtable,2: OID = 2: Invalid value combination: OBJECTID = 2; TEXTFIELD = 'aaa' [Constraints.ConstraintNotFulfilled]",
				caseSensitiveRunner.Errors[0].ToString());
			caseSensitiveRunner.ClearErrors();

			Assert.AreEqual(0, caseSensitiveRunner.Execute(row3));

			Assert.AreEqual(1, caseSensitiveRunner.Execute(row4));
			Assert.AreEqual(
				"testtable,4: OID = 4: Invalid value combination: OBJECTID = 4; TEXTFIELD = 'AAA' [Constraints.ConstraintNotFulfilled]",
				caseSensitiveRunner.Errors[0].ToString());
			caseSensitiveRunner.ClearErrors();

			Assert.AreEqual(1, caseSensitiveRunner.Execute(row5));
			Assert.AreEqual(
				"testtable,5: OID = 5: Invalid value combination: OBJECTID = 5; TEXTFIELD = 'AAA' [Constraints.ConstraintNotFulfilled]",
				caseSensitiveRunner.Errors[0].ToString());
			caseSensitiveRunner.ClearErrors();

			var caseInsensitiveTest = new QaConstraint(objectClass, constraintNodes);
			selection2.Nodes.Add(new ConstraintNode(caseSensitiveOverride));
			var caseInsensitiveRunner = new QaTestRunner(caseInsensitiveTest);

			Assert.AreEqual(0, caseInsensitiveRunner.Execute(row1));
			Assert.AreEqual(0, caseInsensitiveRunner.Execute(row2));
			Assert.AreEqual(0, caseInsensitiveRunner.Execute(row3));

			Assert.AreEqual(1, caseInsensitiveRunner.Execute(row4));
			Assert.AreEqual(
				"testtable,4: OID = 4: Invalid value combination: OBJECTID = 4; TEXTFIELD = 'AAA' [Constraints.ConstraintNotFulfilled]",
				caseInsensitiveRunner.Errors[0].ToString());
			caseInsensitiveRunner.ClearErrors();

			Assert.AreEqual(1, caseInsensitiveRunner.Execute(row5));
			Assert.AreEqual(
				"testtable,5: OID = 5: Invalid value combination: OBJECTID = 5; TEXTFIELD = 'AAA' [Constraints.ConstraintNotFulfilled]",
				caseInsensitiveRunner.Errors[0].ToString());
			caseInsensitiveRunner.ClearErrors();
		}

		private static double GetReferencePerformance([NotNull] string textFieldName,
		                                              [NotNull] string value,
		                                              [NotNull] IObject row,
		                                              int count)
		{
			// expect upper case for oids <= 2
			string mustBeUpper = string.Format("{0} = '{1}'", textFieldName, value.ToUpper());

			var constraintNodes = new List<ConstraintNode>
			                      {
				                      new ConstraintNode(mustBeUpper),
				                      new ConstraintNode(mustBeUpper)
			                      };

			var caseSensitiveTest = new QaConstraint((ITable) row.Class, constraintNodes);
			caseSensitiveTest.SetSqlCaseSensitivity(0, true);

			var caseSensitiveRunner = new QaTestRunner(caseSensitiveTest);

			Stopwatch stopWatch = Stopwatch.StartNew();

			for (int i = 0; i < count; i++)
			{
				Assert.AreEqual(0, caseSensitiveRunner.Execute(row));
			}

			stopWatch.Stop();

			return stopWatch.ElapsedMilliseconds / (double) count;
		}

		[NotNull]
		private static ObjectClassMock CreateObjectClass([NotNull] string textFieldName,
		                                                 out int textFieldIndex)
		{
			var result = new ObjectClassMock(1, "testtable");

			result.AddField(FieldUtils.CreateTextField(textFieldName, 500));

			textFieldIndex = result.FindField(textFieldName);
			Assert.GreaterOrEqual(textFieldIndex, 0);

			return result;
		}

		[NotNull]
		private static IObject CreateRow([NotNull] ObjectClassMock mockObjectClass,
		                                 int oid,
		                                 int textFieldIndex,
		                                 string textFieldValue)
		{
			IObject result = mockObjectClass.CreateObject(oid);

			result.Value[textFieldIndex] = (object) textFieldValue ?? DBNull.Value;

			return result;
		}
	}
}