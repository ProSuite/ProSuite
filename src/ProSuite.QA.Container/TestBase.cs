using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public abstract class TestBase : ProcessBase, ITest, IErrorReporting
	{
		protected static int NoError => 0;

		public virtual event EventHandler<RowEventArgs> TestingRow;

		protected event EventHandler<QaErrorEventArgs> PostProcessError;
		public virtual event EventHandler<QaErrorEventArgs> QaError;

		protected TestBase([NotNull] IEnumerable<ITable> tables)
			: base(tables) { }

		public abstract int Execute();

		public abstract int Execute(IEnvelope boundingBox);

		public abstract int Execute(IPolygon area);

		public abstract int Execute(IEnumerable<IRow> selectedRows);

		public abstract int Execute(IRow row);

		protected virtual void OnQaError([NotNull] QaErrorEventArgs args)
		{
			QaError?.Invoke(this, args);
		}

		int IErrorReporting.Report(string description,
		                           params IRow[] rows)
		{
			return ReportError(description, null, null, null, rows);
		}

		protected bool CancelTestingRow([NotNull] IRow row, Guid? recycleUnique = null,
		                                bool ignoreTestArea = false)
		{
			EventHandler<RowEventArgs> handler = TestingRow;

			if (handler == null)
			{
				return false;
			}

			RowEventArgs rowEventArgs = ! recycleUnique.HasValue
				                            ? new RowEventArgs(row)
				                            : new RowEventArgs(row, recycleUnique.Value);

			rowEventArgs.IgnoreTestArea = ignoreTestArea;
			handler(this, rowEventArgs);

			return rowEventArgs.Cancel;
		}

		int IErrorReporting.Report(string description,
		                           IssueCode issueCode,
		                           string affectedComponent,
		                           params IRow[] rows)
		{
			const IGeometry errorGeometry = null;
			const bool reportIndividualParts = false;

			return ReportError(description, errorGeometry,
			                   issueCode, affectedComponent,
			                   reportIndividualParts,
			                   rows);
		}

		int IErrorReporting.Report(string description,
		                           IGeometry errorGeometry,
		                           params IRow[] rows)
		{
			return ReportError(description, errorGeometry, null, null, rows);
		}

		int IErrorReporting.Report(string description,
		                           IGeometry errorGeometry,
		                           IssueCode issueCode,
		                           string affectedComponent,
		                           params IRow[] rows)
		{
			const bool reportIndividualParts = false;

			return ReportError(description, errorGeometry,
			                   issueCode, affectedComponent,
			                   reportIndividualParts, rows);
		}

		int IErrorReporting.Report(string description,
		                           IGeometry errorGeometry,
		                           IssueCode issueCode,
		                           bool reportIndividualParts,
		                           params IRow[] rows)
		{
			return ReportError(description, errorGeometry,
			                   issueCode, null,
			                   reportIndividualParts, rows);
		}

		public int Report(string description, IGeometry errorGeometry, IssueCode issueCode,
		                  string affectedComponent, IEnumerable<object> values,
		                  params IRow[] rows)
		{
			return ReportError(description, errorGeometry,
			                   issueCode, affectedComponent, values,
			                   rows);
		}

		int IErrorReporting.Report(string description,
		                           IGeometry errorGeometry,
		                           IssueCode issueCode,
		                           string affectedComponent,
		                           bool reportIndividualParts,
		                           params IRow[] rows)
		{
			return ReportError(description, errorGeometry,
			                   issueCode, affectedComponent,
			                   reportIndividualParts, rows);
		}

		protected int ReportError([NotNull] string description,
		                          [CanBeNull] IGeometry errorGeometry,
		                          [CanBeNull] IssueCode issueCode,
		                          [CanBeNull] string affectedComponent,
		                          bool reportIndividualParts,
		                          params IRow[] rows)
		{
			return ReportError(description, errorGeometry,
			                   issueCode, affectedComponent, null,
			                   reportIndividualParts, rows);
		}

		protected int ReportError([NotNull] string description,
		                          [CanBeNull] IGeometry errorGeometry,
		                          [CanBeNull] IssueCode issueCode,
		                          [CanBeNull] string affectedComponent,
		                          [CanBeNull] IEnumerable<object> values,
		                          bool reportIndividualParts,
		                          params IRow[] rows)
		{
			ICollection<object> valueCollection =
				values == null
					? null
					: CollectionUtils.GetCollection(values);

			if (! reportIndividualParts || errorGeometry == null || errorGeometry.IsEmpty)
			{
				return ReportError(description, errorGeometry,
				                   issueCode, affectedComponent, valueCollection,
				                   rows);
			}

			var errorCount = 0;

			foreach (IGeometry part in GeometryUtils.Explode(errorGeometry))
			{
				if (! part.IsEmpty)
				{
					errorCount += ReportError(description, part,
					                          issueCode, affectedComponent, valueCollection,
					                          rows);
				}
			}

			return errorCount;
		}

		protected int ReportError([NotNull] string description,
		                          [CanBeNull] IssueCode issueCode,
		                          [CanBeNull] string affectedComponent,
		                          [NotNull] IRow row)
		{
			return ReportError(description, TestUtils.GetShapeCopy(row),
			                   issueCode, affectedComponent, row);
		}

		[Obsolete("call overload with issueCode and affectedComponent")]
		protected int ReportError([NotNull] string description,
		                          [CanBeNull] IGeometry errorGeometry,
		                          params IRow[] rows)
		{
			return ReportError(description, errorGeometry, null, null, rows);
		}

		protected int ReportError([NotNull] string description,
		                          [CanBeNull] IGeometry errorGeometry,
		                          [CanBeNull] IssueCode issueCode,
		                          [CanBeNull] string affectedComponent,
		                          params IRow[] rows)
		{
			return ReportError(description, errorGeometry,
			                   issueCode, affectedComponent,
			                   InvolvedRowUtils.GetInvolvedRows(rows));
		}

		protected int ReportError([NotNull] string description,
		                          [CanBeNull] IGeometry errorGeometry,
		                          [CanBeNull] IssueCode issueCode,
		                          [CanBeNull] string affectedComponent,
		                          [CanBeNull] IEnumerable<object> values,
		                          params IRow[] rows)
		{
			return ReportError(description, errorGeometry,
			                   issueCode, affectedComponent,
			                   InvolvedRowUtils.GetInvolvedRows(rows),
			                   values);
		}

		[Obsolete("call overload with issueCode and affectedComponent")]
		protected int ReportError([NotNull] string description,
		                          [CanBeNull] IGeometry errorGeometry,
		                          [NotNull] IEnumerable<InvolvedRow> involvedRows)
		{
			return ReportError(description, errorGeometry, null, null, involvedRows);
		}

		protected int ReportError([NotNull] string description,
		                          [CanBeNull] IGeometry errorGeometry,
		                          [CanBeNull] IssueCode issueCode,
		                          [CanBeNull] string affectedComponent,
		                          [NotNull] InvolvedRows involvedRows,
		                          [CanBeNull] IEnumerable<object> values = null)
		{
			var args = new QaErrorEventArgs(new QaError(this, description, involvedRows,
			                                            errorGeometry,
			                                            issueCode, affectedComponent,
			                                            values: values), involvedRows.TestedRows);
			PostProcessError?.Invoke(this, args);
			if (args.Cancel)
			{
				return 0;
			}

			OnQaError(args);
			if (args.Cancel)
			{
				return 0;
			}

			return 1;
		}

		protected int ReportError([NotNull] string description,
		                          [NotNull] ITable table,
		                          [CanBeNull] IssueCode issueCode,
		                          [CanBeNull] string affectedComponent,
		                          [CanBeNull] IEnumerable<object> values = null)
		{
			var involvedRows = new List<InvolvedRow> {CreateInvolvedRowForTable(table)};

			const IGeometry geometry = null;
			var qaError = new QaError(this, description, involvedRows, geometry,
			                          issueCode, affectedComponent,
			                          values: values);

			var args = new QaErrorEventArgs(qaError);
			PostProcessError?.Invoke(this, args);
			if (args.Cancel)
			{
				return 0;
			}

			OnQaError(args);

			return 1;
		}

		[NotNull]
		private static InvolvedRow CreateInvolvedRowForTable([NotNull] ITable table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			return new InvolvedRow(DatasetUtils.GetName(table));
		}
	}
}
