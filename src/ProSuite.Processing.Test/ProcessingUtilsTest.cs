using System;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Processing.Domain;
using ProSuite.Processing.Evaluation;
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

	[Test]
	public void CanGetParameter()
	{
		var processType = typeof(TestProcess);

		var p0 = ProcessingUtils.GetParameter(processType, "NoSuchParameter");
		Assert.IsNull(p0);

		var p1 = ProcessingUtils.GetParameter(processType, nameof(TestProcess.Parameter1));
		Assert.NotNull(p1);
		Assert.AreEqual(nameof(TestProcess.Parameter1), p1.Name);
		Assert.AreEqual(typeof(double), p1.Type);
		Assert.IsFalse(p1.Required);
		Assert.IsFalse(p1.Multivalued);
		Assert.AreEqual(1.25, p1.DefaultValue);

		var p2 = ProcessingUtils.GetParameter(processType, nameof(TestProcess.Parameter2));
		Assert.NotNull(p2);
		Assert.AreEqual(nameof(TestProcess.Parameter2), p2.Name);
		Assert.AreEqual(typeof(ImplicitValue<int>), p2.Type);
		Assert.IsFalse(p2.Required);
		Assert.IsFalse(p2.Multivalued);
		Assert.AreEqual(3, p2.DefaultValue);

		var p3 = ProcessingUtils.GetParameter(processType, nameof(TestProcess.Parameter3));
		Assert.NotNull(p3);
		Assert.AreEqual(nameof(TestProcess.Parameter3), p3.Name);
		Assert.AreEqual(typeof(ImplicitValue<double>), p3.Type);
		Assert.IsTrue(p3.Required);
		Assert.IsFalse(p3.Multivalued);
		Assert.AreEqual(0.0, p3.DefaultValue);
	}

	[Test]
	public void CanExpandParameterDescription()
	{
		const string text = "{DefaultValue}, {PublicConstant}, {PrivateConstant}, {NoSuchField} end";

		var processType = typeof(TestProcess);
		var parameter = ProcessingUtils.GetParameter(processType, nameof(TestProcess.Parameter1));
		Assert.NotNull(parameter);

		var result = parameter.ExpandParameterDescription(text);

		Assert.AreEqual("1.25, 12, hi, {NoSuchField} end", result);
	}

	private class TestProcess
	{
		[UsedImplicitly]
		public const int PublicConstant = 12;
		[UsedImplicitly]
		private const string PrivateConstant = "hi";

		[OptionalParameter(DefaultValue = 1.25)]
		public double Parameter1 { get; set; }

		[OptionalParameter(DefaultValue = 3)]
		public ImplicitValue<int> Parameter2 { get; set; }

		[RequiredParameter]
		public ImplicitValue<double> Parameter3 { get; set; }
	}
}
