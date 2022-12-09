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
		}

		[Test]
		public void CanCreateLiteral()
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
		}

		[Test]
		public void CanImplicitCast()
		{
			var env = new StandardEnvironment();

			var e1 = (ImplicitValue<string>) "foo"; // as expr, not as string!
			Assert.AreEqual("foo", e1.Expression);
			Assert.AreEqual("bar", e1.Evaluate(env.ForgetAll().DefineValue("bar", "foo")));

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
		}

		[Test]
		public void CanReferToEnvironment()
		{
			var env = new StandardEnvironment();

			var e1 = new ImplicitValue<int>("DECODE(foo, 'one', 1, 'two', 2, 99)");

			env.DefineValue(123, "foo");
			Assert.AreEqual(99, e1.Evaluate(env));

			env.DefineValue("one", "foo");
			Assert.AreEqual(1, e1.Evaluate(env));

			env.DefineValue("two", "foo");
			Assert.AreEqual(2, e1.Evaluate(env));

			env.ForgetValue("foo"); // e1 now refers to undefined value
			Assert.Catch<EvaluationException>(() => e1.Evaluate(env));

			// Fields: foo="one", bar="two"
			var row = new RowValuesMock("foo", "bar");
			row.SetValues("one", "two");

			var e2 = new ImplicitValue<string>("CONCAT(foo, bar)");
			env.ForgetAll().DefineFields(row, "qualifier");
			env.DefineValue("stop", "bar"); // takes precedence over unqualified field bar
			Assert.AreEqual("onestop", e2.Evaluate(env));
		}
	}
}
