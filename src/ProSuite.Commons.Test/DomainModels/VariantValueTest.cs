using System;
using NUnit.Framework;
using ProSuite.Commons.DomainModels;

namespace ProSuite.Commons.Test.DomainModels
{
	[TestFixture]
	public class VariantValueTest
	{
		[Test]
		public void CanChangeIntToDouble()
		{
			int input = 123;
			var value = new VariantValue(input);

			Assert.IsTrue(value.CanChangeTo(VariantValueType.Double));

			value.Type = VariantValueType.Double;

			Assert.AreEqual(123.0, (double) value.Value);
		}

		[Test]
		public void CanInferValueForDouble()
		{
			double input = 0.00012345;
			var value = new VariantValue(input);

			Assert.AreEqual(input, (double) value.Value);
		}

		[Test]
		public void CanSetTypeForDouble()
		{
			double input = 0.00012345;
			var value = new VariantValue(input, VariantValueType.Double);

			Assert.AreEqual(input, (double) value.Value);
		}

		[Test]
		public void CantAssignStringToIntegerValue()
		{
			int input = 123;
			var value = new VariantValue(input);

			Assert.IsFalse(value.IsValidValue("test"));

			bool assigned = false;
			try
			{
				value.Value = "test";
				assigned = true;
			}
			catch (Exception e)
			{
				Console.WriteLine("Expected exception: {0}", e.Message);
			}

			Assert.IsFalse(assigned, "Exception expected");
		}

		[Test]
		public void CantAssignValueOfUnsupportedType()
		{
			bool failed = false;
			try
			{
				new VariantValue(new VariantValueTest());
			}
			catch (Exception)
			{
				failed = true;
			}

			Assert.IsTrue(failed, "Value should not be assignable");
		}
	}
}