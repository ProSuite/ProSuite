using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.NamedValuesExpressions;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.Test.NamedValuesExpressions
{
	[TestFixture]
	public class NamedValuesParserTest
	{
		[Test]
		public void CanParseEmpty()
		{
			NamedValuesParser parser = CreateParser();

			IList<NamedValuesExpression> namedValuesExpressions;
			NotificationCollection notifications;
			Assert.IsTrue(parser.TryParse(string.Empty, out namedValuesExpressions,
			                              out notifications));

			Assert.AreEqual(0, notifications.Count);
			Assert.AreEqual(0, namedValuesExpressions.Count);
		}

		[Test]
		public void CantParseInvalid()
		{
			NamedValuesParser parser = CreateParser();

			var sb = new StringBuilder();
			sb.Append("name0=value1,value2, value3;"); // ok
			sb.Append("name1;"); // no '='
			sb.AppendLine("=valuexy"); // '=' in invalid location
			sb.AppendLine("   =value12"); // empty NamedValues name
			sb.AppendLine(" name3 = value99"); // ok

			string namedValuesStrings = sb.ToString();

			IList<NamedValuesExpression> expressions;
			NotificationCollection notifications;
			Assert.IsFalse(parser.TryParse(namedValuesStrings, out expressions,
			                               out notifications));

			Assert.AreEqual(3, notifications.Count);
			Assert.AreEqual(2, expressions.Count);

			Console.Write(NotificationUtils.Concatenate(notifications,
			                                            Environment.NewLine,
			                                            "- {0}"));
		}

		[Test]
		public void CanParseConjunction()
		{
			NamedValuesParser parser = CreateParser();

			var sb = new StringBuilder();
			sb.Append("name0=value1,value2, value3 AND name1=valueA, valueB;");

			string namedValuesString = sb.ToString();
			Console.WriteLine(namedValuesString);

			IList<NamedValuesExpression> expressions;
			NotificationCollection notifications;

			Assert.IsTrue(parser.TryParse(namedValuesString,
			                              out expressions,
			                              out notifications));

			Assert.AreEqual(1, expressions.Count);
			var conjunction = expressions[0] as NamedValuesConjunctionExpression;
			Assert.IsNotNull(conjunction);

			var list = new List<NamedValues>(conjunction.NamedValuesCollection);

			AssertValuesAreEqual("name0", list[0], "value1", "value2", "value3");
			AssertValuesAreEqual("name1", list[1], "valueA", "valueB");
		}

		[Test]
		public void CanParse()
		{
			NamedValuesParser parser = CreateParser();

			var sb = new StringBuilder();
			sb.Append("name0=value1,value2, value3; ");
			sb.Append("name1=valueA, valueB  ;");
			sb.AppendLine(" name2 = valueX , valueY");
			sb.AppendLine();
			sb.AppendLine(" name3 = value99");
			sb.AppendLine(" name4 =; ");
			sb.AppendLine(" name5 =    aa bb cc    ; ");
			sb.AppendLine(" ");

			string namedValuesString = sb.ToString();
			Console.WriteLine(namedValuesString);

			IList<NamedValuesExpression> expressions;
			NotificationCollection notifications;

			Assert.IsTrue(parser.TryParse(namedValuesString,
			                              out expressions,
			                              out notifications));

			Assert.AreEqual(0, notifications.Count);
			Assert.AreEqual(6, expressions.Count);

			List<NamedValues> namedValues = expressions
			                                .Cast<SimpleNamedValuesExpression>()
			                                .Select(e => e.NamedValues)
			                                .ToList();

			AssertValuesAreEqual("name0", namedValues[0], "value1", "value2", "value3");
			AssertValuesAreEqual("name1", namedValues[1], "valueA", "valueB");
			AssertValuesAreEqual("name2", namedValues[2], "valueX", "valueY");
			AssertValuesAreEqual("name3", namedValues[3], "value99");
			AssertValuesAreEqual("name4", namedValues[4]);
			AssertValuesAreEqual("name5", namedValues[5], "aa bb cc");
		}

		[NotNull]
		private static NamedValuesParser CreateParser()
		{
			return new NamedValuesParser('=',
			                             new[] {";", Environment.NewLine},
			                             new[] {","},
			                             " AND ");
		}

		private static void AssertValuesAreEqual([NotNull] string expectedName,
		                                         [NotNull] NamedValues namedValues,
		                                         params string[] expectedValues)
		{
			Console.WriteLine(@"NamedValues: >>{0}<<", namedValues.Name);
			foreach (string value in namedValues.Values)
			{
				Console.WriteLine(@" >>{0}<<", value);
			}

			Assert.AreEqual(expectedName, namedValues.Name);
			Assert.AreEqual(expectedValues.Length, namedValues.ValueCount,
			                "unexpected value count");

			for (int i = 0; i < expectedValues.Length; i++)
			{
				Assert.AreEqual(expectedValues[i], namedValues.GetValue(i), "unexpected value");
			}
		}
	}
}
