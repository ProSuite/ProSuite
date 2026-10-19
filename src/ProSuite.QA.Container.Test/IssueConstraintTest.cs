using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.QA.Container.Test
{
	[TestFixture]
	public class IssueConstraintTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			Commons.AO.Test.TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			Commons.AO.Test.TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanFilterByIssueCode()
		{
			QaError error = CreateQaError(issueCode: "Constraints.ConstraintNotFulfilled");

			AssertFulfilled("$IssueCode = 'Constraints.ConstraintNotFulfilled'", error);
			AssertNotFulfilled("$IssueCode = 'SomeOtherCode'", error);

			// column names and values are compared case-insensitively
			AssertFulfilled("$issuecode = 'constraints.constraintnotfulfilled'", error);
		}

		[Test]
		public void CanFilterByIssueCodePrefix()
		{
			QaError parent = CreateQaError(issueCode: "Foo");
			QaError child = CreateQaError(issueCode: "Foo.Bar");
			QaError unrelated = CreateQaError(issueCode: "FooBar");

			const string constraint = "$IssueCode = 'Foo' OR $IssueCode LIKE 'Foo.%'";

			AssertFulfilled(constraint, parent);
			AssertFulfilled(constraint, child);
			AssertNotFulfilled(constraint, unrelated);
		}

		[Test]
		public void CanHandleMissingIssueCode()
		{
			QaError error = CreateQaError();

			AssertFulfilled("$IssueCode IS NULL", error);
			AssertNotFulfilled("$IssueCode = 'AnyCode'", error);

			// a missing issue code behaves like NULL: <> is NOT fulfilled
			AssertNotFulfilled("$IssueCode <> 'AnyCode'", error);
			AssertFulfilled("$IssueCode IS NULL OR $IssueCode <> 'AnyCode'", error);
		}

		[Test]
		public void CanFilterByDescription()
		{
			QaError error = CreateQaError("Dachkörper is not covered");

			AssertFulfilled("$Description LIKE '%Dachkörper%'", error);
			AssertFulfilled("$Description = 'Dachkörper is not covered'", error);
			AssertNotFulfilled("$Description LIKE '%Fassade%'", error);
		}

		[Test]
		public void CanFilterByAffectedComponent()
		{
			QaError error = CreateQaError(affectedComponent: "HOEHE");

			AssertFulfilled("$AffectedComponent = 'HOEHE'", error);
			AssertNotFulfilled("$AffectedComponent = 'BREITE'", error);

			QaError noComponent = CreateQaError();
			AssertFulfilled("$AffectedComponent IS NULL", noComponent);
		}

		[Test]
		public void CanFilterByValues()
		{
			QaError error = CreateQaError(values: new object[] { 1.5, "abc", 2.5 });

			AssertFulfilled("$Value1 = 1.5", error);
			AssertFulfilled("$Value2 = 2.5", error);
			AssertFulfilled("$TextValue = 'abc'", error);
			AssertFulfilled("$Value1 < 2 AND $TextValue IN ('abc', 'def')", error);
			AssertNotFulfilled("$Value1 > 2", error);

			QaError noValues = CreateQaError();
			AssertFulfilled("$Value1 IS NULL AND $Value2 IS NULL AND $TextValue IS NULL",
			                noValues);
		}

		[Test]
		public void CanFilterByGeometryProperties()
		{
			IGeometry polygon = CreatePolygon(0, 0, 10, 10);

			QaError error = CreateQaError(geometry: polygon);

			AssertFulfilled("$Area = 100", error);
			AssertFulfilled("$Length = 40", error);
			AssertFulfilled("$VertexCount = 5", error);
			AssertFulfilled("$XMIN = 0 AND $YMAX = 10", error);
			AssertNotFulfilled("$Area > 1000", error);
		}

		[Test]
		public void CanCombineIssueAndGeometryColumns()
		{
			IGeometry small = CreatePolygon(0, 0, 1, 1);
			IGeometry large = CreatePolygon(0, 0, 100, 100);

			const string constraint = "$IssueCode = 'PartlyCovered' AND $Area < 10";

			AssertFulfilled(constraint, CreateQaError(issueCode: "PartlyCovered",
			                                          geometry: small));
			AssertNotFulfilled(constraint, CreateQaError(issueCode: "PartlyCovered",
			                                             geometry: large));
			AssertNotFulfilled(constraint, CreateQaError(issueCode: "OtherCode",
			                                             geometry: small));
		}

		[Test]
		public void CanHandleMissingGeometry()
		{
			QaError error = CreateQaError();

			// same as in GeometryConstraint: the envelope-based columns are NULL,
			// $Area and $Length are 0
			AssertFulfilled("$XMIN IS NULL", error);
			AssertFulfilled("$Area = 0", error);
			AssertFulfilled("$Length = 0", error);
			AssertNotFulfilled("$Area > 0", error);
		}

		#region Test utils

		private static void AssertFulfilled([NotNull] string constraint,
		                                    [NotNull] QaError qaError)
		{
			Assert.IsTrue(new IssueConstraint(constraint).IsFulfilled(qaError),
			              "constraint not fulfilled: {0}", constraint);
		}

		private static void AssertNotFulfilled([NotNull] string constraint,
		                                       [NotNull] QaError qaError)
		{
			Assert.IsFalse(new IssueConstraint(constraint).IsFulfilled(qaError),
			               "constraint unexpectedly fulfilled: {0}", constraint);
		}

		[NotNull]
		private static QaError CreateQaError(
			[NotNull] string description = "error description",
			[CanBeNull] string issueCode = null,
			[CanBeNull] string affectedComponent = null,
			[CanBeNull] IGeometry geometry = null,
			[CanBeNull] IEnumerable<object> values = null)
		{
			return new QaError(CreateDummyTest(), description,
			                   new List<InvolvedRow>(), geometry,
			                   issueCode == null ? null : new IssueCode(issueCode),
			                   affectedComponent,
			                   values: values);
		}

		[NotNull]
		private static ITest CreateDummyTest()
		{
			var workspaceMock = new WorkspaceMock();
			var objectClassMock = new ObjectClassMock(1, "table");
			workspaceMock.AddDataset(objectClassMock);

			return new DummyTest(ReadOnlyTableFactory.Create((ITable) objectClassMock));
		}

		[NotNull]
		private static IPolygon CreatePolygon(double xMin, double yMin,
		                                      double xMax, double yMax)
		{
			IPolygon polygon = GeometryFactory.CreatePolygon(
				GeometryFactory.CreateEnvelope(xMin, yMin, xMax, yMax));
			polygon.SpatialReference = SpatialReferenceUtils.CreateSpatialReference(
				(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);

			return polygon;
		}

		private class DummyTest : ContainerTest
		{
			public DummyTest([NotNull] IReadOnlyTable table) : base(table) { }

			protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
			{
				return 0;
			}
		}

		#endregion
	}
}
