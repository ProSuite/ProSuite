using ProSuite.Processing.Utils;
using NUnit.Framework;

namespace ProSuite.Processing.Test
{
    public class ExtremityTypeTest
    {
		[Test]
		public void CanExtremityTypeInvert()
		{
			ExtremityType? value = null;
			Assert.IsNull(value.Invert());

			value = ExtremityType.Both;
			Assert.AreEqual(ExtremityType.None, value.Invert());

			value = ExtremityType.JustBegin;
			Assert.AreEqual(ExtremityType.JustEnd, value.Invert());

			value = ExtremityType.JustEnd;
			Assert.AreEqual(ExtremityType.JustBegin, value.Invert());

			value = ExtremityType.None;
			Assert.AreEqual(ExtremityType.Both, value.Invert());
		}

		[Test]
		public void CanExtremityTypeSetBeginEnd()
		{
			Assert.IsNull(((ExtremityType?) null).SetBegin(true));
			Assert.IsNull(((ExtremityType?) null).SetEnd(true));
			Assert.IsNull(((ExtremityType?) null).SetBegin(false));
			Assert.IsNull(((ExtremityType?) null).SetEnd(false));

			ExtremityType value = ExtremityType.Both;

			Assert.AreEqual(ExtremityType.JustEnd, value = value.SetBegin(false));
			Assert.AreEqual(ExtremityType.JustEnd, value = value.SetEnd(true));
			Assert.AreEqual(ExtremityType.Both, value = value.SetBegin(true));
			Assert.AreEqual(ExtremityType.JustBegin, value = value.SetEnd(false));
			Assert.AreEqual(ExtremityType.None, value.SetBegin(false));
		}

		[Test]
		public void CanExtremityTypeRecompute()
		{
			// Treat null as Both (the default value)
			// unless no ends are being processed: 

			ExtremityType? nullable = null;
			Assert.AreEqual(ExtremityType.Both, nullable.Recompute(true, true, true, true));
			Assert.IsNull(nullable.Recompute(false, false, true, true));

			// There remain 4 * 2 * 2 * 2 * 2 = 64 possibilities:

			ExtremityType value = ExtremityType.Both;

			Assert.AreEqual(ExtremityType.Both, value.Recompute(false, false, false, false));
			Assert.AreEqual(ExtremityType.Both, value.Recompute(false, false, true, false));
			Assert.AreEqual(ExtremityType.Both, value.Recompute(false, false, false, true));
			Assert.AreEqual(ExtremityType.Both, value.Recompute(false, false, true, true));

			Assert.AreEqual(ExtremityType.JustEnd, value.Recompute(true, false, false, false));
			Assert.AreEqual(ExtremityType.Both, value.Recompute(true, false, true, false));
			Assert.AreEqual(ExtremityType.JustEnd, value.Recompute(true, false, false, true));
			Assert.AreEqual(ExtremityType.Both, value.Recompute(true, false, true, true));

			Assert.AreEqual(ExtremityType.JustBegin, value.Recompute(false, true, false, false));
			Assert.AreEqual(ExtremityType.JustBegin, value.Recompute(false, true, true, false));
			Assert.AreEqual(ExtremityType.Both, value.Recompute(false, true, false, true));
			Assert.AreEqual(ExtremityType.Both, value.Recompute(false, true, true, true));

			Assert.AreEqual(ExtremityType.None, value.Recompute(true, true, false, false));
			Assert.AreEqual(ExtremityType.JustBegin, value.Recompute(true, true, true, false));
			Assert.AreEqual(ExtremityType.JustEnd, value.Recompute(true, true, false, true));
			Assert.AreEqual(ExtremityType.Both, value.Recompute(true, true, true, true));

			value = ExtremityType.JustBegin;

			Assert.AreEqual(ExtremityType.JustBegin,
			                value.Recompute(false, false, false, false));
			Assert.AreEqual(ExtremityType.JustBegin, value.Recompute(false, false, true, false));
			Assert.AreEqual(ExtremityType.JustBegin, value.Recompute(false, false, false, true));
			Assert.AreEqual(ExtremityType.JustBegin, value.Recompute(false, false, true, true));

			Assert.AreEqual(ExtremityType.None, value.Recompute(true, false, false, false));
			Assert.AreEqual(ExtremityType.JustBegin, value.Recompute(true, false, true, false));
			Assert.AreEqual(ExtremityType.None, value.Recompute(true, false, false, true));
			Assert.AreEqual(ExtremityType.JustBegin, value.Recompute(true, false, true, true));

			Assert.AreEqual(ExtremityType.JustBegin, value.Recompute(false, true, false, false));
			Assert.AreEqual(ExtremityType.JustBegin, value.Recompute(false, true, true, false));
			Assert.AreEqual(ExtremityType.Both, value.Recompute(false, true, false, true));
			Assert.AreEqual(ExtremityType.Both, value.Recompute(false, true, true, true));

			Assert.AreEqual(ExtremityType.None, value.Recompute(true, true, false, false));
			Assert.AreEqual(ExtremityType.JustBegin, value.Recompute(true, true, true, false));
			Assert.AreEqual(ExtremityType.JustEnd, value.Recompute(true, true, false, true));
			Assert.AreEqual(ExtremityType.Both, value.Recompute(true, true, true, true));

			value = ExtremityType.JustEnd;

			Assert.AreEqual(ExtremityType.JustEnd, value.Recompute(false, false, false, false));
			Assert.AreEqual(ExtremityType.JustEnd, value.Recompute(false, false, true, false));
			Assert.AreEqual(ExtremityType.JustEnd, value.Recompute(false, false, false, true));
			Assert.AreEqual(ExtremityType.JustEnd, value.Recompute(false, false, true, true));

			Assert.AreEqual(ExtremityType.JustEnd, value.Recompute(true, false, false, false));
			Assert.AreEqual(ExtremityType.Both, value.Recompute(true, false, true, false));
			Assert.AreEqual(ExtremityType.JustEnd, value.Recompute(true, false, false, true));
			Assert.AreEqual(ExtremityType.Both, value.Recompute(true, false, true, true));

			Assert.AreEqual(ExtremityType.None, value.Recompute(false, true, false, false));
			Assert.AreEqual(ExtremityType.None, value.Recompute(false, true, true, false));
			Assert.AreEqual(ExtremityType.JustEnd, value.Recompute(false, true, false, true));
			Assert.AreEqual(ExtremityType.JustEnd, value.Recompute(false, true, true, true));

			Assert.AreEqual(ExtremityType.None, value.Recompute(true, true, false, false));
			Assert.AreEqual(ExtremityType.JustBegin, value.Recompute(true, true, true, false));
			Assert.AreEqual(ExtremityType.JustEnd, value.Recompute(true, true, false, true));
			Assert.AreEqual(ExtremityType.Both, value.Recompute(true, true, true, true));

			value = ExtremityType.None;

			Assert.AreEqual(ExtremityType.None, value.Recompute(false, false, false, false));
			Assert.AreEqual(ExtremityType.None, value.Recompute(false, false, true, false));
			Assert.AreEqual(ExtremityType.None, value.Recompute(false, false, false, true));
			Assert.AreEqual(ExtremityType.None, value.Recompute(false, false, true, true));

			Assert.AreEqual(ExtremityType.None, value.Recompute(true, false, false, false));
			Assert.AreEqual(ExtremityType.JustBegin, value.Recompute(true, false, true, false));
			Assert.AreEqual(ExtremityType.None, value.Recompute(true, false, false, true));
			Assert.AreEqual(ExtremityType.JustBegin, value.Recompute(true, false, true, true));

			Assert.AreEqual(ExtremityType.None, value.Recompute(false, true, false, false));
			Assert.AreEqual(ExtremityType.None, value.Recompute(false, true, true, false));
			Assert.AreEqual(ExtremityType.JustEnd, value.Recompute(false, true, false, true));
			Assert.AreEqual(ExtremityType.JustEnd, value.Recompute(false, true, true, true));

			Assert.AreEqual(ExtremityType.None, value.Recompute(true, true, false, false));
			Assert.AreEqual(ExtremityType.JustBegin, value.Recompute(true, true, true, false));
			Assert.AreEqual(ExtremityType.JustEnd, value.Recompute(true, true, false, true));
			Assert.AreEqual(ExtremityType.Both, value.Recompute(true, true, true, true));
		}
    }
}
