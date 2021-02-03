using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using ProSuite.Processing.Evaluation;

namespace ProSuite.Processing.Test
{
	[TestFixture]
	public class ExpressionEvaluatorTest
	{
		#region Tokenizer

		[Test]
		public void CanTokenize()
		{
			const string text = "f(123*-foo, 'it''s'+\"\\ntime\")\t";

			int index = 0;
			var sb = new StringBuilder();

			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Name, "f");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Other, "(");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Number, 123);
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Other, "*");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Other, "-");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Name, "foo");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Other, ",");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.White, null);
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.String, "it's");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Other, "+");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.String, "\ntime");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Other, ")");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.White, null);
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.End, null);

			// Idempotence at end of input:

			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.End, null);
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.End, null);
		}

		[Test]
		public void CanTokenizeOperators()
		{
			const string text = "!!=>+++==>==>?????**-";

			int index = 0;
			var sb = new StringBuilder();

			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Other, "!");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Other, "!=");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Other, ">");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Other, "++");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Other, "+");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Other, "==");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Other, ">=");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Other, "=>");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Other, "??");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Other, "??");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Other, "?");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Other, "*");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Other, "*");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.Other, "-");
			AssertToken(text, ref index, sb, ExpressionEvaluator.TokenType.End, null);
		}

		[Test]
		public void CanCatchTokenErrors()
		{
			var sb = new StringBuilder();

			int i1 = 0; // invalid input character
			Catch(() => ExpressionEvaluator.ScanToken("\0", ref i1, out _, sb));

			int i2 = 0; // unterminated string
			Catch(() => ExpressionEvaluator.ScanToken("\"foo", ref i2, out _, sb));

			int i3 = 0; // control character in string
			Catch(() => ExpressionEvaluator.ScanToken("\"foo\nbar\"", ref i3, out _, sb));

			int i4 = 0; // unknown escape sequence
			Catch(() => ExpressionEvaluator.ScanToken("\"\\x\"", ref i4, out _, sb));

			int i5 = 0; // invalid numeric token
			Catch(() => ExpressionEvaluator.ScanToken("123x", ref i5, out _, sb));

			int i9 = 0; // TODO This really should be supported! (but presently isn't)
			Catch(() => ExpressionEvaluator.ScanToken("\"\\u0000\"", ref i9, out _, sb));
		}

		private static void AssertToken(
			string text, ref int index, StringBuilder sb,
			ExpressionEvaluator.TokenType expectedType, object expectedValue)
		{
			var actualType =
				ExpressionEvaluator.ScanToken(text, ref index, out object actualValue, sb);

			Assert.AreEqual(expectedType, actualType);
			Assert.AreEqual(expectedValue, actualValue);
		}

		#endregion

		[Test]
		public void CanEvaluatePrimitives()
		{
			var env = new StandardEnvironment();
			env.DefineValue("foo", "foo");
			env.DefineValue("_foo_bar", "_foo_bar");

			// Symbols
			Assert.AreEqual("foo", Evaluate("foo", env));
			Assert.AreEqual("foo", Evaluate("  foo  ", env));
			Assert.AreEqual("_foo_bar", Evaluate("_foo_bar", env));

			// Strings
			Assert.AreEqual("foo'bar", Evaluate("'foo''bar'", env));
			Assert.AreEqual("foo\"bar\"\n\'/\\", Evaluate(@"""foo\""bar\""\n\'\/\\""", env));

			// Numbers
			Assert.AreEqual(123, Evaluate("123"));
			Assert.AreEqual(1.23, Evaluate("1.23"));
			Assert.AreEqual(2.7e-5, Evaluate("2.7e-5"));

			// null and Booleans
			Assert.AreEqual(null, Evaluate("null"));
			Assert.AreEqual(false, Evaluate("false"));
			Assert.AreEqual(true, Evaluate("true"));

			// Excessive parens...
			Assert.AreEqual(42, Evaluate("(((23+19)))"));
		}

		[Test]
		public void CanCaseInSensitive()
		{
			const string name = "fAlSe";

			// When ignoring case, "fAlSe" will be recognized as the built-in value false:

			var envIgnoreCase = new StandardEnvironment();
			envIgnoreCase.DefineValue(name, "TheEnv");
			Assert.AreEqual(false, Dump(ExpressionEvaluator.Create("fAlSe")).Evaluate(envIgnoreCase));

			// When respecting case, "fAlSe" is just a name to be looked up in the environment:

			var envRespectCase = new StandardEnvironment(false);
			envRespectCase.DefineValue(name, "TheEnv");
			Assert.AreEqual("TheEnv", Dump(ExpressionEvaluator.Create("fAlSe", false)).Evaluate(envRespectCase));
		}

		[Test]
		public void CanEvaluateUnaryOps()
		{
			Assert.AreEqual(5, Evaluate("+5"));
			Assert.AreEqual(-5, Evaluate("+-5"));
			Assert.AreEqual(25, Evaluate("+ + +25"));
			Assert.AreEqual(-0.2, Evaluate(" - 0.2 "));
			Assert.AreEqual(-0.2, Evaluate("- - -0.2"));
			Assert.AreEqual(-1.2e-03, Evaluate("-1.2e-03"));

			Assert.AreEqual(false, Evaluate("not true"));
			Assert.AreEqual(true, Evaluate("not not true"));
			Assert.AreEqual(true, Evaluate("not not 5"));

			var env = new StandardEnvironment();
			env.DefineValue("foo", 42.0);

			Assert.AreEqual(42.0, Evaluate("foo", env));
			Assert.AreEqual(false, Evaluate("not foo", env));
			Assert.AreEqual(true, Evaluate("not not foo", env));
			Assert.AreEqual(false, Evaluate("not not not foo", env));

			Assert.AreEqual(5, Evaluate("+ +5"));
			Catch(() => Evaluate("++5"));

			Assert.AreEqual(25, Evaluate("- - 25"));
			Catch(() => Evaluate("-- 25")); // -- is one token
		}

		[Test]
		public void CanCatchSyntaxErrors()
		{
			Catch(() => Evaluate(null));

			Catch(() => Evaluate("$"));

			Catch(() => Evaluate("23 + "));
		}

		[Test]
		public void CanEvaluatePlus()
		{
			// Plus: if both operands are numeric, do arithmetic
			// addition, otherwise convert to string and concat.
			Assert.AreEqual(7, Evaluate("2.5 + 4.5"));
			Assert.AreEqual("hi", Evaluate("'h'+\"i\""));
			Assert.AreEqual("f00", Evaluate("'f'+0+0.0"));
		}

		[Test]
		public void CanEvaluateArithmetic()
		{
			Assert.AreEqual(-7, Evaluate(" 1 - 3 - 5 "));
			Assert.AreEqual(3, Evaluate("1-(3-5)"));
			Assert.AreEqual(2, Evaluate("1-2+3"));

			Assert.AreEqual(-6.28, Evaluate("3.14*-2")); // *- is two tokens!

			Assert.AreEqual(0, Evaluate("12-3*4"));
			Assert.AreEqual(36, Evaluate("(12-3)*4"));

			var env = new StandardEnvironment();
			env.DefineValue("LB", 0.46);

			Assert.IsTrue(Math.Abs((double) Evaluate("LB/2", env)) - 0.23 < double.Epsilon);
			Assert.IsTrue(Math.Abs((double) Evaluate("0.5*LB", env)) - 0.23 < double.Epsilon);
		}

		[Test]
		public void CanEvaluateInvocations()
		{
			var env = new StandardEnvironment();

			// The lambdas below generate non-static methods and upon calling them
			// from the evaluator we get a TargetException "Object does not match
			// target type" (which is true: object is the env, target type is the
			// compiler-generated class).
			// Why did this work before? Did the compiler create static methods?
			// My hypothesis: C# 4.0 (with VS 2010) vs C# 5.0 (with VS 2012).
			//
			//env.Register("foo", () => "foo");
			//env.Register<object, object>("id", v => v);
			//env.Register<double, double, double>("pyth", (x, y) => Math.Sqrt(x * x + y * y));
			//
			// Workaround is to write "real" (non-lambda) static methods...

			env.Register("foo", Foo);
			env.Register<object, object>("id", Identity);
			env.Register<double, double, double>("pyth", Pythagoras);
			env.Register("bomb", Bomb);

			var r1 = Evaluate("foo()", env);
			Console.WriteLine(r1);
			Assert.AreEqual("foo", r1);

			var r2 = Evaluate("id(42)", env);
			Console.WriteLine(r2);
			Assert.AreEqual(42, r2);

			var r3 = Evaluate("pyth(1+2, 10-2*3)", env);
			Console.WriteLine(r3);
			Assert.AreEqual(5.0, r3);

			var r4 = Evaluate("id(1) + pyth(-1+4, 2*2)", env);
			Console.WriteLine(r4);
			Assert.AreEqual(6.0, r4);

			var r5 = Evaluate("pyth(3, null)", env);
			Console.WriteLine(@"pyth(3, null) == {0} (the null becomes 0.0)", r5);
			Assert.AreEqual(3.0, r5);

			var ex6 = Assert.Catch<EvaluationException>(() => Evaluate("null('foo')", env));
			Console.WriteLine(@"Expected exception: {0}", ex6.Message);

			var ex7 = Assert.Catch<EvaluationException>(() => Evaluate("'oops'(3)", env));
			Console.WriteLine(@"Expected exception: {0}", ex7.Message);

			// Exception in invocate target's code is wrapped in a EvaluationException:
			var ex8 = Assert.Catch<EvaluationException>(() => Evaluate("bomb()", env));
			Console.WriteLine(@"Expected exception: {0}", ex8.Message);
			Assert.NotNull(ex8.InnerException, "ex.InnerException is null");
			Assert.AreEqual("TESTING", ex8.InnerException.Message);
		}

		#region Static methods for the test environment

		private static string Foo() { return "foo"; }
		private static object Identity(object x) { return x; }
		private static double Pythagoras(double x, double y) { return Math.Sqrt(x * x + y * y); }
		private static object Bomb() { throw new Exception("TESTING"); }

		#endregion

		[Test]
		public void CanEvaluateVariadicFunction()
		{
			var env = new StandardEnvironment();

			var r1 = Evaluate("MIN(3,8,1,5)", env);
			Console.WriteLine(r1);
			Assert.AreEqual(1, r1);

			var r2 = Evaluate("MIN(123)", env);
			Console.WriteLine(r2);
			Assert.AreEqual(123, r2);
		}

		[Test]
		public void CanParseQualifiedStuff()
		{
			var env = new QualifiedTestEnvironment();

			ExpressionEvaluator.Create("input.LB");
			ExpressionEvaluator.Create("input . LB"); // not recommended, but might happen

			Assert.AreEqual("Foo (input)", Evaluate("input.Foo", env));
			Assert.AreEqual("Foo (other)", Evaluate("other.Foo", env));
			Catch(() => Evaluate("Foo", env));

			Assert.AreEqual("Bar (input)", Evaluate("Bar", env));
			Assert.AreEqual("Bar (input)", Evaluate("input.Bar", env));
			Catch(() => Evaluate("other.Bar", env));
		}

		[Test]
		public void CanEvaluateBinaryComparisons()
		{
			Assert.AreEqual(true, Evaluate("1 < 2"));

			Assert.AreEqual(false, Evaluate("23 > 23"));

			Assert.AreEqual(true, Evaluate("23 >= 23"));

			Assert.AreEqual(false, Evaluate("23 = 42"));

			Assert.AreEqual(true, Evaluate("23 <> 42"));

			Assert.AreEqual(true, Evaluate("false < true"));
			Assert.AreEqual(false, Evaluate("null < 123")); // true

			Assert.AreEqual(true, Evaluate("'bar' < 'foo'"));
			Assert.AreEqual(true, Evaluate("'bar' < 'barf'"));
			Assert.AreEqual(true, Evaluate("'A' < 'a'")); // we do simple ordinal comparison
		}

		[Test]
		public void CanEvaluateChainedComparisons()
		{
			Assert.AreEqual(true, Evaluate("1 < 2 < 3"));
			Assert.AreEqual(true, Evaluate("1 < 3 > 2")); // no info about 1 op 2
			Assert.AreEqual(true, Evaluate("1 < 2 < 2+1 = 4-1 <= 9.9 <> 8.8 <> 6 = 2*3"));

			Assert.AreEqual(true, Evaluate("false < true < -0.3 < 0 < 3.14 < 9e7 < '' < 'Bye!'"));
			// Any comparison with null is false:
			Assert.AreEqual(false, Evaluate("false < true < -0.3 < 0 < 3.14 < 'foo' <> null"));
		}

		[Test]
		public void CanEvaluateIsOperator()
		{
			Assert.AreEqual(true, Evaluate("null is null"));
			Assert.AreEqual(false, Evaluate("null is not null"));
			Assert.AreEqual(true, Evaluate("true is boolean"));
			Assert.AreEqual(false, Evaluate("true is not boolean"));
			Assert.AreEqual(true, Evaluate("12.3 is number"));
			Assert.AreEqual(false, Evaluate("12.3 is not number"));
			Assert.AreEqual(true, Evaluate("'foo' is string"));
			Assert.AreEqual(false, Evaluate("'foo' is not string"));

			Catch(() => Evaluate("42 not is boolean")); // syntax error
		}

		[Test]
		public void CanEvaluateInOperator()
		{
			Assert.AreEqual(true, Evaluate("42 in (7,42,81)"));
			Assert.AreEqual(false, Evaluate("42 not in (7,42,81)"));
			Assert.AreEqual(false, Evaluate("42 in (1,2,3)"));
			Assert.AreEqual(true, Evaluate("42 not in (1,2,3)"));
			Assert.AreEqual(true, Evaluate("42 in (42)"));
			Assert.AreEqual(false, Evaluate("42 not in (42)"));

			// We accept the empty list '()', SQL doesn't:
			Assert.AreEqual(false, Evaluate("42 in ()"));

			// A in (B,C) is short for A = B or A = C, therefore:
			Assert.AreEqual(false, Evaluate("null in (null)"));
		}

		[Test]
		public void CanEvaluateLogicalBinops()
		{
			// Notice: The eol comments show the expected results for
			// shortcutting logical AND and OR (where they differ from 3VL).

			Assert.AreEqual(true, Evaluate("3 and 5")); // 5
			Assert.AreEqual(true, Evaluate("3 or 5")); // 3

			Assert.AreEqual(true, Evaluate("3 and 5 and 7 and 9")); // 9
			Assert.AreEqual(true, Evaluate("3 or 5 or 7 or 9")); // 3

			Assert.AreEqual(true, Evaluate("null or false or 'foo' or 'bar'")); // "foo"

			Assert.AreEqual(true, Evaluate("false and true or true"));
			Assert.AreEqual(false, Evaluate("false and (true or true)"));

			Assert.AreEqual(true, Evaluate("1+1=2 and 4-2 and 1 < 2 <= 1 or false or 2*3")); // 6
		}

		[Test]
		public void CanEvaluateConditionalOperator()
		{
			Assert.AreEqual("T", Evaluate("true ? 'T' : 'F'"));
			Assert.AreEqual("F", Evaluate("false ? 'T' : 'F'"));
			Assert.AreEqual(null, Evaluate("null ? 'T' : 'F'"));

			var env = new StandardEnvironment();
			env.DefineValue("xyzzy", "Magic");
			env.DefineValue("d", 5);

			Assert.AreEqual(5, Evaluate("xyzzy ? length(xyzzy) : -1", env));
			Assert.AreEqual("Fri", Evaluate("d=0 ? 'Sun' : d=1 ? 'Mon' : d=2 ? 'Tue' : d=3 ? 'Wed' : d=4 ? 'Thu' : d=5 ? 'Fri' : d=6 ? 'Sat' : null", env));
		}

		[Test]
		public void CanEvaluateNullCoalescingOperator()
		{
			Assert.AreEqual("foo", Evaluate("'foo' ?? 'bar'"));
			Assert.AreEqual("bar", Evaluate("null ?? 'bar'"));

			// Here's how ?? is different from || (logical or):
			Assert.AreEqual(false, Evaluate("false ?? 'bar'"));
			Assert.AreEqual(true, Evaluate("false or 'bar'")); // "bar"

			// ?? binds tighter than ?:
			var env = new StandardEnvironment();
			env.DefineValue("nothing", null);
			Assert.AreEqual("alt", Evaluate("nothing ?? false ? null ?? 'consequent' : 'alt' ?? 'alternative'", env));
		}

		[Test]
		public void CanEvaluateNull()
		{
			// Collection of various evaluations involving null.
			// The eol comment shows the shortcutting result,
			// where it differs from 3VL.

			Assert.IsNull(Evaluate("null"));

			Assert.AreEqual(false, Evaluate("null = null")); // true
			Assert.AreEqual(false, Evaluate("null <> null"));

			// Null propagates through negation and arith:
			Assert.IsNull(Evaluate("not null"));
			Assert.IsNull(Evaluate("not not null"));
			Assert.IsNull(Evaluate("3+null"));
			Assert.IsNull(Evaluate("3 and null"));

			// But null is false in logical binops:
			Assert.AreEqual(true, Evaluate("null or 3")); // 3
			Assert.AreEqual(null, Evaluate("null ? 'foo' : 'bar'")); // "bar"
		}

		[Test]
		public void CanEvaluateNullArithmetic()
		{
			// Arithmetic operations with null are null:
			Assert.IsNull(Evaluate("3 + null"));
			Assert.IsNull(Evaluate("null - 15"));
			Assert.IsNull(Evaluate("3+null*2"));
			Assert.IsNull(Evaluate("0/null"));
			Assert.IsNull(Evaluate("7%null"));

			// However, null/0 is still a division by zero!
			var ex1 = Assert.Catch<EvaluationException>(() => Evaluate("null/0"));
			Console.WriteLine(@"Expected exception: {0}", ex1.Message);
			var ex2 = Assert.Catch<EvaluationException>(() => Evaluate("null%0"));
			Console.WriteLine(@"Expected exception: {0}", ex2.Message);
		}

		[Test]
		public void CanEvaluateNullLogic()
		{
			Assert.IsNull(Evaluate("null"));
			Assert.IsNull(Evaluate("not null"));
			Assert.IsNull(Evaluate("not not null"));

			// Presently, the evaluator implements shortcutting logical and/or,
			// that is, it does not evaluate the 2nd argument if the result is
			// defined by the first (false && x is false, whatever x is). This
			// requires traditional 2-valued logic.  SQL, however, has 3-valued
			// logic, which requires both arguments to be evaluated.

			// With shortcutting:
			// A and B  is  if A then B else A
			// A or  B  is  if A then A else B

			//Assert.AreEqual(null, Evaluate("null && null"));
			//Assert.AreEqual(null, Evaluate("null && false"));
			//Assert.AreEqual(false, Evaluate("false && null"));
			//Assert.AreEqual(null, Evaluate("null && true"));
			//Assert.AreEqual(null, Evaluate("true && null"));

			//Assert.AreEqual(null, Evaluate("null || null"));
			//Assert.AreEqual(false, Evaluate("null || false"));
			//Assert.AreEqual(null, Evaluate("false || null"));
			//Assert.AreEqual(true, Evaluate("null || true"));
			//Assert.AreEqual(true, Evaluate("true || null"));

			// In 3-valued logic:
			// null and false is false, because anything and false is false
			// null or true is true, because anything or true is true

			Assert.AreEqual(null, Evaluate("null and null"));
			Assert.AreEqual(false, Evaluate("null and false"));
			Assert.AreEqual(false, Evaluate("false and null"));
			Assert.AreEqual(null, Evaluate("null and true"));
			Assert.AreEqual(null, Evaluate("true and null"));

			Assert.AreEqual(null, Evaluate("null or null"));
			Assert.AreEqual(null, Evaluate("null or false"));
			Assert.AreEqual(null, Evaluate("false or null"));
			Assert.AreEqual(true, Evaluate("null or true"));
			Assert.AreEqual(true, Evaluate("true or null"));

			// To have 3-valued logic (as in SQL), we need to completely
			// rewrite the code generation for AND and OR, and we want
			// IEvaluationEnvironment.And(x,y) and IEvaluationEnvironment.Or(x,y).
		}

		[Test]
		public void CanEvaluateNullRelational()
		{
			// All comparisons with null are false, because null is not comparable.
			// This is quite different from the (IMHO more natural) treatment of
			// null as the least element.

			Assert.AreEqual(false, Evaluate("null = null"));
			Assert.AreEqual(false, Evaluate("null <> null"));

			Assert.AreEqual(false, Evaluate("null < null"));
			Assert.AreEqual(false, Evaluate("null < false"));
			Assert.AreEqual(false, Evaluate("null < true"));
			Assert.AreEqual(false, Evaluate("null < -99"));
			Assert.AreEqual(false, Evaluate("null < 12.3"));
			Assert.AreEqual(false, Evaluate("null < 'foo'"));

			Assert.AreEqual(false, Evaluate("null <= null"));
			Assert.AreEqual(false, Evaluate("null <= false"));
			Assert.AreEqual(false, Evaluate("null <= true"));
			Assert.AreEqual(false, Evaluate("null <= -99"));
			Assert.AreEqual(false, Evaluate("null <= 12.3"));
			Assert.AreEqual(false, Evaluate("null <= 'foo'"));

			Assert.AreEqual(false, Evaluate("null > null"));
			Assert.AreEqual(false, Evaluate("null > false"));
			Assert.AreEqual(false, Evaluate("null > true"));
			Assert.AreEqual(false, Evaluate("null > -99"));
			Assert.AreEqual(false, Evaluate("null > 12.3"));
			Assert.AreEqual(false, Evaluate("null > 'foo'"));

			// Test chained relations involving null (because
			// for the binary case optimized code is emitted):

			Assert.AreEqual(false, Evaluate("null < 1 < 2"));
			Assert.AreEqual(false, Evaluate("1 < null < 2"));
			Assert.AreEqual(false, Evaluate("1 < 2 < null"));
			Assert.AreEqual(false, Evaluate("1 = null = 1"));
			Assert.AreEqual(false, Evaluate("1 <> 2 <> null"));
		}

		[Test]
		public void CanDivideByZero()
		{
			// The standard environment throws an exception on divide by zero
			// (rather than returning some type of double infinity or null):

			var ex1 = Assert.Catch<EvaluationException>(() => Evaluate("3.7/0.0"));
			Console.WriteLine(@"Expected exception: {0}", ex1.Message);

			var ex2 = Assert.Catch<EvaluationException>(() => Evaluate("-3/0"));
			Console.WriteLine(@"Expected exception: {0}", ex2.Message);

			var ex3 = Assert.Catch<EvaluationException>(() => Evaluate("null/0")); // sic
			Console.WriteLine(@"Beware: null/0 gives: {0}", ex3.Message);
			Assert.IsNull(Evaluate("null/2"));
			Assert.IsNull(Evaluate("2/null"));
		}

		[Test]
		public void CanParseSubstring()
		{
			const string text = "Hello ; 1+2*3  ";
			
			const int index1 = 0;
			var ee1 = ExpressionEvaluator.Create(text, index1, out int length1);
			Assert.AreEqual(6, length1); // white space included
			Assert.AreEqual("Hello", ee1.Clause); // white space excluded
			// text[index1+length1] should be the semicolon between the exprs:
			Assert.AreEqual(';', text[index1 + length1]);

			const int index2 = 7;
			var ee2 = ExpressionEvaluator.Create(text, index2, out int length2);
			Assert.AreEqual(8, length2); // white space included
			Assert.AreEqual("1+2*3", ee2.Clause); // white space excluded
			// Regardless of trailing white space, index2+length2 is text.Length:
			Assert.AreEqual(text.Length, index2 + length2);
		}

		#region Real world tests

		[Test(Description = "Replacement for the hard-coded OU logic")]
		public void CanOverUnderExpression()
		{
			// CreateCrossingMasks: qualifiers are "input" and "crossing"
			// The values used here are valid for DKM25_STRASSE as of June 2015
			// (KUNSTBAUTE values for DKM25_EISENBAHN are different).

			Assert.AreEqual(false, IsAbove(100, 0, 100, 0)); // Keine 0, Keine 0
			Assert.AreEqual(true, IsAbove(100, 0, 100, 1)); // Keine 0, Keine 1
			Assert.AreEqual(false, IsAbove(200, -5, 100, 2)); // Brücke -5, Keine 2
			Assert.AreEqual(true, IsAbove(700, 5, 100, -2)); // Tunnel 5, Keine -2

			Assert.AreEqual(true, IsAbove(100, null, 100, 1)); // Keine NULL, Keine 1
			Assert.AreEqual(true, IsAbove(null, 1, null, 2)); // NULL 1, NULL 2
		}

		private static object IsAbove(
			object inputKunstbaute, object inputStufe,
			object crossingKunstbaute, object crossingStufe)
		{
			const string i = "DECODE(input.KUNSTBAUTE," +
			                 " 200,100, 300,100, 400,100, 500,100," +
			                 " 700,-100, 1100,-100, 1200,-100, 0) + (input.STUFE ?? 0)";

			const string c = "DECODE(crossing.KUNSTBAUTE," +
			                 " 200,100, 300,100, 400,100, 500,100," +
			                 " 700,-100, 1100,-100, 1200,-100, 0) + (crossing.STUFE ?? 0)";

			var ie = ExpressionEvaluator.Create(i);
			var ce = ExpressionEvaluator.Create(c);
			var ee = ExpressionEvaluator.Create(string.Format("({0}) < ({1})", i, c));

			var input = new NamedValues
			            {
				            {"KUNSTBAUTE", inputKunstbaute},
				            {"STUFE", inputStufe}
			            };

			var crossing = new NamedValues
			               {
				               {"KUNSTBAUTE", crossingKunstbaute},
				               {"STUFE", crossingStufe}
			               };

			var env = new StandardEnvironment();
			env.DefineFields(input, "input");
			env.DefineFields(crossing, "crossing");

			var iv = ie.Evaluate(env);
			var cv = ce.Evaluate(env);
			var ev = ee.Evaluate(env);

			Console.WriteLine(
				@"Input: {0} level {1}, Crossing: {2} level {3}, iv={4}, cv={5}, above={6}",
				inputKunstbaute, inputStufe, crossingKunstbaute, crossingStufe,
				iv, cv, ev);

			return ev;
		}

		private class NamedValues : Dictionary<string, object>, INamedValues
		{
			public NamedValues() : base(StringComparer.OrdinalIgnoreCase)
			{
			}

			public bool Exists(string name)
			{
				return name != null && ContainsKey(name);
			}

			public object GetValue(string name)
			{
				if (name == null)
					return null;
				return TryGetValue(name, out var value) ? value : null;
			}
		}

		#endregion

		private static object Evaluate(string clause)
		{
			return Dump(ExpressionEvaluator.Create(clause)).Evaluate(null);
		}

		private static object Evaluate(string clause, IEvaluationEnvironment env)
		{
			return Dump(ExpressionEvaluator.Create(clause)).Evaluate(env);
		}

		private static ExpressionEvaluator Dump(ExpressionEvaluator ee)
		{
			Console.WriteLine(@"Clause: {0}", ee.Clause);

			var writer = new StringWriter();
			ee.DumpEngine(writer);
			Console.WriteLine(writer);

			return ee;
		}

		private static void Catch(Action code)
		{
			try
			{
				code();

				Assert.Fail("Expected an exception");
			}
			catch (Exception ex)
			{
				Console.WriteLine(@"Expected: {0}", ex.Message);
			}
		}

		/// <summary>
		/// A test environment that contains the following bindings:
		/// <list type="bullet">
		/// <item>input.Foo = "Foo (input)"</item>
		/// <item>other.Foo = "Foo (other)"</item>
		/// <item>input.Bar = "Bar (input)"</item>
		/// </list>
		/// </summary>
		private class QualifiedTestEnvironment : EnvironmentBase
		{
			public override object Lookup(string name, string qualifier)
			{
				var lookupComparer = StringComparer.OrdinalIgnoreCase;

				if (lookupComparer.Equals(name, "Foo"))
				{
					if (lookupComparer.Equals(qualifier, "input"))
					{
						return "Foo (input)";
					}
					if (lookupComparer.Equals(qualifier, "other"))
					{
						return "Foo (other)";
					}
					throw new Exception("Ambiguous name: Foo");
				}

				if (lookupComparer.Equals(name, "Bar"))
				{
					if (string.IsNullOrEmpty(qualifier) ||
						lookupComparer.Equals(qualifier, "input"))
					{
						return "Bar (input)";
					}
				}

				throw string.IsNullOrEmpty(qualifier)
				      	? new Exception(string.Format("No such field: {0}", name))
				      	: new Exception(string.Format("No such field: {0}.{1}", qualifier, name));
			}

			public override object Invoke(Function target, params object[] args)
			{
				throw new NotSupportedException();
			}
		}
	}
}
