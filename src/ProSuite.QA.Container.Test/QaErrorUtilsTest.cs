using System.Collections.Generic;
using NUnit.Framework;

namespace ProSuite.QA.Container.Test
{
	[TestFixture]
	public class QaErrorUtilsTest
	{
		[Test]
		public void CanGetValuesFromNullList()
		{
			QaErrorUtils.GetValues(null,
			                       out double? doubleValue1,
			                       out double? doubleValue2,
			                       out string textValue);

			Assert.IsNull(doubleValue1);
			Assert.IsNull(doubleValue2);
			Assert.IsNull(textValue);
		}

		[Test]
		public void CanGetValuesFromEmptyList()
		{
			QaErrorUtils.GetValues(new List<object>(),
			                       out double? doubleValue1,
			                       out double? doubleValue2,
			                       out string textValue);

			Assert.IsNull(doubleValue1);
			Assert.IsNull(doubleValue2);
			Assert.IsNull(textValue);
		}

		[Test]
		public void CanGetNumericValuesOnly()
		{
			QaErrorUtils.GetValues(new List<object> { 1.5, 2.5 },
			                       out double? doubleValue1,
			                       out double? doubleValue2,
			                       out string textValue);

			Assert.AreEqual(1.5, doubleValue1);
			Assert.AreEqual(2.5, doubleValue2);
			Assert.IsNull(textValue);
		}

		[Test]
		public void CanGetNumericValuesOfAnyNumericType()
		{
			QaErrorUtils.GetValues(new List<object> { (short) 1, 2f },
			                       out double? doubleValue1,
			                       out double? doubleValue2,
			                       out string textValue);

			Assert.AreEqual(1, doubleValue1);
			Assert.AreEqual(2, doubleValue2);
			Assert.IsNull(textValue);

			QaErrorUtils.GetValues(new List<object> { 3, 4m },
			                       out doubleValue1, out doubleValue2, out textValue);

			Assert.AreEqual(3, doubleValue1);
			Assert.AreEqual(4, doubleValue2);
			Assert.IsNull(textValue);
		}

		[Test]
		public void CanGetTextValueOnly()
		{
			QaErrorUtils.GetValues(new List<object> { "abc" },
			                       out double? doubleValue1,
			                       out double? doubleValue2,
			                       out string textValue);

			Assert.IsNull(doubleValue1);
			Assert.IsNull(doubleValue2);
			Assert.AreEqual("abc", textValue);
		}

		[Test]
		public void CanGetValuesInMixedOrder()
		{
			QaErrorUtils.GetValues(new List<object> { 1.5, "abc", 2.5 },
			                       out double? doubleValue1,
			                       out double? doubleValue2,
			                       out string textValue);

			Assert.AreEqual(1.5, doubleValue1);
			Assert.AreEqual(2.5, doubleValue2);
			Assert.AreEqual("abc", textValue);
		}

		[Test]
		public void CanGetValuesIgnoringNullsInList()
		{
			QaErrorUtils.GetValues(new List<object> { null, 1.5, null, "abc", null },
			                       out double? doubleValue1,
			                       out double? doubleValue2,
			                       out string textValue);

			Assert.AreEqual(1.5, doubleValue1);
			Assert.IsNull(doubleValue2);
			Assert.AreEqual("abc", textValue);
		}

		[Test]
		public void CanGetValuesFromMoreValuesThanColumns()
		{
			// the third numeric value goes to the (still empty) text value, converted;
			// any further value is dropped
			QaErrorUtils.GetValues(new List<object> { 1.5, 2.5, 3.5, "abc" },
			                       out double? doubleValue1,
			                       out double? doubleValue2,
			                       out string textValue);

			Assert.AreEqual(1.5, doubleValue1);
			Assert.AreEqual(2.5, doubleValue2);
			Assert.AreEqual("3.5", textValue);
		}
	}
}
