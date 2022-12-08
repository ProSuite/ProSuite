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
			var env = new StandardEnvironment();

			var e1 = new ImplicitValue<string>("'abc'");
			var v1 = e1.Evaluate(env);

			Assert.AreEqual("'abc'", e1.Expression);
			Assert.IsNull(e1.Name);
			Assert.AreEqual("abc", v1);

			var e2 = new ImplicitValue<double>("1+5.0/2").SetName("Test");
			var v2 = e2.Evaluate(env);

			Assert.AreEqual("1+5.0/2", e2.Expression);
			Assert.AreEqual("Test", e2.Name);
			Assert.AreEqual(3.5, v2);

			// Invalid expressions throw a FormatException on creation:
			Assert.Catch<FormatException>(() => new ImplicitValue<double>("12 + * 3"));



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
			var env = new StandardEnvironment();

			var e1 = new ImplicitValue<double>(null);
			Assert.IsNull(e1.Expression);
			Assert.AreEqual(default(double), e1.Evaluate(env));

			var e2 = new ImplicitValue<double>(string.Empty);
			Assert.IsNull(e2.Expression);
			Assert.AreEqual(1234.5, e2.Evaluate(env, 1234.5));

			var e3 = new ImplicitValue<string>("  \t\n  ");
			Assert.IsNull(e3.Expression);
			Assert.AreEqual("nix", e3.Evaluate(env, "nix"));



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
		public void CanCreateConstant() // TODO rename ...Literal
		{
			var env = new StandardEnvironment();

			var e1 = ImplicitValue<double>.Literal(-1234.5);
			Assert.AreEqual("-1234.5", e1.Expression);
			Assert.AreEqual(-1234.5, e1.Evaluate(env));

			var e2 = ImplicitValue<bool>.Literal(true);
			Assert.AreEqual("True", e2.Expression);
			Assert.AreEqual(true, e2.Evaluate(env));

			var e3 = ImplicitValue<string>.Literal("hello");
			Assert.AreEqual("\"hello\"", e3.Expression);
			Assert.AreEqual("hello", e3.Evaluate(env));



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
		public void CanImplicitCast()
		{
			var env = new StandardEnvironment();

			var e1 = (ImplicitValue<string>) "foo"; // as expr, not as string!
			Assert.AreEqual("foo", e1.Expression);
			Assert.AreEqual("bar", e1.Evaluate(env.ForgetAll().DefineValue("foo", "bar")));

			var e2 = (ImplicitValue<string>) null;
			Assert.IsNull(e2);

			var s1 = (string) e1;
			Assert.AreEqual("foo", s1);

			var s2 = (string) e2;
			Assert.IsNull(s2);
		}

		[Test]
		public void CanCastResult()
		{
			var env = new StandardEnvironment();

			var e1 = new ImplicitValue<int>("LENGTH('abc')");
			Assert.AreEqual(3, e1.Evaluate(env));

			var e2 = new ImplicitValue<bool>("DECODE(1, 1, true, 2, false)");
			Assert.AreEqual(true, e2.Evaluate(env));

			var e3 = new ImplicitValue<bool>("DECODE(3, 1, true, 2, false)");
			Assert.IsFalse(e3.Evaluate(env)); // null converts to false

			var e4 = new ImplicitValue<float>("5/2");
			Assert.AreEqual(2.5f, e4.Evaluate(env));

			var e5 = new ImplicitValue<string>("5/2");
			Assert.AreEqual("2.5", e5.Evaluate(env));

			var e6 = new ImplicitValue<int>("5/2");
			Assert.AreEqual(2, e6.Evaluate(env));

			var e7 = new ImplicitValue<double>("'12.5'");
			Assert.AreEqual(12.5, e7.Evaluate(env));

			var e8 = new ImplicitValue<int>("'12.5'");
			Assert.Catch<EvaluationException>(() => e8.Evaluate(env));



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
			var env = new StandardEnvironment();

			var e1 = new ImplicitValue<string>("null");
			Assert.IsNull(e1.Evaluate(env));
			Assert.AreEqual("nix", e1.Evaluate(env, "nix"));

			var e2 = new ImplicitValue<double>("null");
			Assert.AreEqual(0.0, e2.Evaluate(env));
			Assert.AreEqual(999, e2.Evaluate(env, 999));

			var e3 = new ImplicitValue<bool>("null");
			Assert.AreEqual(false, e3.Evaluate(env));
			Assert.AreEqual(true, e3.Evaluate(env, true));

			var e4 = new ImplicitValue<int>("null");
			Assert.AreEqual(0, e4.Evaluate(env));
			Assert.AreEqual(-9, e4.Evaluate(env, -9));



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
			var env = new StandardEnvironment();

			var e2 = new ImplicitValue<double>("'foo'");
			Assert.Catch<EvaluationException>(() => e2.Evaluate(env, 123));

			Assert.Catch<EvaluationException>(() => new ImplicitValue<int>("'-2.5'").Evaluate(env));
			// While the above fails, the next two work:
			Assert.AreEqual(-2.5, new ImplicitValue<double>("'-2.5'").Evaluate(env));
			Assert.AreEqual(-2, new ImplicitValue<int>("-2.5").Evaluate(env));



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
			var env = new StandardEnvironment();

			var e1 = new ImplicitValue<int>("DECODE(foo, 'one', 1, 'two', 2, 99)");

			env.DefineValue("foo", 123);
			Assert.AreEqual(99, e1.Evaluate(env));

			env.DefineValue("foo", "one");
			Assert.AreEqual(1, e1.Evaluate(env));

			env.DefineValue("foo", "two");
			Assert.AreEqual(2, e1.Evaluate(env));

			env.ForgetValue("foo"); // e1 now refers to undefined value
			Assert.Catch<EvaluationException>(() => e1.Evaluate(env));



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

			var e2 = new ImplicitValue<string>("CONCAT(foo, bar)");
			env.ForgetAll().DefineFields(row, "qualifier");
			env.DefineValue("bar", "stop"); // takes precedence over unqualified field bar
			Assert.AreEqual("onestop", e2.Evaluate(env));



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
