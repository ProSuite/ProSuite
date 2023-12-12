using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.QA.Container;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.DomainServices.AO.Test.QA.Exceptions
{
	public class ExceptionObjectValuesPredicateTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanMatch()
		{
			Assert.True(Matches(1, 2, "test",
			                    new object[] { 1, 2, "test" }));
		}

		[Test]
		public void CanMatchOnlyText()
		{
			Assert.True(Matches(null, null, "test",
			                    new object[] { "test" }));
		}

		[Test]
		public void CanMatchOnlyDoubleValue()
		{
			Assert.True(Matches(99, null, null,
			                    new object[] { 99 }));
		}

		[Test]
		public void CanMatchIgnoringInsignificantDifferences()
		{
			Assert.True(Matches(1, 2, "test",
			                    new object[] { 1.000000000000001, 2, "test" },
			                    significantDigits: 1E-14));
		}

		[Test]
		public void CanMatchWithUndefinedExceptionValues()
		{
			Assert.True(Matches(null, null, null,
			                    new object[] { 1, 2, "test" }));
		}

		[Test]
		public void CanMatchWithAdditionalErrorValues()
		{
			Assert.True(Matches(1, 2, "test",
			                    new object[] { 1, 2, "test", "more_text", 99 }));
		}

		[Test]
		public void CanDetectSignificantDifferences()
		{
			Assert.False(Matches(1, 2, "test",
			                     new object[] { 1.0000000000001, 2, "test" },
			                     significantDigits: 1E-14));
		}

		[Test]
		public void CanDetectTextDifference()
		{
			Assert.False(Matches(null, null, "test",
			                     new object[] { "TEST" }));
		}

		[Test]
		public void CanIgnoreCaseOnlyTextDifference()
		{
			Assert.True(Matches(null, null, "test",
			                    new object[] { "TEST" },
			                    ignoreCase: true));
		}

		[Test]
		public void CanIgnoreTextDifferenceDueToLeadingOrTrailingWhitespace()
		{
			Assert.True(Matches(null, null, " test",
			                    new object[] { "test " }));
		}

		[Test]
		public void CanDetectMissingErrorValues()
		{
			Assert.False(Matches(1, 2, "test",
			                     new object[] { 1 }));
		}

		private static bool Matches(double? doubleValue1, double? doubleValue2,
		                            [CanBeNull] string textValue,
		                            [CanBeNull] IEnumerable<object> values,
		                            double significantDigits = 1E-7,
		                            bool ignoreLeadingAndTrailingWhitespace = true,
		                            bool ignoreCase = false)
		{
			ExceptionObject exceptionObject = CreateExceptionObject(1, doubleValue1,
				doubleValue2,
				textValue);
			ITable table = ExceptionObjectTestUtils.GetMockTable();

			QaError qaError = ExceptionObjectTestUtils.CreateQaError(table, null, null, values);

			var predicate = new ExceptionObjectValuesPredicate(significantDigits,
			                                                   ignoreLeadingAndTrailingWhitespace,
			                                                   ignoreCase);
			return predicate.Matches(exceptionObject, qaError);
		}

		[NotNull]
		private static ExceptionObject CreateExceptionObject(
			int id, double? doubleValue1, double? doubleValue2, string textValue)
		{
			return new ExceptionObject(id, new Guid(), new Guid(),
			                           null, null, null,
			                           ShapeMatchCriterion.EqualEnvelope,
			                           null, null,
			                           new InvolvedTable[] { },
			                           doubleValue1: doubleValue1,
			                           doubleValue2: doubleValue2,
			                           textValue: textValue);
		}
	}
}
