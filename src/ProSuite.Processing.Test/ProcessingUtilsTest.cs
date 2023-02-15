using System;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProSuite.Processing.Utils;

namespace ProSuite.Processing.Test;

[TestFixture]
public class ProcessingUtilsTest
{
	[Test]
	public void CanIsFinite()
	{
		Assert.False(double.NaN.IsFinite());
		Assert.False(double.NegativeInfinity.IsFinite());
		Assert.False(double.PositiveInfinity.IsFinite());
		Assert.True(1.23.IsFinite());
		Assert.True((-1.23e-12).IsFinite());
		Assert.True(double.Epsilon.IsFinite());
	}

	[Test]
	public void CanClamp()
	{
		Assert.AreEqual(5, 5.Clamp(1, 9, "test"));
		Assert.AreEqual(9, 12.Clamp(1, 9, "test"));
		Assert.AreEqual(1, (-2).Clamp(1, 9, "test"));

		Assert.AreEqual(12.3, 12.3.Clamp(1.1, 22.2, "test"));
		Assert.AreEqual(1.1, 0.99.Clamp(1.1, 22.2, "test"));
		Assert.AreEqual(22.2, 1e5.Clamp(1.1, 22.2, "test"));
	}

	[Test]
	public void CanToPositiveDegrees()
	{
		Assert.AreEqual(ProcessingUtils.ToPositiveDegrees(0.0), 0.0);

		Assert.AreEqual(ProcessingUtils.ToPositiveDegrees(1.0), 1.0);
		Assert.AreEqual(ProcessingUtils.ToPositiveDegrees(360.0), 0.0);
		Assert.AreEqual(ProcessingUtils.ToPositiveDegrees(361.0), 1.0);
		Assert.AreEqual(ProcessingUtils.ToPositiveDegrees(720.0), 0.0);
		Assert.AreEqual(ProcessingUtils.ToPositiveDegrees(721.0), 1.0);

		Assert.AreEqual(ProcessingUtils.ToPositiveDegrees(-1.0), 359.0);
		Assert.AreEqual(ProcessingUtils.ToPositiveDegrees(-360.0), 0.0);
		Assert.AreEqual(ProcessingUtils.ToPositiveDegrees(-361.0), 359.0);
		Assert.AreEqual(ProcessingUtils.ToPositiveDegrees(-720.0), 0.0);
		Assert.AreEqual(ProcessingUtils.ToPositiveDegrees(-721.0), 359.0);
	}

	[Test]
	public void CanNormalizeRadians()
	{
		Assert.AreEqual(0.0, ProcessingUtils.NormalizeRadians(0.0));
		Assert.AreEqual(Math.PI, ProcessingUtils.NormalizeRadians(Math.PI));
		Assert.AreEqual(-Math.PI, ProcessingUtils.NormalizeRadians(-Math.PI));
		Assert.AreEqual(Math.PI / 2, ProcessingUtils.NormalizeRadians(4.5 * Math.PI));
		Assert.AreEqual(0.0, ProcessingUtils.NormalizeRadians(2 * Math.PI));
		Assert.AreEqual(Math.PI / 2, ProcessingUtils.NormalizeRadians(-1.5 * Math.PI));
		Assert.AreEqual(-Math.PI / 2, ProcessingUtils.NormalizeRadians(1.5 * Math.PI));
	}

	[Test]
	public void CanAppendScale()
	{
		const string sep = "'";
		var sb = new StringBuilder();

		sb.Append("Scales are ").AppendScale(500);
		var o = sb.Append(" and ").AppendScale(1000, sep);
		sb.Append(" and ").AppendScale(1000 * 1000, sep);

		Assert.AreSame(sb, o);
		Assert.AreEqual("Scales are 1:500 and 1:1'000 and 1:1'000'000", sb.ToString());

		var x1 = $"1:{1234.5}"; // use current culture
		Assert.AreEqual(x1, sb.Clear().AppendScale(1234.5, sep).ToString());

		var x2 = $"1:{0.025}"; // use current culture
		Assert.AreEqual(x2, sb.Clear().AppendScale(0.025, sep).ToString());

		var r3 = sb.Clear().AppendScale(25000).ToString();
		Assert.AreEqual("1:25\u2009000", r3); // default sep is THIN SPACE
	}

	[Test]
	public void CanAppendSRef()
	{
		var sb = new StringBuilder();

		Assert.AreSame(sb, sb.AppendSRef("WGS84", 4326));
		var text = sb.ToString();
		Assert.IsTrue(text.Contains("WGS84"));
		Assert.IsTrue(text.Contains("4326"));
	}

	[Test]
	public void CanParseIntegerList()
	{
		const char sep = ',';

		Assert.IsNull(ProcessingUtils.ParseIntegerList(null, sep));
		Assert.IsNull(ProcessingUtils.ParseIntegerList(string.Empty, sep));
		Assert.IsNull(ProcessingUtils.ParseIntegerList(" \t", sep));
		Assert.IsNull(ProcessingUtils.ParseIntegerList(",, \t,", sep));

		var l1 = ProcessingUtils.ParseIntegerList("-12", sep);
		Assert.NotNull(l1);
		Assert.AreEqual(1, l1.Length);
		Assert.AreEqual(-12, l1[0]);

		var l2 = ProcessingUtils.ParseIntegerList(" 012 ,-55, 0", sep);
		Assert.NotNull(l2);
		Assert.True(l2.SequenceEqual(new[] { 12, -55, 0 }));
	}
}
