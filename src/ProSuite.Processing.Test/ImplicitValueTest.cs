using System;
using NUnit.Framework;
using ProSuite.Processing.Evaluation;
using ProSuite.Processing.Test.Mocks;

namespace ProSuite.Processing.Test
{
	[TestFixture]
	public class ImplicitValueTest
	{
		[Test]
		public void CanCreateExpr()
		{
			var expr1 = ImplicitValue.Create("'abc'");
			object value1 = expr1.Evaluate();

			Assert.IsInstanceOf<string>(value1);
			Assert.AreEqual("abc", (string) value1);
			Assert.False(expr1.IsMissing);

			var expr2 = ImplicitValue.Create("1+5.0/2");
			object value2 = expr2.Evaluate();

			Assert.IsFalse(expr2.IsMissing);
			Assert.IsInstanceOf<double>(value2);
			Assert.AreEqual(3.5, (double) value2);

			// Invalid expressions throw FormatException on creation:
			var ex3 = Assert.Catch<EvaluationException>(() => ImplicitValue.Create("12 + * 3"));
			Console.WriteLine(@"Expected exception: {0}", ex3.Message);
		}

		[Test]
		public void CanCreateDefault()
		{
			var expr1 = ImplicitValue.Create(null);
			Assert.True(expr1.IsMissing);
			Assert.IsNull(expr1.Evaluate());
			Assert.AreEqual(999, expr1.Evaluate<int>(999));

			var expr2 = ImplicitValue.Create(string.Empty);
			Assert.True(expr2.IsMissing);
			Assert.IsNull(expr2.Evaluate());
			Assert.AreEqual(999, expr2.Evaluate<int>(999));

			var expr3 = ImplicitValue.Create("  \t\n  ");
			Assert.True(expr3.IsMissing);
			Assert.IsNull(expr3.Evaluate());
			Assert.AreEqual(999, expr3.Evaluate<int>(999));
		}

		[Test]
		public void CanCreateConstant()
		{
			var expr1 = ImplicitValue.CreateConstant(12.34);
			Assert.IsFalse(expr1.IsMissing);
			Assert.AreEqual(12.34, expr1.Evaluate());
			Assert.AreEqual(12.34, expr1.Evaluate<double>(9.9));

			var expr2 = ImplicitValue.CreateConstant("hello");
			Assert.AreEqual("hello", expr2.Evaluate());

			var expr3 = ImplicitValue.CreateConstant(false);
			Assert.AreEqual(false, expr3.Evaluate());
			Assert.AreEqual(false, expr3.Evaluate<bool>(true));

			var expr4 = ImplicitValue.CreateConstant(null);
			Assert.IsNull(expr4.Evaluate());
		}

		[Test]
		public void CanCastResult()
		{
			var expr1 = ImplicitValue.Create("LENGTH('abc')");
			int value1 = expr1.Evaluate<int>();
			Assert.AreEqual(3, value1);

			var expr2 = ImplicitValue.Create("DECODE(1, 1, true, 2, false)");
			bool value2 = expr2.Evaluate<bool>();
			Assert.IsTrue(value2);

			var expr3 = ImplicitValue.Create("DECODE(3, 1, true, 2, false)");
			Assert.IsNull(expr3.Evaluate());
			bool value3 = expr3.Evaluate<bool>(false); // return false if null
			Assert.IsFalse(value3);

			var expr4 = ImplicitValue.Create("5/2");
			double value4a = expr4.Evaluate<double>();
			Assert.AreEqual(2.5, value4a);
			float value4b = expr4.Evaluate<float>();
			Assert.AreEqual(2.5f, value4b);
			int value4c = expr4.Evaluate<int>();
			Assert.AreEqual(2, value4c);

			var expr5 = ImplicitValue.Create("CONCAT(123)");
			Assert.IsInstanceOf<string>(expr5.Evaluate());
			int value5 = expr5.Evaluate<int>();
			Assert.AreEqual(123, value5);

			var expr6 = ImplicitValue.Create("CONCAT(-2.5)");
			Assert.IsInstanceOf<string>(expr6.Evaluate());
			double value6a = expr6.Evaluate<double>();
			Assert.AreEqual(-2.5, value6a);
			float value6b = expr6.Evaluate<float>();
			Assert.AreEqual(-2.5f, value6b);
		}

		[Test]
		public void CanHandleNullResults()
		{
			var expr1 = ImplicitValue.Create("null");
			Assert.IsNull(expr1.Evaluate());
			Assert.AreEqual(false, expr1.Evaluate<bool>(false));
			Assert.AreEqual(0.0, expr1.Evaluate<double>(0.0));
			Assert.AreEqual(0.0f, expr1.Evaluate<float>(0.0f));
			Assert.AreEqual("quux", expr1.Evaluate() ?? "quux");

			var ex1 = Assert.Catch<EvaluationException>(() => expr1.Evaluate<int>());
			Console.WriteLine(@"Expected exception: {0}", ex1.Message);
		}

		[Test]
		public void CanCatchErrors()
		{
			var expr1 = ImplicitValue.Create("null");
			var ex1 = Assert.Catch(() => expr1.Evaluate<int>());
			Console.WriteLine(@"Expected: {0}", ex1.Message);

			var expr2 = ImplicitValue.Create("'foo'");
			var ex2 = Assert.Catch(() => expr2.Evaluate<double>());
			Console.WriteLine(@"Expected: {0}", ex2.Message);

			// Beware that this 'cast' will succeed:
			var expr3 = ImplicitValue.Create("'123'");
			Assert.AreEqual(123, expr3.Evaluate<int>());

			// But this 'cast' to int will fail:
			var expr4 = ImplicitValue.Create("'-2.5'");
			var ex4 = Assert.Catch(() => expr4.Evaluate<int>());
			Console.WriteLine(@"Expected: {0}", ex4.Message);
			Assert.AreEqual(-2.5, expr4.Evaluate<double>());
		}

		[Test]
		public void CanReferToEnvironment()
		{
			var expr1 = ImplicitValue.Create("DECODE(foo, 'one', 1, 'two', 2, 99)");

			expr1.DefineValue("foo", 123);
			Assert.AreEqual(99, expr1.Evaluate<int>());

			expr1.DefineValue("foo", "one");
			Assert.AreEqual(1, expr1.Evaluate<int>());
			expr1.DefineValue("foo", "two");
			Assert.AreEqual(2, expr1.Evaluate<int>());

			expr1.ForgetValue("foo");
			var ex1 = Assert.Catch(() => expr1.Evaluate<int>());
			Console.WriteLine(@"Expected: {0}", ex1.Message);

			// Fields: foo="one", bar="two"
			var row = new RowValuesMock("foo", "bar");
			row.SetValues("one", "two");

			var expr2 = ImplicitValue.Create("CONCAT(foo, bar)");
			expr2.DefineFields(row, "qualifier");
			expr2.DefineValue("bar", "stop"); // takes precedence over unqualified field bar

			Assert.AreEqual("onestop", expr2.Evaluate());
		}

		[Test]
		public void CanStaticNullValue()
		{
			Assert.AreSame(ImplicitValue.Null, ImplicitValue.Null);

			Assert.IsNull(ImplicitValue.Null.Evaluate());
			Assert.AreEqual(0.0, ImplicitValue.Null.Evaluate<double>(0.0));
			Assert.AreEqual(123, ImplicitValue.Null.Evaluate<int>(123));
		}
	}
}
