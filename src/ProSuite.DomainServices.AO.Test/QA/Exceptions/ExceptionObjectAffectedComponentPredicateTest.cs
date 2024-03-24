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
	public class ExceptionObjectAffectedComponentPredicateTest
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
		public void CanMatchEmptyToEmpty()
		{
			Assert.True(Matches(string.Empty, string.Empty));
		}

		[Test]
		public void CanMatchNullToNull()
		{
			Assert.True(Matches(null, null));
		}

		[Test]
		public void CanMatchNullToDefined()
		{
			Assert.True(Matches(null, "FeldXY"));
		}

		[Test]
		public void CanMatchEmptyToDefined()
		{
			Assert.True(Matches(string.Empty, "FeldXY"));
		}

		[Test]
		public void CanMatchSingleValue()
		{
			Assert.True(Matches("FELDXY ", "  FeldXY"));
		}

		[Test]
		public void CanMatchMultivalue()
		{
			Assert.True(Matches(" FELDXY, FELDYZ", "feldyz FeldXY "));
		}

		[Test]
		public void CanDetectDistinctSingleValue()
		{
			Assert.False(Matches("FELDXY", "FELDYZ"));
		}

		[Test]
		public void CanDetectDistinctMultiValue()
		{
			Assert.False(Matches("FELDXY, FELDYZ", "FELDYZ"));
		}

		[Test]
		public void CanDetectDistinctMultiValueToEmpty()
		{
			Assert.False(Matches("FELDXY, FELDYZ", " "));
		}

		private static bool Matches([CanBeNull] string exceptionAffectedComponent,
		                            [CanBeNull] string qaErrorAffectedComponent)
		{
			ExceptionObject exceptionObject = CreateExceptionObject(1,
				exceptionAffectedComponent);
			ITable table = ExceptionObjectTestUtils.GetMockTable();
			QaError qaError = ExceptionObjectTestUtils.CreateQaError(table, null,
				qaErrorAffectedComponent);

			var predicate = new ExceptionObjectAffectedComponentPredicate();

			return predicate.Matches(exceptionObject, qaError);
		}

		[NotNull]
		private static ExceptionObject CreateExceptionObject(
			int id, [CanBeNull] string exceptionAffectedComponent)
		{
			return new ExceptionObject(id, new Guid(), new Guid(),
			                           null, null, null,
			                           ShapeMatchCriterion.EqualEnvelope,
			                           null, exceptionAffectedComponent,
			                           new InvolvedTable[] { });
		}
	}
}
