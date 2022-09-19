using System.Globalization;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaValidCoordinateFieldsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		private const string _fieldNameX = "X";
		private const string _fieldNameY = "Y";
		private const string _fieldNameZ = "Z";

		private const double _xyTolerance = 0.01;
		private const double _zTolerance = 0.01;

		private const string _culture = "de-DE";
		private readonly CultureInfo _cultureInfo = CultureInfo.GetCultureInfo(_culture);

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void TestEqualCoordinatesDoubleFields()
		{
			var featureClass = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryPoint);

			featureClass.AddField(FieldUtils.CreateDoubleField(_fieldNameX));
			featureClass.AddField(FieldUtils.CreateDoubleField(_fieldNameY));
			featureClass.AddField(FieldUtils.CreateDoubleField(_fieldNameZ));

			// create point feature
			const double x = 2600000;
			const double y = 1200000;
			const double z = 500;

			IFeature feature = CreateFeature(featureClass,
			                                 x, x,
			                                 y, y,
			                                 z, z);

			var test = new QaValidCoordinateFields(
				ReadOnlyTableFactory.Create(featureClass),
				_fieldNameX, _fieldNameY, _fieldNameZ,
				_xyTolerance, _zTolerance, _culture);

			var runner = new QaTestRunner(test);

			runner.Execute(feature);

			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		public void TestNonEqualXCoordinateDoubleField()
		{
			var featureClass = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryPoint);

			featureClass.AddField(FieldUtils.CreateDoubleField(_fieldNameX));

			// create point feature
			const double x = 2600000;
			const double y = 1200000;
			const double z = 500;

			IFeature feature = CreateFeature(featureClass,
			                                 x, x + 1.01 * _xyTolerance,
			                                 y, null,
			                                 z, null);

			var test = new QaValidCoordinateFields(
				ReadOnlyTableFactory.Create(featureClass),
				_fieldNameX, null, null,
				_xyTolerance, _zTolerance, _culture);

			var runner = new QaTestRunner(test);

			runner.Execute(feature);

			QaError error;
			AssertUtils.OneError(runner,
			                     "ValidCoordinateFields.XYFieldCoordinateValueTooFarFromShape",
			                     out error);
			Assert.AreEqual(_fieldNameX, error.AffectedComponent);
		}

		[Test]
		public void TestNonEqualYCoordinateDoubleField()
		{
			var featureClass = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryPoint);

			featureClass.AddField(FieldUtils.CreateDoubleField(_fieldNameY));

			// create point feature
			const double x = 2600000;
			const double y = 1200000;
			const double z = 500;

			IFeature feature = CreateFeature(featureClass,
			                                 x, null,
			                                 y, y + 1.01 * _xyTolerance,
			                                 z, null);

			var test = new QaValidCoordinateFields(
				ReadOnlyTableFactory.Create(featureClass),
				null, _fieldNameY, null,
				_xyTolerance, _zTolerance, _culture);

			var runner = new QaTestRunner(test);

			runner.Execute(feature);

			QaError error;
			AssertUtils.OneError(runner,
			                     "ValidCoordinateFields.XYFieldCoordinateValueTooFarFromShape",
			                     out error);
			Assert.AreEqual(_fieldNameY, error.AffectedComponent);
		}

		[Test]
		public void TestNonEqualZCoordinateDoubleField()
		{
			var featureClass = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryPoint);

			featureClass.AddField(FieldUtils.CreateDoubleField(_fieldNameZ));

			// create point feature
			const double x = 2600000;
			const double y = 1200000;
			const double z = 500;

			IFeature feature = CreateFeature(featureClass,
			                                 x, null,
			                                 y, null,
			                                 z, z + 1.01 * _zTolerance);

			var test = new QaValidCoordinateFields(
				ReadOnlyTableFactory.Create(featureClass),
				null, null, _fieldNameZ,
				_xyTolerance, _zTolerance, _culture);

			var runner = new QaTestRunner(test);

			runner.Execute(feature);

			QaError error;
			AssertUtils.OneError(runner,
			                     "ValidCoordinateFields.ZFieldCoordinateTooFarFromShape",
			                     out error);
		}

		[Test]
		public void TestEqualCoordinatesTextFields()
		{
			var featureClass = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryPoint);

			featureClass.AddField(FieldUtils.CreateTextField(_fieldNameX, 50));
			featureClass.AddField(FieldUtils.CreateTextField(_fieldNameY, 50));
			featureClass.AddField(FieldUtils.CreateTextField(_fieldNameZ, 50));

			// create point feature
			const double x = 2600000.12345;
			const double y = 1200000.12345;
			const double z = 500.12345;

			IFeature feature = CreateFeature(
				featureClass,
				x, string.Format(_cultureInfo, "{0:N3}", x),
				y, string.Format(_cultureInfo, "{0:N3}", y),
				z, string.Format(_cultureInfo, "{0:N3}", z));

			var test = new QaValidCoordinateFields(
				ReadOnlyTableFactory.Create(featureClass),
				_fieldNameX, _fieldNameY, _fieldNameZ,
				_xyTolerance, _zTolerance, _culture);

			var runner = new QaTestRunner(test);

			runner.Execute(feature);

			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		public void TestNonEqualCoordinatesTextFieldsEqualY()
		{
			var featureClass = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryPoint);

			featureClass.AddField(FieldUtils.CreateTextField(_fieldNameX, 50));
			featureClass.AddField(FieldUtils.CreateTextField(_fieldNameY, 50));
			featureClass.AddField(FieldUtils.CreateTextField(_fieldNameZ, 50));

			// create point feature
			const double x = 2600000.12345;
			const double y = 1200000.12345;
			const double z = 500.12345;

			IFeature feature = CreateFeature(
				featureClass,
				x, string.Format(_cultureInfo, "{0:N3}", x + 1.01 * _xyTolerance),
				y, string.Format(_cultureInfo, "{0:N3}", y),
				z, string.Format(_cultureInfo, "{0:N3}", z + 1.01 * _zTolerance));

			var test = new QaValidCoordinateFields(
				ReadOnlyTableFactory.Create(featureClass),
				_fieldNameX, _fieldNameY, _fieldNameZ,
				_xyTolerance, _zTolerance, _culture);

			var runner = new QaTestRunner(test);

			runner.Execute(feature);

			Assert.AreEqual(2, runner.Errors.Count);

			QaError xyError = runner.Errors[0];

			IssueCode xyIssueCode = xyError.IssueCode;
			Assert.IsNotNull(xyIssueCode);
			Assert.AreEqual("ValidCoordinateFields.XYFieldCoordinateValueTooFarFromShape",
			                xyIssueCode.ID);

			QaError zError = runner.Errors[1];

			IssueCode zIssueCode = zError.IssueCode;
			Assert.IsNotNull(zIssueCode);
			Assert.AreEqual("ValidCoordinateFields.ZFieldCoordinateTooFarFromShape",
			                zIssueCode.ID);
		}

		[Test]
		public void TestNonEqualCoordinatesTextFieldsBothXYAboveTolerance()
		{
			var featureClass = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryPoint);

			featureClass.AddField(FieldUtils.CreateTextField(_fieldNameX, 50));
			featureClass.AddField(FieldUtils.CreateTextField(_fieldNameY, 50));
			featureClass.AddField(FieldUtils.CreateTextField(_fieldNameZ, 50));

			// create point feature
			const double x = 2600000.12345;
			const double y = 1200000.12345;

			IFeature feature = CreateFeature(
				featureClass,
				x, string.Format(_cultureInfo, "{0:N3}", x + 1.01 * _xyTolerance),
				y, string.Format(_cultureInfo, "{0:N3}", y - 1.01 * _xyTolerance),
				z: 100, zFieldValue: "100");

			var test = new QaValidCoordinateFields(
				ReadOnlyTableFactory.Create(featureClass),
				_fieldNameX, _fieldNameY, _fieldNameZ,
				_xyTolerance, _zTolerance, _culture);

			var runner = new QaTestRunner(test);

			runner.Execute(feature);

			AssertUtils.OneError(runner,
			                     "ValidCoordinateFields.XYFieldCoordinatesTooFarFromShape");
		}

		[Test]
		public void TestNonEqualCoordinatesTextFieldsBothXYJustBelowTolerance()
		{
			var featureClass = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryPoint);

			featureClass.AddField(FieldUtils.CreateTextField(_fieldNameX, 50));
			featureClass.AddField(FieldUtils.CreateTextField(_fieldNameY, 50));
			featureClass.AddField(FieldUtils.CreateTextField(_fieldNameZ, 50));

			// create point feature
			const double x = 2600000.12345;
			const double y = 1200000.12345;

			IFeature feature = CreateFeature(
				featureClass,
				x, string.Format(_cultureInfo, "{0:N3}", x + 0.9 * _xyTolerance),
				y, string.Format(_cultureInfo, "{0:N3}", y - 0.9 * _xyTolerance),
				z: 100, zFieldValue: "100");

			var test = new QaValidCoordinateFields(
				ReadOnlyTableFactory.Create(featureClass),
				_fieldNameX, _fieldNameY, _fieldNameZ,
				_xyTolerance, _zTolerance, _culture);

			var runner = new QaTestRunner(test);

			runner.Execute(feature);

			AssertUtils.OneError(runner,
			                     "ValidCoordinateFields.XYFieldCoordinatesTooFarFromShape");
		}

		[Test]
		public void TestInvalidTextFieldValues()
		{
			var featureClass = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryPoint);

			featureClass.AddField(FieldUtils.CreateTextField(_fieldNameX, 50));
			featureClass.AddField(FieldUtils.CreateTextField(_fieldNameY, 50));
			featureClass.AddField(FieldUtils.CreateTextField(_fieldNameZ, 50));

			// create point feature
			const double x = 2600000.12345;
			const double y = 1200000.12345;
			const double z = 500.12345;

			IFeature feature = CreateFeature(featureClass,
			                                 x, "a",
			                                 y, "b",
			                                 z, "c");

			var test = new QaValidCoordinateFields(
				ReadOnlyTableFactory.Create(featureClass),
				_fieldNameX, _fieldNameY, _fieldNameZ,
				_xyTolerance, _zTolerance, _culture);

			var runner = new QaTestRunner(test);

			runner.Execute(feature);

			Assert.AreEqual(3, runner.Errors.Count);

			foreach (QaError error in runner.Errors)
			{
				IssueCode issueCode = error.IssueCode;
				Assert.IsNotNull(issueCode);
				Assert.AreEqual("ValidCoordinateFields.TextFieldValueIsNotNumeric",
				                issueCode.ID);
			}

			Assert.AreEqual(_fieldNameX, runner.Errors[0].AffectedComponent);
			Assert.AreEqual(_fieldNameY, runner.Errors[1].AffectedComponent);
			Assert.AreEqual(_fieldNameZ, runner.Errors[2].AffectedComponent);
		}

		[Test]
		public void TestCoordinatesForUndefinedShape()
		{
			var featureClass = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryPoint);

			featureClass.AddField(FieldUtils.CreateDoubleField(_fieldNameX));
			featureClass.AddField(FieldUtils.CreateDoubleField(_fieldNameY));
			featureClass.AddField(FieldUtils.CreateDoubleField(_fieldNameZ));

			// create point feature
			const double x = 2600000;
			const double y = 1200000;
			const double z = 500;

			IFeature feature = CreateFeature(featureClass,
			                                 x, x,
			                                 y, y,
			                                 z, z);
			feature.Shape.SetEmpty();

			var test = new QaValidCoordinateFields(
				ReadOnlyTableFactory.Create(featureClass),
				_fieldNameX, _fieldNameY, _fieldNameZ,
				_xyTolerance, _zTolerance, _culture);

			var runner = new QaTestRunner(test);

			runner.Execute(feature);

			Assert.AreEqual(3, runner.Errors.Count);

			foreach (QaError error in runner.Errors)
			{
				IssueCode issueCode = error.IssueCode;
				Assert.IsNotNull(issueCode);
				Assert.AreEqual(
					"ValidCoordinateFields.ShapeIsUndefinedButCoordinateFieldHasValue",
					issueCode.ID);
			}

			Assert.AreEqual(_fieldNameX, runner.Errors[0].AffectedComponent);
			Assert.AreEqual(_fieldNameY, runner.Errors[1].AffectedComponent);
			Assert.AreEqual(_fieldNameZ, runner.Errors[2].AffectedComponent);
		}

		[Test]
		public void TestMissingXYCoordinatesForDefinedShape()
		{
			var featureClass = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryPoint);

			featureClass.AddField(FieldUtils.CreateDoubleField(_fieldNameX));
			featureClass.AddField(FieldUtils.CreateDoubleField(_fieldNameY));
			featureClass.AddField(FieldUtils.CreateDoubleField(_fieldNameZ));

			// create point feature
			const double x = 2600000;
			const double y = 1200000;
			const double z = 500;

			IFeature feature = CreateFeature(featureClass,
			                                 x, null,
			                                 y, null,
			                                 z, z);

			var test = new QaValidCoordinateFields(
				           ReadOnlyTableFactory.Create(featureClass),
				           _fieldNameX, _fieldNameY, _fieldNameZ,
				           _xyTolerance, _zTolerance, _culture)
			           {
				           AllowMissingXYFieldValueForDefinedShape = false,
				           AllowMissingZFieldValueForDefinedShape = false
			           };

			var runner = new QaTestRunner(test);

			runner.Execute(feature);

			Assert.AreEqual(2, runner.Errors.Count);

			foreach (QaError error in runner.Errors)
			{
				IssueCode issueCode = error.IssueCode;
				Assert.IsNotNull(issueCode);
				Assert.AreEqual(
					"ValidCoordinateFields.ShapeIsDefinedButCoordinateFieldHasNoValue",
					issueCode.ID);
			}

			Assert.AreEqual(_fieldNameX, runner.Errors[0].AffectedComponent);
			Assert.AreEqual(_fieldNameY, runner.Errors[1].AffectedComponent);
		}

		[Test]
		public void TestMissingZCoordinateForDefinedShape()
		{
			var featureClass = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryPoint);

			featureClass.AddField(FieldUtils.CreateDoubleField(_fieldNameX));
			featureClass.AddField(FieldUtils.CreateDoubleField(_fieldNameY));
			featureClass.AddField(FieldUtils.CreateDoubleField(_fieldNameZ));

			// create point feature
			const double x = 2600000;
			const double y = 1200000;
			const double z = 500;

			IFeature feature = CreateFeature(featureClass,
			                                 x, x,
			                                 y, y,
			                                 z, null);

			var test = new QaValidCoordinateFields(
				           ReadOnlyTableFactory.Create(featureClass),
				           _fieldNameX, _fieldNameY, _fieldNameZ,
				           _xyTolerance, _zTolerance, _culture)
			           {
				           AllowMissingXYFieldValueForDefinedShape = false,
				           AllowMissingZFieldValueForDefinedShape = false
			           };

			var runner = new QaTestRunner(test);

			runner.Execute(feature);

			Assert.AreEqual(1, runner.Errors.Count);
			QaError error = runner.Errors[0];

			IssueCode issueCode = error.IssueCode;
			Assert.IsNotNull(issueCode);
			Assert.AreEqual(
				"ValidCoordinateFields.ShapeIsDefinedButCoordinateFieldHasNoValue",
				issueCode.ID);

			Assert.AreEqual(_fieldNameZ, error.AffectedComponent);
		}

		[Test]
		public void TestAllowedCoordinatesForUndefinedShape()
		{
			var featureClass = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryPoint);

			featureClass.AddField(FieldUtils.CreateDoubleField(_fieldNameX));
			featureClass.AddField(FieldUtils.CreateDoubleField(_fieldNameY));
			featureClass.AddField(FieldUtils.CreateDoubleField(_fieldNameZ));

			// create point feature
			const double x = 2600000;
			const double y = 1200000;
			const double z = 500;

			IFeature feature = CreateFeature(featureClass,
			                                 x, x,
			                                 y, y,
			                                 z, z);
			feature.Shape.SetEmpty();

			var test = new QaValidCoordinateFields(
				           ReadOnlyTableFactory.Create(featureClass),
				           _fieldNameX, _fieldNameY, _fieldNameZ,
				           _xyTolerance, _zTolerance, _culture)
			           {
				           AllowXYFieldValuesForUndefinedShape = true,
				           AllowZFieldValueForUndefinedShape = true
			           };

			var runner = new QaTestRunner(test);

			runner.Execute(feature);

			Assert.AreEqual(0, runner.Errors.Count);
		}

		[NotNull]
		private static IFeature CreateFeature([NotNull] FeatureClassMock featureClass,
		                                      double x, object xFieldValue,
		                                      double y, object yFieldValue,
		                                      double z, object zFieldValue)
		{
			int fieldIndexX = featureClass.FindField(_fieldNameX);
			int fieldIndexY = featureClass.FindField(_fieldNameY);
			int fieldIndexZ = featureClass.FindField(_fieldNameZ);

			IFeature feature = featureClass.CreateFeature(GeometryFactory.CreatePoint(x, y, z));

			if (fieldIndexX >= 0)
			{
				feature.set_Value(fieldIndexX, xFieldValue);
			}

			if (fieldIndexY >= 0)
			{
				feature.set_Value(fieldIndexY, yFieldValue);
			}

			if (fieldIndexZ >= 0)
			{
				feature.set_Value(fieldIndexZ, zFieldValue);
			}

			feature.Store();

			return feature;
		}
	}
}
