using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.QA.Container;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.DomainServices.AO.Test.QA.Exceptions
{
	[TestFixture]
	public class ExceptionObjectInvolvedRowsPredicateTest
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
			var involvedRows = new List<InvolvedRow>
			                   {
				                   new InvolvedRow("Table2", 100),
				                   new InvolvedRow("Table1", 1000),
				                   new InvolvedRow("Table2", 200)
			                   };

			var involvedTables =
				new List<InvolvedTable>
				{
					new InvolvedTable("Table2",
					                  new RowReference[]
					                  {
						                  new OIDRowReference(200),
						                  new OIDRowReference(100)
					                  }),
					new InvolvedTable("Table1",
					                  new RowReference[]
					                  {
						                  new OIDRowReference(1000)
					                  })
				};

			ITable tableMock = ExceptionObjectTestUtils.GetMockTable();

			ExceptionObject exceptionObject = ExceptionObjectTestUtils.CreateExceptionObject(
				1, involvedTables);
			QaError qaError = ExceptionObjectTestUtils.CreateQaError(tableMock, involvedRows);

			var predicate = new ExceptionObjectInvolvedRowsPredicate(null);
			Assert.True(predicate.Matches(exceptionObject, qaError));
		}

		[Test]
		public void CaDetectDistinct()
		{
			var involvedRows = new List<InvolvedRow>
			                   {
				                   new InvolvedRow("Table2", 100),
				                   new InvolvedRow("Table1", 1000),
				                   new InvolvedRow("Table2", 200)
			                   };

			var involvedTables =
				new List<InvolvedTable>
				{
					new InvolvedTable("Table2", new RowReference[] {new OIDRowReference(100)}),
					new InvolvedTable("Table1", new RowReference[] {new OIDRowReference(1000)})
				};

			ITable tableMock = ExceptionObjectTestUtils.GetMockTable();

			ExceptionObject exceptionObject = ExceptionObjectTestUtils.CreateExceptionObject(
				1, involvedTables);
			QaError qaError = ExceptionObjectTestUtils.CreateQaError(tableMock, involvedRows);

			var predicate = new ExceptionObjectInvolvedRowsPredicate(null);
			Assert.False(predicate.Matches(exceptionObject, qaError));
		}
	}
}
