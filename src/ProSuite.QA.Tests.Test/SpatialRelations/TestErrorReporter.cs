using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.TestRunners;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test.SpatialRelations
{
	[CLSCompliant(false)]
	public class TestErrorReporter : IErrorReporting
	{
		[NotNull]
		[PublicAPI]
		public List<QaError> Errors { get; } = new List<QaError>();

		public void Reset()
		{
			Errors.Clear();
		}

		public int Report(string description, params IRow[] rows)
		{
			return ReportCore(description, null, null, null,
			                  false,
			                  null, rows);
		}

		public int Report(string description, IssueCode issueCode,
		                  string affectedComponent,
		                  params IRow[] rows)
		{
			return ReportCore(description, null, issueCode, affectedComponent,
			                  false,
			                  null, rows);
		}

		public int Report(string description, IGeometry errorGeometry, params IRow[] rows)
		{
			return ReportCore(description, errorGeometry, null, null,
			                  false,
			                  null, rows);
		}

		public int Report(string description, IGeometry errorGeometry,
		                  IssueCode issueCode,
		                  string affectedComponent, params IRow[] rows)
		{
			return ReportCore(description, errorGeometry, issueCode, null,
			                  false,
			                  null, rows);
		}

		public int Report(string description, IGeometry errorGeometry,
		                  IssueCode issueCode,
		                  bool reportIndividualParts, params IRow[] rows)
		{
			return ReportCore(description, errorGeometry, issueCode, null,
			                  reportIndividualParts,
			                  null, rows);
		}

		public int Report(string description, IGeometry errorGeometry,
		                  IssueCode issueCode,
		                  string affectedComponent, IEnumerable<object> values,
		                  params IRow[] rows)
		{
			return ReportCore(description, errorGeometry, issueCode, affectedComponent,
			                  false,
			                  values, rows);
		}

		public int Report(string description, IGeometry errorGeometry,
		                  IssueCode issueCode,
		                  string affectedComponent, bool reportIndividualParts,
		                  params IRow[] rows)
		{
			return ReportCore(description, errorGeometry, issueCode, affectedComponent,
			                  reportIndividualParts,
			                  null, rows);
		}

		private int ReportCore([NotNull] string description,
		                       [CanBeNull] IGeometry errorGeometry,
		                       [CanBeNull] IssueCode issueCode,
		                       [CanBeNull] string affectedComponent,
		                       bool reportIndividualParts,
		                       [CanBeNull] IEnumerable<object> values,
		                       [NotNull] params IRow[] rows)
		{
			var errors = CreateErrors(description, errorGeometry, issueCode,
			                          affectedComponent, reportIndividualParts, values,
			                          rows);

			errors.ForEach(TestRunnerUtils.PrintError);

			Errors.AddRange(errors);

			return errors.Count;
		}

		[NotNull]
		private static List<QaError> CreateErrors([NotNull] string description,
		                                          [CanBeNull] IGeometry errorGeometry,
		                                          [CanBeNull] IssueCode issueCode,
		                                          [CanBeNull] string affectedComponent,
		                                          bool reportIndividualParts,
		                                          [CanBeNull] IEnumerable<object> values,
		                                          [NotNull] IRow[] rows)
		{
			return reportIndividualParts && errorGeometry != null
				       ? GeometryUtils.Explode(errorGeometry)
				                      .Select(g => new QaError(
					                              new DummyTest(), description,
					                              InvolvedRowUtils.GetInvolvedRows(rows),
					                              g, issueCode,
					                              affectedComponent, false, values))
				                      .ToList()
				       : new List<QaError>
				         {
					         new QaError(new DummyTest(), description,
					                     InvolvedRowUtils.GetInvolvedRows(rows),
					                     errorGeometry, issueCode,
					                     affectedComponent, false, values)
				         };
		}

		private class DummyTest : TestBase
		{
			public DummyTest() : base(new ITable[] { }) { }

			public override int Execute()
			{
				throw new NotImplementedException();
			}

			public override int Execute(IEnvelope boundingBox)
			{
				throw new NotImplementedException();
			}

			public override int Execute(IPolygon area)
			{
				throw new NotImplementedException();
			}

			public override int Execute(IEnumerable<IRow> selectedRows)
			{
				throw new NotImplementedException();
			}

			public override int Execute(IRow row)
			{
				throw new NotImplementedException();
			}

			protected override ISpatialReference GetSpatialReference()
			{
				throw new NotImplementedException();
			}
		}
	}
}
