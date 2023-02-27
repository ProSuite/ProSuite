using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using ProSuite.Commons.Collections;
using ProSuite.Processing.Utils;

namespace ProSuite.Processing.Test
{
	[TestFixture]
	public class WhereClauseMatcherTest
	{
		[Test]
		public void BasicTests()
		{
			var values = new NamedValues();
			values.AddValue("A", "a");
			values.AddValue("B", "b");
			values.AddValue("C", "c");
			values.AddValue("ONE", 1);
			values.AddValue("TWO", 2);
			values.AddValue("PI", 3.14159);
			values.AddValue("X", DBNull.Value);
			values.AddValue("Y", null);

			var matcher = new WhereClauseMatcher("ONE = 1");
			Assert.IsTrue(matcher.Match(values));

			matcher = new WhereClauseMatcher("ONE = 1.0");
			Assert.IsTrue(matcher.Match(values));

			matcher = new WhereClauseMatcher("PI = 3");
			Assert.IsFalse(matcher.Match(values));

			matcher = new WhereClauseMatcher("PI = 3.141591");
			Assert.IsFalse(matcher.Match(values));

			matcher = new WhereClauseMatcher("PI = 3.141590");
			Assert.IsTrue(matcher.Match(values));

			matcher = new WhereClauseMatcher("A in ('a','b')");
			Console.WriteLine(@"Clause: ""{0}""", matcher.Clause);
			Console.WriteLine(@"Engine: {0}", GetDump(matcher));
			Assert.IsTrue(matcher.Match(values));

			matcher = new WhereClauseMatcher("A = 'x' AND NOT B IN ('a','b','c') OR A = 'a' AND B IS NOT NULL");
			Console.WriteLine(@"Clause: ""{0}""", matcher.Clause);
			Console.WriteLine(@"Engine: {0}", GetDump(matcher));
			Assert.IsTrue(matcher.Match(values));

			// Treat both C# null and DBNull.Value as NULL:
			Assert.IsTrue(new WhereClauseMatcher("X is NULL").Match(values));
			Assert.IsTrue(new WhereClauseMatcher("Y is NULL").Match(values));
		}

		[Test]
		public void CanCompareEquality()
		{
			var values = new NamedValues();

			var matcher = new WhereClauseMatcher("1=1");
			Assert.IsTrue(matcher.Match(values));

			matcher = new WhereClauseMatcher("1.0=1.0");
			Assert.IsTrue(matcher.Match(values));

			matcher = new WhereClauseMatcher("1 = 1.0");
			Assert.IsTrue(matcher.Match(values));

			matcher = new WhereClauseMatcher("'abc' = 'abc'");
			Assert.IsTrue(matcher.Match(values));

			matcher = new WhereClauseMatcher("'foo' = 'bar'");
			Assert.IsFalse(matcher.Match(values));

			matcher = new WhereClauseMatcher("FALSE=FALSE");
			Assert.IsTrue(matcher.Match(values));

			matcher = new WhereClauseMatcher("FALSE= TRUE");
			Assert.IsFalse(matcher.Match(values));

			matcher = new WhereClauseMatcher("TRUE =FALSE");
			Assert.IsFalse(matcher.Match(values));

			matcher = new WhereClauseMatcher("TRUE = TRUE");
			Assert.IsTrue(matcher.Match(values));

			var m1 = new WhereClauseMatcher("0 = FALSE");
			var ex = Assert.Catch(() => m1.Match(values));
			Assert.IsNotNull(ex, "Expected exception not thrown");
			Console.WriteLine(@"Clause ""{0}"" throws: {1}", matcher.Clause, ex.Message);

			var m2 = new WhereClauseMatcher("1 = TRUE");
			ex = Assert.Catch(() => m2.Match(values));
			Assert.IsNotNull(ex, "Expected exception not thrown");
			Console.WriteLine(@"Clause ""{0}"" throws: {1}", matcher.Clause, ex.Message);

			var m3 = new WhereClauseMatcher("'false' = FALSE");
			ex = Assert.Catch(() => m3.Match(values));
			Assert.IsNotNull(ex, "Expected exception not thrown");
			Console.WriteLine(@"Clause ""{0}"" throws: {1}", matcher.Clause, ex.Message);

			var m4 = new WhereClauseMatcher("'false' <> FALSE");
			ex = Assert.Catch(() => m4.Match(values));
			Assert.IsNotNull(ex, "Expected exception not thrown");
			Console.WriteLine(@"Clause ""{0}"" throws: {1}", matcher.Clause, ex.Message);
		}

		[Test]
		public void CanCompareInequality()
		{
			var values = new NamedValues();
			values.AddValue("foo", "foo");
			values.AddValue("bar", "bar");

			Assert.IsTrue(new WhereClauseMatcher("0 < 1").Match(values));
			Assert.IsTrue(new WhereClauseMatcher("0.999 < 0.9999").Match(values));
			Assert.IsFalse(new WhereClauseMatcher("3 < 3").Match(values));
			Assert.IsTrue(new WhereClauseMatcher("3 <= 3").Match(values));

			Assert.IsTrue(new WhereClauseMatcher("5 <= 5.0").Match(values));
			Assert.IsTrue(new WhereClauseMatcher("5 < 5.00000001").Match(values));
			Assert.IsTrue(new WhereClauseMatcher("0.9999999998 <> 0.9999999999").Match(values));

			Assert.IsTrue(new WhereClauseMatcher("false < true").Match(values));
			Assert.IsTrue(new WhereClauseMatcher("false <> true").Match(values));
			Assert.IsTrue(new WhereClauseMatcher("true >= false").Match(values));

			Assert.IsTrue(new WhereClauseMatcher("'foo' <> 'bar'").Match(values));
			Assert.IsFalse(new WhereClauseMatcher("'foo' < 'bar'").Match(values));
			Assert.IsTrue(new WhereClauseMatcher("'foo' >= 'bar'").Match(values));

			Assert.IsTrue(new WhereClauseMatcher("foo <> bar").Match(values));
			Assert.IsFalse(new WhereClauseMatcher("foo < bar").Match(values));
			Assert.IsTrue(new WhereClauseMatcher("foo >= bar").Match(values));

			Assert.IsTrue(new WhereClauseMatcher("NULL <> NULL").Match(values));
			Assert.IsFalse(new WhereClauseMatcher("NULL = NULL").Match(values));
			Assert.IsFalse(new WhereClauseMatcher("123 = NULL").Match(values));
			Assert.IsFalse(new WhereClauseMatcher("NULL = 123").Match(values));
		}

		[Test]
		public void CanParentheses()
		{
			var values = new NamedValues();
			values.AddValue("A", 3);
			values.AddValue("B", 5);

			var matcher = new WhereClauseMatcher("A=99 AND (B=4 OR B=5)");
			Console.WriteLine(@"Clause: ""{0}""", matcher.Clause);
			Console.WriteLine(@"Engine: {0}", GetDump(matcher));
			Assert.IsFalse(matcher.Match(values));

			matcher = new WhereClauseMatcher("A=99 AND B=4 OR B=5");
			Console.WriteLine(@"Clause: ""{0}""", matcher.Clause);
			Console.WriteLine(@"Engine: {0}", GetDump(matcher));
			Assert.IsTrue(matcher.Match(values));

			matcher = new WhereClauseMatcher("(A=3)");
			Console.WriteLine(@"Clause: ""{0}""", matcher.Clause);
			Console.WriteLine(@"Engine: {0}", GetDump(matcher));
			Assert.IsTrue(matcher.Match(values));

			matcher = new WhereClauseMatcher("(((A=3)))");
			Console.WriteLine(@"Clause: ""{0}""", matcher.Clause);
			Console.WriteLine(@"Engine: {0}", GetDump(matcher));
			Assert.IsTrue(matcher.Match(values));
		}

		[Test]
		public void CanIsNullAndNotNull()
		{
			var values = new NamedValues();
			values.AddValue("A", "abc");
			values.AddValue("B", 1.23);
			values.AddValue("C", 123);
			values.AddValue("D", DBNull.Value);
			values.AddValue("E", null);

			Assert.IsFalse(new WhereClauseMatcher("A IS NULL").Match(values));
			Assert.IsFalse(new WhereClauseMatcher("B IS NULL").Match(values));
			Assert.IsFalse(new WhereClauseMatcher("C IS NULL").Match(values));
			Assert.IsTrue(new WhereClauseMatcher("D IS NULL").Match(values));
			Assert.IsTrue(new WhereClauseMatcher("E IS NULL").Match(values));

			Assert.IsTrue(new WhereClauseMatcher("A IS NOT NULL").Match(values));
			Assert.IsTrue(new WhereClauseMatcher("NOT A IS NULL").Match(values));

			var matcher = new WhereClauseMatcher(
				"A IS NOT NULL AND B IS NOT NULL AND C IS NOT NULL AND D IS NULL AND E IS NULL");
			Console.WriteLine(@"Clause: ""{0}""", matcher.Clause);
			Console.WriteLine(@"Engine: {0}", GetDump(matcher));
			Assert.IsTrue(matcher.Match(values));

			matcher = new WhereClauseMatcher(
				"NOT A IS NULL AND NOT B IS NULL AND NOT C IS NULL AND NOT D IS NOT NULL AND NOT E IS NOT NULL");
			Console.WriteLine(@"Clause: ""{0}""", matcher.Clause);
			Console.WriteLine(@"Engine: {0}", GetDump(matcher));
			Assert.IsTrue(matcher.Match(values));

			matcher = new WhereClauseMatcher("E = NULL");
			Console.WriteLine(@"Clause: ""{0}""", matcher.Clause);
			Console.WriteLine(@"Engine: {0}", GetDump(matcher));
			Assert.IsFalse(matcher.Match(values));
		}

		[Test]
		public void CanMatchEmptyClause()
		{
			var values = new NamedValues();

			// By convention, the empty clause matches everything:

			var matcher = new WhereClauseMatcher(string.Empty);
			Console.WriteLine(@"Clause: ""{0}""", matcher.Clause);
			Console.WriteLine(@"Engine: {0}", GetDump(matcher));
			Assert.IsTrue(matcher.Match(values));

			matcher = new WhereClauseMatcher("  \t \t\t\n ");
			Console.WriteLine(@"Clause: ""{0}""", matcher.Clause);
			Console.WriteLine(@"Engine: {0}", GetDump(matcher));
			Assert.IsTrue(matcher.Match(values));
		}

		[Test]
		public void CanMatchEmptyString()
		{
			var values = new NamedValues();
			values.AddValue("Empty", string.Empty);
			values.AddValue("OneBlank", " ");
			values.AddValue("JustNull", DBNull.Value);

			Assert.IsTrue(new WhereClauseMatcher("Empty = ''").Match(values));
			Assert.IsFalse(new WhereClauseMatcher("Empty = ' '").Match(values));
			Assert.IsFalse(new WhereClauseMatcher("Empty IS NULL").Match(values));

			Assert.IsFalse(new WhereClauseMatcher("OneBlank = ''").Match(values));
			Assert.IsTrue(new WhereClauseMatcher("OneBlank = ' '").Match(values));
			Assert.IsFalse(new WhereClauseMatcher("OneBlank IS NULL").Match(values));

			Assert.IsFalse(new WhereClauseMatcher("JustNull = ''").Match(values));
			Assert.IsTrue(new WhereClauseMatcher("JustNull IS NULL").Match(values));
		}

		[Test]
		public void CanIsInAndNotIn()
		{
			var values = new NamedValues();
			values.AddValue("x", 42);

			var matcher = new WhereClauseMatcher("x IN (41, 42, 43)");
			Console.WriteLine(@"Clause: ""{0}""", matcher.Clause);
			Console.WriteLine(@"Engine: {0}", GetDump(matcher));
			Assert.IsTrue(matcher.Match(values));

			matcher = new WhereClauseMatcher("x NOT IN (41, 42, 43)");
			Console.WriteLine(@"Clause: ""{0}""", matcher.Clause);
			Console.WriteLine(@"Engine: {0}", GetDump(matcher));
			Assert.IsFalse(matcher.Match(values));
		}

		[Test]
		public void RegressionTestCOM264()
		{
			// https://issuetracker02.eggits.net/browse/COM-264 "RuleID_1 = 3" failed because of the underbar in "Rule_ID"
			var values = new NamedValues();
			values.AddValue("RuleID_1", 3);
			var matcher = new WhereClauseMatcher("RuleID_1 = 3");
			Assert.IsTrue(matcher.Match(values));
		}

		[Test]
		public void CanCompareAcrossNumericTypes()
		{
			var values = new NamedValues();
			values.AddValue("short", (short) 123);
			values.AddValue("int", 123);
			values.AddValue("long", (long) 123);
			values.AddValue("uint", (uint) 123);
			values.AddValue("float", (float) 123.0);
			values.AddValue("double", 123.0);

			var matcher = new WhereClauseMatcher(
				"short = 123 AND int = 123 AND long = 123 AND uint = 123 AND float = 123 AND double = 123");
			Assert.IsTrue(matcher.Match(values));

			matcher = new WhereClauseMatcher("short = int AND int = long AND short = long");
			Assert.IsTrue(matcher.Match(values));

			matcher = new WhereClauseMatcher("int = uint");
			Assert.IsTrue(matcher.Match(values));

			matcher = new WhereClauseMatcher("float = 123 AND double = 123 AND float = double");
			Assert.IsTrue(matcher.Match(values));

			matcher = new WhereClauseMatcher("short = float AND float = int AND int = double");
			Assert.IsTrue(matcher.Match(values));
		}

		[Test]
		public void CanNullEqualsNull()
		{
			// In a database, NULL = NULL is false.
			// Ensure we match this interpretation.

			var values = new NamedValues();
			values.AddValue("TheValueNull", null);
			values.AddValue("DatabaseNull", DBNull.Value);

			Assert.IsFalse(new WhereClauseMatcher("NULL = NULL").Match(values));
			Assert.IsTrue(new WhereClauseMatcher("NULL IS NULL").Match(values));

			Assert.IsFalse(new WhereClauseMatcher("0 IS NULL").Match(values));
			Assert.IsTrue(new WhereClauseMatcher("0 IS NOT NULL").Match(values));

			Assert.IsTrue(new WhereClauseMatcher("TheValueNull IS NULL").Match(values));
			Assert.IsTrue(new WhereClauseMatcher("DatabaseNull IS NULL").Match(values));

			Assert.IsFalse(new WhereClauseMatcher("TheValueNull = DatabaseNull").Match(values));
			Assert.IsFalse(new WhereClauseMatcher("TheValueNull = TheValueNull").Match(values));
			Assert.IsFalse(new WhereClauseMatcher("DatabaseNull = DatabaseNull").Match(values));

		}

		[Test]
		public void CanValidate()
		{
			var matcher = new WhereClauseMatcher("A = 1 AND 2 = B");

			Assert.True(matcher.Validate(new[] { "A", "B", "C" }));

			Assert.False(matcher.Validate(new[] { "C" }));

			Assert.False(matcher.Validate(Array.Empty<string>()));

			Assert.False(matcher.Validate(null));
		}

		#region Private helpers

		private static string GetDump(WhereClauseMatcher matcher)
		{
			var sb = new StringBuilder();
			matcher.DumpProgram(sb);
			return sb.ToString();
		}

		private class NamedValues : INamedValues
		{
			private readonly IDictionary<string, object> _values;

			public NamedValues()
			{
				_values = new Dictionary<string, object>();
			}

			public void AddValue(string name, object value)
			{
				_values.Add(name, value);
			}

			public bool Exists(string name)
			{
				return _values.ContainsKey(name);
			}

			public object GetValue(string name)
			{
				return _values[name];
			}
		}

		#endregion
	}
}
