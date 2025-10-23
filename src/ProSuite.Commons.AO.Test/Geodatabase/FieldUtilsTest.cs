using System;
using System.Globalization;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;

namespace ProSuite.Commons.AO.Test.Geodatabase
{
	[TestFixture]
	public class FieldUtilsTest
	{
		[Test]
		public void CanCheckValuesForEquality()
		{
			const short int16Value = 100;
			const int int32Value = 100;
			const long int64Value = 100;

			const double doubleValue = 1.111;
			const double equalDoubleValue = 1.11100000000001;
			const double unEqualDoubleValue = 1.1110000000001;

			const float floatValue = 1.111f;
			const float equalFloatValue = 1.111000001f;
			const float unequalFloatValue = 1.11100001f;

			DateTime dateTimeValue = new DateTime(2020, 1, 1, 12, 0, 0);
			DateTime sameTimeValue = new DateTime(2020, 1, 1, 12, 0, 0);
			DateTime differentTimeValue = new DateTime(2020, 1, 1, 12, 0, 1);

			Assert.True(FieldUtils.AreValuesEqual(int16Value, int32Value));
			Assert.True(FieldUtils.AreValuesEqual(int32Value, int16Value));

			Assert.True(FieldUtils.AreValuesEqual(int16Value, int64Value));
			Assert.True(FieldUtils.AreValuesEqual(int64Value, int16Value));

			Assert.True(FieldUtils.AreValuesEqual(int32Value, int64Value));
			Assert.True(FieldUtils.AreValuesEqual(int64Value, int32Value));

			Assert.True(FieldUtils.AreValuesEqual(doubleValue, floatValue));
			Assert.True(FieldUtils.AreValuesEqual(floatValue, doubleValue));
			Assert.True(FieldUtils.AreValuesEqual(floatValue, equalFloatValue));
			Assert.True(FieldUtils.AreValuesEqual(doubleValue, equalDoubleValue));

			Assert.False(FieldUtils.AreValuesEqual(floatValue, unequalFloatValue));
			Assert.False(FieldUtils.AreValuesEqual(doubleValue, unEqualDoubleValue));

			Assert.False(FieldUtils.AreValuesEqual("aa", "AA"));
			Assert.True(FieldUtils.AreValuesEqual("aa", "AA", caseSensitive: false));

			Assert.True(FieldUtils.AreValuesEqual(dateTimeValue, dateTimeValue));
			Assert.True(FieldUtils.AreValuesEqual(dateTimeValue, sameTimeValue));
			Assert.False(FieldUtils.AreValuesEqual(dateTimeValue, differentTimeValue));
		}

		[Test]
		public void CanConvertInt32ToInt16()
		{
			const int sourceValue = 100;
			object targetValue =
				FieldUtils.ConvertAttributeValue(
					sourceValue,
					esriFieldType.esriFieldTypeInteger,
					esriFieldType.esriFieldTypeSmallInteger);

			Assert.IsInstanceOf<short>(targetValue);
			Assert.AreEqual(sourceValue, targetValue);
		}

		[Test]
		public void CantConvertLargeInt32ToInt16()
		{
			const int sourceValue = 10000000;

			Assert.Throws<OverflowException>(
				() => FieldUtils.ConvertAttributeValue(
					sourceValue,
					esriFieldType.esriFieldTypeInteger,
					esriFieldType.esriFieldTypeSmallInteger));
		}

		[Test]
		public void CanConvertInt16ToInt32()
		{
			const short sourceValue = 100;
			object targetValue =
				FieldUtils.ConvertAttributeValue(
					sourceValue,
					esriFieldType.esriFieldTypeSmallInteger,
					esriFieldType.esriFieldTypeInteger);

			Assert.IsInstanceOf<int>(targetValue);
			Assert.AreEqual(sourceValue, targetValue);
		}

		[Test]
		public void CanConvertStringToInt32()
		{
			const string sourceValue = "111";
			object targetValue =
				FieldUtils.ConvertAttributeValue(
					sourceValue,
					esriFieldType.esriFieldTypeString,
					esriFieldType.esriFieldTypeInteger);

			Assert.IsInstanceOf<int>(targetValue);
			Assert.AreEqual(sourceValue, targetValue.ToString());
		}

		[Test]
		public void CantConvertNonNumericStringToInt32()
		{
			const string sourceValue = "abc";

			Assert.Throws<FormatException>(
				() => FieldUtils.ConvertAttributeValue(
					sourceValue,
					esriFieldType.esriFieldTypeString,
					esriFieldType.esriFieldTypeInteger));
		}

		[Test]
		public void CanConvertDoubleToInt32()
		{
			const double sourceValue = 111.123456;
			object targetValue =
				FieldUtils.ConvertAttributeValue(
					sourceValue,
					esriFieldType.esriFieldTypeDouble,
					esriFieldType.esriFieldTypeInteger);

			Assert.IsInstanceOf<int>(targetValue);
			Assert.AreEqual(Math.Round(sourceValue), targetValue);
		}

		[Test]
		public void CanConvertDoubleToString()
		{
			CultureInfo cultureInfo = CultureInfo.GetCultureInfo("de-DE");

			const double sourceValue = 111.123456;
			object targetValue =
				FieldUtils.ConvertAttributeValue(
					sourceValue,
					esriFieldType.esriFieldTypeDouble,
					esriFieldType.esriFieldTypeString, cultureInfo);

			Assert.IsInstanceOf<string>(targetValue);
			Assert.AreEqual(sourceValue.ToString(cultureInfo), targetValue);
		}

		[Test]
		[Category(Commons.Test.TestCategory.FixMe)]
		public void CanConvertDateTimeToString()
		{
			DateTime sourceValue = DateTime.Now;
			object targetValue =
				FieldUtils.ConvertAttributeValue(
					sourceValue,
					esriFieldType.esriFieldTypeDate,
					esriFieldType.esriFieldTypeString);

			Assert.IsInstanceOf<string>(targetValue);
			Assert.AreEqual(sourceValue.ToString(CultureInfo.GetCultureInfo("de-DE")),
			                targetValue);
		}

		[Test]
		public void CantConvertVeryLongDoubleToInt32()
		{
			const double sourceValue = 1111111111111111111111.0;

			Assert.Throws<OverflowException>(
				() => FieldUtils.ConvertAttributeValue(
					sourceValue,
					esriFieldType.esriFieldTypeDouble,
					esriFieldType.esriFieldTypeInteger));
		}

		[Test]
		public void CanConvertStringToGuid()
		{
			const string sourceValue = "{E2F909CD-3EE2-4885-BFD0-9CB19A9A9A06}";
			object targetValue =
				FieldUtils.ConvertAttributeValue(
					sourceValue,
					esriFieldType.esriFieldTypeString,
					esriFieldType.esriFieldTypeGUID);

			Assert.IsInstanceOf<Guid>(targetValue);
			Assert.AreEqual(new Guid(sourceValue), targetValue);
		}
	}
}
