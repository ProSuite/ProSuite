using System;
using System.Globalization;
using NUnit.Framework;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Globalization;
using ProSuite.Commons.Test;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.Core.Test.QA
{
	[TestFixture]
	public class ScalarTestParameterValueTest
	{
		[Test]
		[Category(TestCategory.FixMe)]
		public void CanClone()
		{
			string parameterName = "p1Name";
			const string stringValueInvariant = "0.123";

			Type dataType = typeof(double);
			var testParameterValue = new ScalarTestParameterValue(parameterName, dataType);

			testParameterValue.SetStringValue(0.123d.ToString(CultureInfo.CurrentCulture));

			ScalarTestParameterValue clone = (ScalarTestParameterValue) testParameterValue.Clone();

			Assert.IsTrue(testParameterValue.Equals(clone));
			Assert.IsTrue(testParameterValue.DataType == clone.DataType);

			Assert.AreEqual(parameterName, clone.TestParameterName);
			Assert.AreEqual(dataType, clone.DataType);

			Assert.AreEqual(stringValueInvariant, clone.StringValue);
		}

		[Test]
		public void CanAssignDoubleInDeCulture()
		{
			string parameterName = "p1Dbl";

			const double doubleValue = 0.123d;
			const string stringValueInvariant = "0.123";

			Type dataType = typeof(double);
			var testParameterValue = new ScalarTestParameterValue(parameterName, dataType);

			string stringValueDe = null;

			CultureInfoUtils.ExecuteUsing("de-DE",
			                              () =>
			                              {
				                              stringValueDe = doubleValue.ToString(
					                              CultureInfo.CreateSpecificCulture("DE-de"));
				                              testParameterValue.StringValue = stringValueDe;
			                              });

			Assert.AreEqual(stringValueInvariant, testParameterValue.PersistedStringValue);

			testParameterValue.SetValue(doubleValue);
			Assert.AreEqual(stringValueInvariant, testParameterValue.PersistedStringValue);

			CultureInfoUtils.ExecuteUsing("de-DE",
			                              () =>
			                              {
				                              Assert.AreEqual(stringValueDe,
				                                              testParameterValue.GetDisplayValue());
			                              });
		}

		[Test]
		public void CanGetDisplayValue()
		{
			EnsureDisplayValue(typeof(double), 0.123d.ToString(CultureInfo.CurrentCulture));
			EnsureDisplayValue(typeof(int), 123.ToString(CultureInfo.CurrentCulture));
			EnsureDisplayValue(typeof(bool), true.ToString(CultureInfo.CurrentCulture));
			EnsureDisplayValue(typeof(DateTime),
			                   new DateTime(1975, 4, 27, 19, 30, 0).ToString(
				                   CultureInfo.CurrentCulture));
			EnsureDisplayValue(typeof(FieldType), FieldType.Integer.ToString());
		}

		private static void EnsureDisplayValue(Type dataType,
		                                       string stringValueCurrentCulture)
		{
			const string parameterName = "Test";

			var testParameterValue = new ScalarTestParameterValue(parameterName, dataType);

			testParameterValue.SetStringValue(stringValueCurrentCulture);

			string valueWithKnownDatatype = testParameterValue.GetDisplayValue();

			Assert.AreEqual(stringValueCurrentCulture, valueWithKnownDatatype);

			object value = testParameterValue.GetValue();
			Assert.NotNull(value);

			string persistedValue = testParameterValue.PersistedStringValue;

			testParameterValue.DataType = null;

			string valueWithGuessedDatatype = testParameterValue.GetDisplayValue();

			Assert.AreEqual(stringValueCurrentCulture, valueWithGuessedDatatype);

			// Now set the same value using InvariantCulture and try again:
			testParameterValue = new ScalarTestParameterValue(parameterName, dataType);
			testParameterValue.SetStringValue(persistedValue, CultureInfo.InvariantCulture);

			valueWithKnownDatatype = testParameterValue.GetDisplayValue();

			Assert.AreEqual(stringValueCurrentCulture, valueWithKnownDatatype);

			testParameterValue.DataType = null;

			valueWithGuessedDatatype = testParameterValue.GetDisplayValue();

			Assert.AreEqual(stringValueCurrentCulture, valueWithGuessedDatatype);
		}
	}
}
