using System;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.QA.Container;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.DomainServices.AO.Test.QA.Exceptions
{
	[TestFixture]
	public class ExceptionObjectIssueCodePredicateTest
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
		public void CanMatchEqual()
		{
			Assert.True(Matches(" A.B", "a.b "));
		}

		[Test]
		public void CanMatchChild()
		{
			Assert.True(Matches("A.B", "a.b.c"));
		}

		[Test]
		public void CanMatchNullToDefined()
		{
			Assert.True(Matches(null, "a.b.c"));
		}

		[Test]
		public void CanMatchEmptyToDefined()
		{
			Assert.True(Matches(string.Empty, "a.b.c"));
		}

		[Test]
		public void CanDetectDistinct1()
		{
			Assert.False(Matches("A.B", "A"));
		}

		[Test]
		public void CanDetectDistinct2()
		{
			Assert.False(Matches("A.B", "C"));
		}

		[Test]
		public void CanDetectDistinct3()
		{
			Assert.False(Matches("A.B", " "));
		}

		private static bool Matches([CanBeNull] string exceptionObjectIssueCode,
		                            [CanBeNull] string qaErrorIssueCode)
		{
			ExceptionObject exceptionObject = CreateExceptionObject(1, exceptionObjectIssueCode);
			ITable table = ExceptionObjectTestUtils.GetMockTable();
			QaError qaError = ExceptionObjectTestUtils.CreateQaError(table, qaErrorIssueCode,
				null);

			var predicate = new ExceptionObjectIssueCodePredicate();

			return predicate.Matches(exceptionObject, qaError);
		}

		[NotNull]
		private static ExceptionObject CreateExceptionObject(int id,
		                                                     [CanBeNull] string issueCode)
		{
			return new ExceptionObject(id, new Guid(), new Guid(),
			                           null, null, null,
			                           ShapeMatchCriterion.EqualEnvelope,
			                           issueCode, null,
			                           new InvolvedTable[] { });
		}
	}
}
