using System;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.Test.QA.Exceptions
{
	[TestFixture]
	public class ExceptionObjectIssueCodePredicateTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
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
