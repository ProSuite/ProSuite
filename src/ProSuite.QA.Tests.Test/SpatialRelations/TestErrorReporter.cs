using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test.SpatialRelations
{
	public class TestErrorReporter : IErrorReporting
	{
		[NotNull]
		[PublicAPI]
		public List<QaError> Errors { get; } = new List<QaError>();

		public void Reset()
		{
			Errors.Clear();
		}

		public int Report(string description, InvolvedRows involved,
		                  IGeometry errorGeometry,
		                  IssueCode issueCode,
		                  string affectedComponent,
		                  bool reportIndividualParts = false,
		                  IEnumerable<object> values = null)
		{
			var errors = CreateErrors(description, involved, errorGeometry, issueCode,
			                          affectedComponent, reportIndividualParts, values);

			errors.ForEach(TestRunnerUtils.PrintError);

			Errors.AddRange(errors);

			return errors.Count;
		}

		[NotNull]
		private static List<QaError> CreateErrors([NotNull] string description,
		                                          [NotNull] InvolvedRows involved,
		                                          [CanBeNull] IGeometry errorGeometry,
		                                          [CanBeNull] IssueCode issueCode,
		                                          [CanBeNull] string affectedComponent,
		                                          bool reportIndividualParts,
		                                          [CanBeNull] IEnumerable<object> values)
		{
			return reportIndividualParts && errorGeometry != null
				       ? GeometryUtils.Explode(errorGeometry)
				                      .Select(g => new QaError(
					                              new DummyTest(), description,
					                              involved,
					                              g, issueCode,
					                              affectedComponent, false, values))
				                      .ToList()
				       : new List<QaError>
				         {
					         new QaError(new DummyTest(), description,
					                     involved,
					                     errorGeometry, issueCode,
					                     affectedComponent, false, values)
				         };
		}

		private class DummyTest : TestBase
		{
			public DummyTest() : base(new IReadOnlyTable[] { }) { }

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

			public override int Execute(IEnumerable<IReadOnlyRow> selectedRows)
			{
				throw new NotImplementedException();
			}

			public override int Execute(IReadOnlyRow row)
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
