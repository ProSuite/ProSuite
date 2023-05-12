using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.QA.Container
{
	public abstract class TestBase : ProcessBase, ITest, IErrorReporting
	{
		protected static int NoError => 0;

		public virtual event EventHandler<RowEventArgs> TestingRow;

		protected event EventHandler<QaErrorEventArgs> PostProcessError;
		public virtual event EventHandler<QaErrorEventArgs> QaError;

		protected TestBase([NotNull] IEnumerable<IReadOnlyTable> tables)
			: base(tables) { }

		public abstract int Execute();

		public abstract int Execute(IEnvelope boundingBox);

		public abstract int Execute(IPolygon area);

		public abstract int Execute(IEnumerable<IReadOnlyRow> selectedRows);

		public abstract int Execute(IReadOnlyRow row);

		protected virtual void OnQaError([NotNull] QaErrorEventArgs args)
		{
			QaError?.Invoke(this, args);
		}

		protected bool CancelTestingRow([NotNull] IReadOnlyRow row, Guid? recycleUnique = null,
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
		                           InvolvedRows rows,
		                           IGeometry errorGeometry,
		                           IssueCode issueCode,
		                           string affectedComponent,
		                           bool reportIndividualParts,
		                           IEnumerable<object> values)
		{
			return ReportError(description, rows, errorGeometry, issueCode, affectedComponent,
			                   reportIndividualParts, values);
		}

		protected int ReportError([NotNull] string description,
		                          [NotNull] InvolvedRows rows,
		                          [CanBeNull] IGeometry errorGeometry,
		                          [CanBeNull] IssueCode issueCode,
		                          [CanBeNull] string affectedComponent,
		                          bool reportIndividualParts = false,
		                          [CanBeNull] IEnumerable<object> values = null)
		{
			ICollection<object> valueCollection =
				values == null
					? null
					: CollectionUtils.GetCollection(values);

			if (! reportIndividualParts || errorGeometry == null || errorGeometry.IsEmpty)
			{
				return ReportError(description, rows, errorGeometry,
				                   issueCode, affectedComponent,
				                   valueCollection);
			}

			var errorCount = 0;

			foreach (IGeometry part in GeometryUtils.Explode(errorGeometry))
			{
				if (! part.IsEmpty)
				{
					errorCount += ReportError(description, rows, part,
					                          issueCode, affectedComponent, valueCollection);
				}
			}

			return errorCount;
		}

		private int ReportError([NotNull] string description,
		                        [NotNull] InvolvedRows involvedRows,
		                        [CanBeNull] IGeometry errorGeometry,
		                        [CanBeNull] IssueCode issueCode,
		                        [CanBeNull] string affectedComponent,
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
		                          [NotNull] IReadOnlyTable table,
		                          [CanBeNull] IssueCode issueCode,
		                          [CanBeNull] string affectedComponent,
		                          [CanBeNull] IEnumerable<object> values = null)
		{
			var involvedRows = new List<InvolvedRow> { CreateInvolvedRowForTable(table) };

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
		private static InvolvedRow CreateInvolvedRowForTable([NotNull] IReadOnlyTable table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			return new InvolvedRow(table.Name);
		}
	}
}
