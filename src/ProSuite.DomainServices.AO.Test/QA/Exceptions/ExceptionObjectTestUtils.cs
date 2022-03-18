using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.Test.QA.Exceptions
{
	internal static class ExceptionObjectTestUtils
	{
		[NotNull]
		public static QaError CreateQaError([NotNull] ITable tableMock,
		                                    [CanBeNull] string issueCode,
		                                    [CanBeNull] string affectedComponent,
		                                    [CanBeNull] IEnumerable<object> values = null)
		{
			return new QaError(new DummyTest(ReadOnlyTableFactory.Create(tableMock)), "error description",
			                   new List<InvolvedRow>(), null,
			                   issueCode == null
				                   ? null
				                   : new IssueCode(issueCode),
			                   affectedComponent,
			                   values: values);
		}

		[NotNull]
		public static QaError CreateQaError([NotNull] ITable tableMock,
		                                    [NotNull] IEnumerable<InvolvedRow> involvedRows)
		{
			return new QaError(new DummyTest(ReadOnlyTableFactory.Create(tableMock)), "error description",
			                   involvedRows, null, null, null);
		}

		[NotNull]
		public static ITable GetMockTable()
		{
			var workspaceMock = new WorkspaceMock();
			var result = new ObjectClassMock(1, "table");
			workspaceMock.AddDataset(result);
			return result;
		}

		[NotNull]
		public static ExceptionObject CreateExceptionObject(
			int id, [NotNull] IList<InvolvedTable> involvedTables)
		{
			return new ExceptionObject(id, new Guid(), new Guid(),
			                           null, null, null,
			                           ShapeMatchCriterion.EqualEnvelope,
			                           "Issue.Code", null,
			                           involvedTables);
		}

		private class DummyTest : ContainerTest
		{
			public DummyTest([NotNull] IReadOnlyTable table) : base(table) { }

			protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
			{
				return 0;
			}
		}
	}
}
