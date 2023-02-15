using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using ProSuite.Commons.Collections;
using ProSuite.Processing.Evaluation;

namespace ProSuite.Processing.Test.Evaluation
{
	[TestFixture]
	public class StandardEnvironmentTest
	{
		[Test]
		public void CanLookupRespectCase()
		{
			var envRespectCase = new StandardEnvironment(false);

			envRespectCase.DefineValue("bar", "foo");
			Assert.AreEqual("bar", envRespectCase.Lookup("foo", null));
			Assert.Catch<EvaluationException>(() => envRespectCase.Lookup("FOO", null));
			Assert.Catch<EvaluationException>(() => envRespectCase.Lookup("foo", "qualifier"));

			Assert.IsInstanceOf(typeof(Function), envRespectCase.Lookup("TRIM", null));
			var ex3 = Assert.Catch<EvaluationException>(() => envRespectCase.Lookup("Trim", null));
			Console.WriteLine(@"Expected exception: {0}", ex3.Message);
		}

		[Test]
		public void CanLookupIgnoreCase()
		{
			var envIgnoreCase = new StandardEnvironment();

			envIgnoreCase.DefineValue("bar", "foo");
			Assert.AreEqual("bar", envIgnoreCase.Lookup("foo", null));
			Assert.AreEqual("bar", envIgnoreCase.Lookup("FOO", null));
			var ex1 = Assert.Catch<EvaluationException>(() => envIgnoreCase.Lookup("foo", "qualifier"));
			Console.WriteLine(@"Expected exception: {0}", ex1.Message);

			Assert.IsInstanceOf(typeof(Function), envIgnoreCase.Lookup("TRIM", null));
			Assert.IsInstanceOf(typeof(Function), envIgnoreCase.Lookup("Trim", null));
		}

		[Test]
		public void CanLookupQualified()
		{
			var env = new StandardEnvironment(false);

			env.DefineValue("bar", "foo");

			Assert.AreEqual("bar", env.Lookup("foo", null));
			var ex1 = Assert.Catch<EvaluationException>(() => env.Lookup("foo", "qualifier"));
			Console.WriteLine(@"Expected exception: {0}", ex1.Message);

			Assert.IsInstanceOf<Function>(env.Lookup("TRIM", null));
			var ex2 = Assert.Catch<EvaluationException>(() => env.Lookup("TRIM", "qualifier"));
			Console.WriteLine(@"Expected exception: {0}", ex2.Message);
		}

		[Test]
		public void CannotLookupAmbiguous()
		{
			var env = new StandardEnvironment();
			var one = new ConstantValueTestRow("one", "FOO");
			var two = new ConstantValueTestRow("two", "FOO");

			env.DefineFields(one, "one");
			env.DefineFields(two, "two");

			var ex = Assert.Catch<EvaluationException>(() => env.Lookup("FOO", null));
			Console.WriteLine(@"Expected exception: {0}", ex.Message);

			Assert.AreEqual("one", env.Lookup("FOO", "one"));
			Assert.AreEqual("two", env.Lookup("FOO", "two"));
		}

		[Test]
		public void CanLookupPrecedence()
		{
			// Lookup precedence for unqualified names:
			//   defined value < standard function < field value
			// Qualified names are always looked up on a row of fields
			// defined with the qualifier. CONCAT is a standard function.

			var env = new StandardEnvironment();
			var row = new ConstantValueTestRow("row", "CONCAT", "FOO");
			const string qualifier = "qualifier";

			Assert.Catch<EvaluationException>(() => env.Lookup("FOO", null));
			Assert.IsInstanceOf<Function>(env.Lookup("CONCAT", null));
			Assert.Catch<EvaluationException>(() => env.Lookup("CONCAT", qualifier));

			env.DefineFields(row, qualifier);

			Assert.AreEqual("row", env.Lookup("FOO", null));
			Assert.IsInstanceOf<Function>(env.Lookup("CONCAT", null));
			Assert.AreEqual("row", env.Lookup("CONCAT", qualifier));

			env.DefineValue("foo", "FOO");
			env.DefineValue("value", "CONCAT");

			Assert.AreEqual("foo", env.Lookup("FOO", null));
			Assert.AreEqual("value", env.Lookup("CONCAT", null));
			Assert.AreEqual("row", env.Lookup("CONCAT", qualifier));

			env.ForgetValue("CONCAT");
			env.ForgetValue("FOO");

			Assert.AreEqual("row", env.Lookup("FOO", null));
			Assert.IsInstanceOf<Function>(env.Lookup("CONCAT", null));
			Assert.AreEqual("row", env.Lookup("CONCAT", qualifier));

			env.ForgetFields("qualifier");

			Assert.Catch<EvaluationException>(() => env.Lookup("FOO", null));
			Assert.IsInstanceOf<Function>(env.Lookup("CONCAT", null));
			Assert.Catch<EvaluationException>(() => env.Lookup("CONCAT", qualifier));
		}

		private class ConstantValueTestRow : INamedValues
		{
			private readonly object _value;
			private readonly string[] _names;

			/// <summary>
			/// Create a named values ("row") instance that returns the
			/// given <paramref name="value"/> for all <paramname ref="names"/>.
			/// </summary>
			/// <param name="value">The constant value for all fields</param>
			/// <param name="names">The field names</param>
			public ConstantValueTestRow(object value, params string[] names)
			{
				_value = value;
				_names = names.ToArray();
			}

			public bool Exists(string name)
			{
				return _names.Contains(name, StringComparer.OrdinalIgnoreCase);
			}

			public object GetValue(string name)
			{
				return Exists(name) ? _value : null;
			}
		}

		#region Test the standard functions

		[Test]
		public void CanAbsFunction()
		{
			var env = new StandardEnvironment();

			var fabs = GetFunction(env, "ABS");

			Assert.IsNull(env.Invoke(fabs, new object[] {null}));
			Assert.AreEqual(13.5, env.Invoke(fabs, Args(13.5)));
			Assert.AreEqual(7.0, env.Invoke(fabs, Args(-7.0)));
		}

		[Test]
		public void CanCeilFunction()
		{
			var env = new StandardEnvironment();

			var fceil = GetFunction(env, "CEIL");

			Assert.IsNull(env.Invoke(fceil, new object[] { null }));
			Assert.AreEqual(14.0, env.Invoke(fceil, Args(13.5)));
			Assert.AreEqual(-7.0, env.Invoke(fceil, Args(-7.1)));
			Assert.AreEqual(0, env.Invoke(fceil, Args(-0.99)));
		}

		[Test]
		public void CanFloorFunction()
		{
			var env = new StandardEnvironment();

			var ffloor = GetFunction(env,"FLOOR");

			Assert.IsNull(env.Invoke(ffloor, new object[] { null }));
			Assert.AreEqual(13.0, env.Invoke(ffloor, Args(13.5)));
			Assert.AreEqual(-8.0, env.Invoke(ffloor, Args(-7.1)));
			Assert.AreEqual(0, env.Invoke(ffloor, Args(0.99)));
		}

		[Test]
		public void CanRoundFunction()
		{
			var env = new StandardEnvironment();

			var fround = GetFunction(env, "ROUND");

			Assert.IsNull(env.Invoke(fround, Args(null)));
			Assert.AreEqual(13.0, env.Invoke(fround, Args(13.4)));
			Assert.AreEqual(14.0, env.Invoke(fround, Args(13.5)));
			Assert.AreEqual(14.0, env.Invoke(fround, Args(13.6)));
			Assert.AreEqual(-7.0, env.Invoke(fround, Args(-7.1)));
			Assert.AreEqual(1.0, env.Invoke(fround, Args(0.99)));

			Assert.IsNull(env.Invoke(fround, Args(null, null)));
			Assert.AreEqual(12.345, env.Invoke(fround, Args(12.345, 3)));
			Assert.AreEqual(12.34, env.Invoke(fround, Args(12.345, 2)));
			Assert.AreEqual(12.3, env.Invoke(fround, Args(12.345, 1)));
			Assert.AreEqual(12.0, env.Invoke(fround, Args(12.345, 0)));
			Assert.AreEqual(12.0, env.Invoke(fround, Args(12.345, null)));
		}

		[Test]
		public void CanTruncFunction()
		{
			var env = new StandardEnvironment();

			var ftrunc = GetFunction(env, "TRUNC");

			Assert.IsNull(env.Invoke(ftrunc, new object[] { null }));
			Assert.AreEqual(13.0, env.Invoke(ftrunc, Args(13.5)));
			Assert.AreEqual(-7.0, env.Invoke(ftrunc, Args(-7.1)));
			Assert.AreEqual(0, env.Invoke(ftrunc, Args(0.99)));

			// The argument to TRUNC is required:
			var ex = Assert.Catch<EvaluationException>(() => env.Invoke(ftrunc));
			Console.WriteLine(@"Expected exception: {0}", ex.Message);
		}

		[Test]
		public void CanRandFunction()
		{
			var random = new Random(1234); // repeatable randomness
			var env = new StandardEnvironment().SetRandom(random);

			const double epsilon = 0.0000000001;

			var frand = GetFunction(env, "RAND");

			Assert.AreEqual(0.39908097935797693, (double)env.Invoke(frand, Args()), epsilon); // RAND/0
			Assert.AreEqual(8, env.Invoke(frand, Args(10)));
			Assert.AreEqual(7, env.Invoke(frand, Args(5, 12)));

			Assert.IsNull(env.Invoke(frand, Args(null)));
			Assert.IsNull(env.Invoke(frand, Args(null, 12)));
			Assert.IsNull(env.Invoke(frand, Args(7, null)));
			Assert.IsNull(env.Invoke(frand, Args(null, null)));

			var ex = Assert.Catch<EvaluationException>(() => env.Invoke(frand, Args(1, 2, 3)));
			Console.WriteLine(@"Expected exception: {0}", ex.Message);
		}

		[Test]
		public void CanRandPickFunction()
		{
			var random = new Random(1234); // repeatable randomness
			var env = new StandardEnvironment().SetRandom(random);

			var frandpick = GetFunction(env, "RANDPICK");

			Assert.IsNull(env.Invoke(frandpick, Args()));
			Assert.AreEqual("one", env.Invoke(frandpick, Args("one")));
			Assert.AreEqual("foo", env.Invoke(frandpick, Args("foo", "bar")));
			Assert.AreEqual(4, env.Invoke(frandpick, Args(1, 2, 3, 3, 3, 4)));
		}

		[Test]
		public void CanMinFunction()
		{
			var env = new StandardEnvironment();

			var fmin = GetFunction(env, "MIN");

			Assert.IsNull(env.Invoke(fmin, new object[] { null }));
			Assert.AreEqual("one", env.Invoke(fmin, Args("one")));
			Assert.AreEqual("one", env.Invoke(fmin, Args("one", "two", "three")));

			// The StandardEnvironment's MIN function defines
			// false < true < numbers < strings and treats null
			// as uncomparable (in which case it returns null):

			Assert.AreEqual(false, env.Invoke(fmin, Args("foo", 123.45, true, "bar", -7, false)));
			Assert.AreEqual(null, env.Invoke(fmin, Args("foo", 123.45, true, "bar", -7, false, null))); // false
		}

		[Test]
		public void CanMaxFunction()
		{
			var env = new StandardEnvironment();

			var fmax = GetFunction(env, "MAX");

			Assert.IsNull(env.Invoke(fmax, new object[] { null }));
			Assert.AreEqual("one", env.Invoke(fmax, Args("one")));
			Assert.AreEqual("two", env.Invoke(fmax, Args("one", "two", "three")));

			// The StandardEnvironment's MAX function defines
			// false < true < numbers < strings and treats null
			// as uncomparable (in which case it returns null):

			Assert.AreEqual("foo", env.Invoke(fmax, Args("foo", 123.45, true, "bar", -7, false)));
			Assert.AreEqual(null, env.Invoke(fmax, Args("foo", 123.45, true, "bar", -7, false, null))); // "foo"
		}

		[Test]
		public void CanTrimFunction()
		{
			var env = new StandardEnvironment();

			var ftrim = GetFunction(env, "TRIM");

			Assert.AreEqual(".abc-", env.Invoke(ftrim, Args(" .abc-\t  ")));
			Assert.AreEqual("abc", env.Invoke(ftrim, Args(" .abc-\t  ", " \t.-")));

			// TRIM: 1st arg is null-contagious, 2nd arg defaults to white space
			Assert.IsNull(env.Invoke(ftrim, Args(null)));
			Assert.AreEqual("x", env.Invoke(ftrim, Args(" x ", null)));
			Assert.IsNull(env.Invoke(ftrim, Args(null, null)));

			var ex1 = Assert.Catch<EvaluationException>(() => env.Invoke(ftrim, Args()));
			Console.WriteLine(@"Expected exception: {0}", ex1.Message);
			var ex2 = Assert.Catch<EvaluationException>(() => env.Invoke(ftrim, "foo", "bar", "bang"));
			Console.WriteLine(@"Expected exception: {0}", ex2.Message);
		}

		[Test]
		public void CanUcaseFunction()
		{
			var env = new StandardEnvironment();

			var fucase = GetFunction(env, "UCASE");

			Assert.AreEqual("HELLO WORLD", env.Invoke(fucase, Args("Hello World")));
			Assert.IsNull(env.Invoke(fucase, Args(null)));

			Assert.Catch<EvaluationException>(() => env.Invoke(fucase, Args()));
			Assert.Catch<EvaluationException>(() => env.Invoke(fucase, Args("one", "two")));
			var ex = Assert.Catch(() => env.Invoke(fucase, Args(123)));
			Console.WriteLine(@"Expected exception: {0}", ex.Message);
		}

		[Test]
		public void CanLcaseFunction()
		{
			var env = new StandardEnvironment();

			var flcase = GetFunction(env, "LCASE");

			Assert.AreEqual("hello world", env.Invoke(flcase, Args("Hello World")));
			Assert.IsNull(env.Invoke(flcase, Args(null)));

			Assert.Catch<EvaluationException>(() => env.Invoke(flcase, Args()));
			Assert.Catch<EvaluationException>(() => env.Invoke(flcase, Args("one", "two")));
			var ex = Assert.Catch(() => env.Invoke(flcase, Args(123)));
			Console.WriteLine(@"Expected exception: {0}", ex.Message);
		}

		[Test]
		public void CanLPadFunction()
		{
			var env = new StandardEnvironment();

			var flpad = GetFunction(env, "LPAD");

			Assert.AreEqual("   abc", env.Invoke(flpad, Args("abc", 6)));
			Assert.AreEqual("***123", env.Invoke(flpad, Args(123, 6, "*")));

			// 1st arg is null-contagious, 2nd & 3rd args have defaults
			Assert.IsNull(env.Invoke(flpad, Args(null, null)));
			Assert.IsNull(env.Invoke(flpad, Args(null, 5)));
			Assert.AreEqual("x", env.Invoke(flpad, Args("x", null)));
		}

		[Test]
		public void CanRPadFunction()
		{
			var env = new StandardEnvironment();

			var frpad = GetFunction(env, "RPAD");

			Assert.AreEqual("abc   ", env.Invoke(frpad, Args("abc", 6)));
			Assert.AreEqual("123***", env.Invoke(frpad, Args(123, 6, "*")));

			// 1st arg is null-contagious, 2nd & 3rd args have defaults
			Assert.IsNull(env.Invoke(frpad, Args(null, null)));
			Assert.IsNull(env.Invoke(frpad, Args(null, 5)));
			Assert.AreEqual("x", env.Invoke(frpad, Args("x", null)));
		}

		[Test]
		public void CanSubstrFunction()
		{
			var env = new StandardEnvironment();

			var fsubstr = GetFunction(env, "SUBSTR");

			Assert.AreEqual("cde", env.Invoke(fsubstr, Args("abcdefg", 2, 3)));
			Assert.AreEqual("cdefg", env.Invoke(fsubstr, Args("abcdefg", 2)));
			Assert.AreEqual("cdefg", env.Invoke(fsubstr, Args("abcdefg", 2, 99)));
			Assert.AreEqual("", env.Invoke(fsubstr, Args("abcdefg", -7, 3)));
			Assert.AreEqual("a", env.Invoke(fsubstr, Args("abcdefg", -7, 8)));
			Assert.AreEqual("abcdefgh", env.Invoke(fsubstr, Args("abcdefgh", -7)));
			Assert.AreEqual("", env.Invoke(fsubstr, Args("abcdefg", 9, 3)));

			// SUBSTR: the 1st and 2nd arg are null-contagious; the 3rd defaults
			Assert.IsNull(env.Invoke(fsubstr, Args(null, null, null)));
			Assert.IsNull(env.Invoke(fsubstr, Args(null, null)));
			Assert.IsNull(env.Invoke(fsubstr, Args(null, 2, 3)));
			Assert.IsNull(env.Invoke(fsubstr, Args(null, 2)));
			Assert.AreEqual("b", env.Invoke(fsubstr, Args("abc", 1, 1)));
			Assert.AreEqual("bc", env.Invoke(fsubstr, Args("abc", 1, null)));
			Assert.IsNull(env.Invoke(fsubstr, Args("abc", null, 1)));
			Assert.IsNull(env.Invoke(fsubstr, Args("abc", null, null)));
			Assert.IsNull(env.Invoke(fsubstr, Args("abc", null)));
		}

		[Test]
		public void CanConcatFunction()
		{
			var env = new StandardEnvironment();

			var fconcat = GetFunction(env, "CONCAT");

			Assert.AreEqual("x", env.Invoke(fconcat, Args("x")));
			Assert.AreEqual("xy", env.Invoke(fconcat, Args("x", "y")));
			Assert.AreEqual("xyz", env.Invoke(fconcat, Args("x", "y", "z")));

			Assert.AreEqual("", env.Invoke(fconcat, Args()));
			Assert.AreEqual("", env.Invoke(fconcat, Args(null)));

			Assert.AreEqual("123", env.Invoke(fconcat, Args(123)));
			Assert.AreEqual("x3", env.Invoke(fconcat, Args(null, "x", null, 3.0, null)));

			// CONCAT: null on input results in empty string on output!
			Assert.AreEqual("", env.Invoke(fconcat, Args()));
			Assert.AreEqual("", env.Invoke(fconcat, Args(null)));
			Assert.AreEqual("", env.Invoke(fconcat, Args(null, null)));
			Assert.AreEqual("", env.Invoke(fconcat, Args(null, null, null)));
		}

		[Test]
		public void CanLengthFunction()
		{
			var env = new StandardEnvironment();

			var flength = GetFunction(env, "LENGTH");

			Assert.AreEqual(3, env.Invoke(flength, Args("abc")));
			Assert.AreEqual(0, env.Invoke(flength, Args("")));
			Assert.IsNull(env.Invoke(flength, Args(null)));
		}

		[Test]
		public void CanDecodeFunction()
		{
			var env = new StandardEnvironment();

			var fdecode = GetFunction(env, "DECODE");

			Assert.AreEqual(12, env.Invoke(fdecode, Args(12)));
			Assert.AreEqual(13, env.Invoke(fdecode, Args(12, 13)));
			Assert.AreEqual(null, env.Invoke(fdecode, Args(12, 8, 16)));
			Assert.AreEqual(24, env.Invoke(fdecode, Args(12, 12, 24)));
			Assert.AreEqual(25, env.Invoke(fdecode, Args(12, 8, 16, 25)));

			// DECODE considers two nulls to be equal (emulate Oracle behaviour):
			Assert.IsNull(env.Invoke(fdecode, Args(null)));
			Assert.IsNull(env.Invoke(fdecode, Args(null, null)));
			Assert.AreEqual("deflt", env.Invoke(fdecode, Args(null, "deflt")));
			Assert.AreEqual("deflt", env.Invoke(fdecode, Args(null, "s1", "r1", "s2", "r2", "deflt")));
			Assert.AreEqual("r2", env.Invoke(fdecode, Args(null, "s1", "r1", null, "r2", "deflt")));
			Assert.IsNull(env.Invoke(fdecode, Args(null, "s1", "r1", "s2", "r2")));
			Assert.IsNull(env.Invoke(fdecode, Args(null, "s1", "r1", "s2", "r2", null)));
			Assert.IsNull(env.Invoke(fdecode, Args("s2", "s1", null, "s2", null, "deflt")));

			// DECODE: equate (int) 1 and (double/float) 1.0 etc.
			Assert.AreEqual("one", env.Invoke(fdecode, Args(1.0, 1, "one", "oops")));
		}

		[Test]
		public void CanWhenFunction()
		{
			var env = new StandardEnvironment();

			var fwhen = GetFunction(env, "WHEN");

			Assert.IsNull(env.Invoke(fwhen, Args()));
			Assert.AreEqual(123, env.Invoke(fwhen, Args(123)));
			Assert.AreEqual(null, env.Invoke(fwhen, Args(false, "foo")));
			Assert.AreEqual("foo", env.Invoke(fwhen, Args(true, "foo")));
			Assert.AreEqual("nix", env.Invoke(fwhen, Args(false, "foo", "nix")));
			Assert.AreEqual("foo", env.Invoke(fwhen, Args(true, "foo", "nix")));
			Assert.AreEqual("bar", env.Invoke(fwhen, Args(false, "foo", true, "bar")));
			Assert.AreEqual("foo", env.Invoke(fwhen, Args(true, "foo", true, "bar")));
			Assert.AreEqual(-1, env.Invoke(fwhen, Args(false, 1, false, 2, false, 3, -1)));
		}

		[Test]
		public void CanRegexMatchFunction()
		{
			var env = new StandardEnvironment();

			var fregex = GetFunction(env, "REGEX");

			Assert.IsNull(env.Invoke(fregex, Args(null, null)));
			Assert.IsNull(env.Invoke(fregex, Args("pattern", null)));
			Assert.IsNull(env.Invoke(fregex, Args(null, "text")));

			Assert.AreEqual(true, env.Invoke(fregex, Args("bazaa?r", "What a bazaar!")));
			Assert.AreEqual(true, env.Invoke(fregex, Args("bazaa?r", "What a bazar!")));
			Assert.AreEqual(false, env.Invoke(fregex, Args("bazaa?r", "What a baz!")));

			var ex = Assert.Catch<EvaluationException>(() => env.Invoke(fregex, Args("something missing")));
			Console.WriteLine(@"Expected exception: {0}", ex.Message);
		}

		[Test]
		public void CanRegexReplaceFunction()
		{
			var env = new StandardEnvironment();

			var fregex = GetFunction(env, "REGEX");

			Assert.IsNull(env.Invoke(fregex, Args("pattern", "text", null)));
			Assert.IsNull(env.Invoke(fregex, Args("pattern", null, "replacement")));
			Assert.IsNull(env.Invoke(fregex, Args(null, "text", "replacement")));

			Assert.AreEqual("a_b_c", env.Invoke(fregex, Args("(\\w)(?=.)", "abc", "$1_")));

			var ex = Assert.Catch<EvaluationException>(() => env.Invoke(fregex, Args("this", "is", "too", "much")));
			Console.WriteLine(@"Expected exception: {0}", ex.Message);
		}

		[Test]
		public void InvariantCultureTest()
		{
			// The StandardEnvironment must always use InvariantCulture
			// (de-DE uses "," as the decimal separator). Because CONCAT
			// converts its arguments to string, it's a good test case:

			var env = new StandardEnvironment();

			var fconcat = GetFunction(env, "CONCAT");

			var cc = Thread.CurrentThread.CurrentCulture;
			Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

			try
			{
				Assert.AreEqual("1,2", Convert.ToString(1.2, CultureInfo.CurrentCulture));

				Assert.AreEqual("1.2", env.Invoke(fconcat, Args(1.2)));
				Assert.AreEqual("1.23.4", env.Invoke(fconcat, Args(1.2, 3.4)));
				Assert.AreEqual("1.23.45.67.8", env.Invoke(fconcat, Args(1.2, 3.4, 5.6, 7.8)));
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = cc; // restore
			}
		}

		#endregion

		[Test]
		public void CanLogicalNot()
		{
			var env = new StandardEnvironment();

			Assert.AreEqual(null, env.Not(null));
			Assert.AreEqual(true, env.Not(false));
			Assert.AreEqual(false, env.Not(true));
		}

		[Test]
		public void CanLogicalAnd()
		{
			var env = new StandardEnvironment();

			Assert.AreEqual(null, env.And(null, null));
			Assert.AreEqual(false, env.And(null, false));
			Assert.AreEqual(null, env.And(null, true));
			Assert.AreEqual(false, env.And(false, null));
			Assert.AreEqual(false, env.And(false, false));
			Assert.AreEqual(false, env.And(false, true));
			Assert.AreEqual(null, env.And(true, null));
			Assert.AreEqual(false, env.And(true, false));
			Assert.AreEqual(true, env.And(true, true));
		}

		[Test]
		public void CanLogicalOr()
		{
			var env = new StandardEnvironment();

			Assert.AreEqual(null, env.Or(null, null));
			Assert.AreEqual(null, env.Or(null, false));
			Assert.AreEqual(true, env.Or(null, true));
			Assert.AreEqual(null, env.Or(false, null));
			Assert.AreEqual(false, env.Or(false, false));
			Assert.AreEqual(true, env.Or(false, true));
			Assert.AreEqual(true, env.Or(true, null));
			Assert.AreEqual(true, env.Or(true, false));
			Assert.AreEqual(true, env.Or(true, true));
		}

		[Test]
		public void CanIsFalse()
		{
			var env = new StandardEnvironment();

			// null and false are "falsy", all other values are "truthy"
			// Generally, IsFalse(x) != IsTrue(x) for all x except null.

			Assert.IsFalse(env.IsFalse(null));
			Assert.IsTrue(env.IsFalse(false));
			Assert.IsFalse(env.IsFalse(true));
			Assert.IsFalse(env.IsFalse(0.0));
			Assert.IsFalse(env.IsFalse(-2.5));
			Assert.IsFalse(env.IsFalse(string.Empty));
			Assert.IsFalse(env.IsFalse("foo"));
		}

		[Test]
		public void CanIsTrue()
		{
			var env = new StandardEnvironment();

			// null and false are "falsy", all other values are "truthy"
			// Generally, IsFalse(x) != IsTrue(x) for all x except null.

			Assert.IsFalse(env.IsTrue(null));
			Assert.IsFalse(env.IsTrue(false));
			Assert.IsTrue(env.IsTrue(true));
			Assert.IsTrue(env.IsTrue(0.0));
			Assert.IsTrue(env.IsTrue(-2.5));
			Assert.IsTrue(env.IsTrue(string.Empty));
			Assert.IsTrue(env.IsTrue("foo"));
		}

		[Test]
		public void CanIsType()
		{
			var env = new StandardEnvironment();

			Assert.IsTrue(env.IsType(null, "null"));
			Assert.IsTrue(env.IsType(true, "boolean"));
			Assert.IsTrue(env.IsType(1.25, "number"));
			Assert.IsTrue(env.IsType("foo", "string"));

			Assert.IsFalse(env.IsType(0, "null"));
			Assert.IsFalse(env.IsType(0, "boolean"));
			Assert.IsFalse(env.IsType("123", "number"));
			Assert.IsFalse(env.IsType(123, "string"));
			Assert.IsFalse(env.IsType(null, "boolean"));
			Assert.IsFalse(env.IsType(null, "number"));
			Assert.IsFalse(env.IsType(null, "string"));

			// Case in type name is ignored:

			Assert.IsTrue(env.IsType(null, "NULL"));
			Assert.IsTrue(env.IsType(false, "Boolean"));
			Assert.IsTrue(env.IsType(-500, "NuMbEr"));
			Assert.IsTrue(env.IsType("", "STRING"));

			// Unknown type name must throw an exception: 

			var ex = Assert.Catch<EvaluationException>(() => env.IsType("foo", "NoSuchType"));
			Console.WriteLine(@"Expected: {0}", ex.Message);
		}

		[Test]
		public void CannotDivideByZero()
		{
			// The StandardEnvironment throws on divide by zero
			// (rather than returning a double infinity or NaN or null)

			var evaluator = ExpressionEvaluator.Create("2.5/0.0");
			var env = new StandardEnvironment();

			var ex = Assert.Catch<EvaluationException>(() => evaluator.Evaluate(env));
			Console.WriteLine(@"Expected: {0}", ex.Message);
		}

		[Test]
		public void CanRegisterExtraFunctions()
		{
			var extra = new ExtraFunctionality(2.0);

			var env = new StandardEnvironment();

			// Register functions (static):
			env.Register("Pi", ExtraFunctionality.Pi);
			env.Register<double,double>("Square", ExtraFunctionality.Square);

			// Register methods (need object state):
			env.Register("Circumference", extra.Circumference, extra);
			env.Register<double,double>("Volume", extra.Volume, extra);
			env.Register<object[], double>("Volume", extra.Volume, extra); // variadic

			object r1 = env.Invoke(GetFunction(env, "Pi"));
			Assert.AreEqual(Math.PI, r1);

			object r2 = env.Invoke(GetFunction(env, "Square"), 2.0);
			Assert.AreEqual(4.0, r2);

			object r3 = env.Invoke(GetFunction(env, "Circumference"));
			Assert.AreEqual(extra.Radius * Math.PI * 2, r3);

			object r4 = env.Invoke(GetFunction(env, "Volume"), 3.0);
			Assert.AreEqual(extra.Radius * extra.Radius * Math.PI * 3.0, r4);

			object r5 = env.Invoke(GetFunction(env, "Volume"), 1.0, 2.0, 3.0); // variadic
			Assert.AreEqual(extra.Radius * extra.Radius * Math.PI * 6.0, r5);
		}

		#region Extra (non-env-subclass) functionality

		private class ExtraFunctionality
		{
			public double Radius { get; }

			public ExtraFunctionality(double radius)
			{
				Radius = radius;
			}

			// Functions (static)

			public static double Pi()
			{
				return Math.PI;
			}

			public static double Square(double x)
			{
				return x * x;
			}

			// Methods (using object state)

			public double Circumference()
			{
				return Radius * Pi() * 2;
			}

			public double Volume(double height)
			{
				return height * Square(Radius) * Pi();
			}

			public double Volume(object[] heights)
			{
				return heights.Cast<double>().Sum(h => Volume(h));
			}
		}

		#endregion

		#region Private methods

		private static object[] Args(params object[] args)
		{
			// The invocation Args(null) results in args being null,
			// not a one-element array containing null. This is
			// according to the C# spec: if there's a single arg,
			// and it can be implicitly converted to the parameter
			// array type, then this will be done (and null can be
			// implicitly converted to object[]).
			return args ?? new object[] {null};
		}

		private static Function GetFunction(IEvaluationEnvironment env, string name, string qualifier = null)
		{
			object value = env.Lookup(name, qualifier);

			if (value is Function function)
			{
				return function;
			}

			throw new AssertionException($"Value is not a function: {name}");
		}

		#endregion
	}
}
