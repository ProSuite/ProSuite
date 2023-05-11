using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase.TableBased;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.QA.Container;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.DomainServices.AO.Test.QA.Exceptions
{
	[TestFixture]
	public class ExceptionObjectInvolvedRowsIndexTest
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
		public void CanSearch()
		{
			var involvedRows1 = new List<InvolvedRow>
			                    {
				                    new InvolvedRow("Table2", 100),
				                    new InvolvedRow("Table1", 1000),
				                    new InvolvedRow("Table2", 200)
			                    };

			var involvedTables1 =
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

			var involvedRows2 = new List<InvolvedRow>
			                    {
				                    new InvolvedRow("Table1", 1000),
				                    new InvolvedRow("Table2", 100)
			                    };

			var involvedTables2 =
				new List<InvolvedTable>
				{
					new InvolvedTable("Table2",
					                  new RowReference[]
					                  {
						                  new OIDRowReference(100)
					                  }),
					new InvolvedTable("Table1",
					                  new RowReference[]
					                  {
						                  new OIDRowReference(1000)
					                  })
				};

			var involvedRows3 = new List<InvolvedRow>
			                    {
				                    new InvolvedRow("Table3", 1000)
			                    };

			var exceptionObjects =
				new List<ExceptionObject>
				{
					ExceptionObjectTestUtils.CreateExceptionObject(1, involvedTables2),
					ExceptionObjectTestUtils.CreateExceptionObject(2, involvedTables2),
					ExceptionObjectTestUtils.CreateExceptionObject(3, involvedTables1),
					ExceptionObjectTestUtils.CreateExceptionObject(4, involvedTables1),
					ExceptionObjectTestUtils.CreateExceptionObject(5, involvedTables1)
				};

			var index = new ExceptionObjectInvolvedRowsIndex(null);

			foreach (ExceptionObject exceptionObject in exceptionObjects)
			{
				index.Add(exceptionObject);
			}

			ITable table = ExceptionObjectTestUtils.GetMockTable();
			QaError qaError1 = ExceptionObjectTestUtils.CreateQaError(table, involvedRows1);
			QaError qaError2 = ExceptionObjectTestUtils.CreateQaError(table, involvedRows2);
			QaError qaError3 = ExceptionObjectTestUtils.CreateQaError(table, involvedRows3);

			Assert.AreEqual(3, index.Search(qaError1).Count());
			Assert.AreEqual(2, index.Search(qaError2).Count());
			Assert.AreEqual(0, index.Search(qaError3).Count());
		}
	}
}
