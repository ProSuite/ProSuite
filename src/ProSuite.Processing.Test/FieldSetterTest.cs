using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using ProSuite.Processing.Evaluation;
using ProSuite.Processing.Test.Mocks;
using ProSuite.Processing.Utils;

namespace ProSuite.Processing.Test
{
	[TestFixture]
	public class FieldSetterTest
	{
		[Test]
		public void CanEmptyAndNull()
		{
			// An empty (or null) assignments string must not choke the parser
			// and it must not change any fields on Execute. This is important
			// because null/empty is probably the default value for the field
			// assignments parameter in all Carto Processes that use it.

			var row = new RowValuesMock("foo", "bar", "baz", "quux");
			var fieldNames = row.FieldNames;

			var fsEmpty = FieldSetter.Create(string.Empty);
			Assert.NotNull(fsEmpty);
			row.SetValues(1.2, "2.3", DBNull.Value, null);
			fsEmpty.Execute(row);
			row.AssertValues(1.2, "2.3", DBNull.Value, null);

			var fsNull = FieldSetter.Create(null);
			Assert.NotNull(fsNull);
			row.SetValues(1.2, "2.3", DBNull.Value, null);
			fsEmpty.Execute(row);
			row.AssertValues(1.2, "2.3", DBNull.Value, null);

			// Target field validation must also succeed:
			fsEmpty.ValidateTargetFields(fieldNames);
			fsNull.ValidateTargetFields(fieldNames);
		}

		[Test]
		public void CanParseFieldNames()
		{
			// Field names: 1 Letter or Underscore followed by 0..N Letters, Digits, Underscores

			FieldSetter.Create("foo = 'bar'");
			FieldSetter.Create("RuleID_1 = 5");
			FieldSetter.Create("_a = 6");
			FieldSetter.Create("_7_ = 7");

			Exception ex1 = Assert.Catch<FormatException>(() => FieldSetter.Create("123 = 'oops'"));
			Console.WriteLine(@"Expected error: {0}", ex1.Message);
			Exception ex2 = Assert.Catch<FormatException>(() => FieldSetter.Create("7X = 77"));
			Console.WriteLine(@"Expected error: {0}", ex2.Message);
			Exception ex3 = Assert.Catch<FormatException>(() => FieldSetter.Create("foo bar"));
			Console.WriteLine(@"Expected error: {0}", ex3.Message);
		}

		[Test]
		public void CanParseAssignments()
		{
			// Make sure the empty string works!
			var fs0 = FieldSetter.Create(string.Empty);
			Assert.AreEqual(string.Empty, ToString(fs0.GetAssignments()));

			// Assignment op is either "=" or ":=" and trailing ";" is optional:
			var fs1 = FieldSetter.Create("a=a+a; b:=a*a;");
			Assert.AreEqual("a=a+a;b=a*a", ToString(fs1.GetAssignments()));

			// White space between tokens shall be ignored:
			var fs2 = FieldSetter.Create("  a  =  'b'  ;  c  =  'd'  ;  ");
			Assert.AreEqual("a='b';c='d'", ToString(fs2.GetAssignments()));

			var fs3 = FieldSetter.Create(" \t size=RAND(5,9);angle=RAND()*360.0   ;   foo = marker.size/2  ");
			Assert.AreEqual("size=RAND(5,9);angle=RAND()*360.0;foo=marker.size/2", ToString(fs3.GetAssignments()));
		}

		[Test]
		public void CanValidateTargetFields()
		{
			var fields = new RowValuesMock("a", "b", "c").FieldNames;

			// All existing target fields:
			var good = FieldSetter.Create("a=3;b=5;c=7");
			good.ValidateTargetFields(fields);

			// Invalid target field:
			var bad = FieldSetter.Create("a=3;b=5;c=7;d=a+b+c");
			var ex1 = Assert.Catch<Exception>(() => bad.ValidateTargetFields(fields));
			Console.WriteLine(@"Expected exception: {0}", ex1.Message);

			// Multiple invalid target fields:
			var oops = FieldSetter.Create("a=3;b=5;c=6;foo=7;bar=8");
			var ex2 = Assert.Catch<Exception>(() => oops.ValidateTargetFields(fields));
			Console.WriteLine(@"Expected exception: {0}", ex2.Message);
		}

		[Test]
		public void CanAssignFields()
		{
			var row = new RowValuesMock("a", "b", "c", "d");

			// The empty assignment must not change any values:
			row.SetValues("foo", 10, 2.4, DBNull.Value);
			FieldSetter.Create(string.Empty).Execute(row);
			row.AssertValues("foo", 10, 2.4, DBNull.Value);

			// This assignment must change all values:
			FieldSetter.Create("a = 'bar'; b = 11; c = 3.5; d = d ?? 'def'")
			           .DefineFields(row)
			           .Execute(row);
			row.AssertValues("bar", 11, 3.5, "def");

			// A more realistic example:
			row.SetValues(DBNull.Value, 11, 3.5, "hi");
			FieldSetter.Create("a=CONCAT(b+marker.c); b=10+10*RAND(4); c=TRUNC(1.5+b/2); d=null")
			           .DefineFields(row, "marker")
			           .SetRandomSeed(1234)
			           .Execute(row);
			row.AssertValues("14.5", 20, 7, DBNull.Value);

			// On reading, DBNull shall be mapped to null,
			// on writing, null shall be mapped to DBNull:
			row.SetValues(DBNull.Value, null, "hi", "there");
			FieldSetter.Create("a=a??'null'; b=b??'null'; c=null; d=NULL")
			           .DefineFields(row)
			           .Execute(row);
			row.AssertValues("null", "null", DBNull.Value, DBNull.Value);
		}

		[Test]
		public void CanReferenceManualBindings()
		{
			var row = new RowValuesMock("a", "b", "c", "d");

			var fs1 = FieldSetter.Create("a=foo; b=bar; c=pi; d=CONCAT(pi)")
			                     .DefineValue("foo", "Foo")
			                     .DefineValue("bar", "Bar")
			                     .DefineValue("pi", 3.14159);
			fs1.Execute(row);
			row.AssertValues("Foo", "Bar", 3.14159, "3.14159");

			// Manual bindings override standard functions:
			fs1.DefineValue("CONCAT", 123);
			var ex1 = Assert.Catch<EvaluationException>(() => fs1.Execute(row)); // not a function
			Console.WriteLine(@"Expected exception: {0}", ex1.Message);
		}

		[Test]
		public void CanReferenceMultipleRows()
		{
			var one = new RowValuesMock("a", "b", "c");
			var two = new RowValuesMock("c", "d");

			one.SetValues("1", "2", "3");
			two.SetValues("III", "IV");

			var row = new RowValuesMock("a", "b", "x");
			row.SetValues(DBNull.Value, DBNull.Value, DBNull.Value);

			var fs1 = FieldSetter.Create("a=one.A; b=one.B; x=x??CONCAT(one.C, two.C, D)")
			                     .DefineFields(one, "one")
			                     .DefineFields(two, "two")
			                     .DefineFields(row);
			fs1.Execute(row);
			row.AssertValues("1", "2", "3IIIIV");

			fs1.ForgetAll(); // forget all previous definitions
			var ex1 = Assert.Catch<EvaluationException>(() => fs1.Execute(row));
			Console.WriteLine(@"Expected exception: {0}", ex1.Message);

			var fs2 = FieldSetter.Create("a=one.D").DefineFields(one, "one");
			var ex2 = Assert.Catch<EvaluationException>(() => fs2.Execute(row)); // No such field: one.D
			Console.WriteLine(@"Expected exception: {0}", ex2.Message);

			var fs3 = FieldSetter.Create("a=A").DefineFields(one, "one")
			                     .DefineFields(two, "two").DefineFields(row);
			var ex3 = Assert.Catch<EvaluationException>(() => fs3.Execute(row)); // Field name not unique
			Console.WriteLine(@"Expected exception: {0}", ex3.Message);
		}

		[Test]
		public void CanLookupPrecedence()
		{
			// Precedence: defined value < standard function < field value
			// CONCAT is a standard function.

			var row = new RowValuesMock("a", "b");

			var other = new RowValuesMock("CONCAT");
			other.SetValues("Field value");

			var fs = FieldSetter.Create("a=CONCAT; b = other.CONCAT");

			fs.DefineValue("CONCAT", "Defined value");
			fs.DefineFields(other, "other");

			fs.Execute(row);
			// Defined value takes precedence, but qualified always refers to field:
			row.AssertValues("Defined value", "Field value");

			fs.DefineValue("CONCAT", null);
			fs.Execute(row);
			// Defined value takes precedence, even if it is null:
			row.AssertValues(DBNull.Value, "Field value");

			fs.ForgetAll();
			fs.DefineFields(other, "other");
			fs.Execute(row);
			row.AssertValues(new Function("CONCAT"), "Field value");
		}

		[Test]
		public void CanReset()
		{
			var row = new RowValuesMock("a", "b", "c", "d");

			var another = new RowValuesMock("foo", "bar", "nix");
			another.SetValues("One", "Two", "Three");

			// Explicitly defined values override field values:

			var fs1 = FieldSetter.Create("a=foo; b=bar; c=nix")
								 .DefineValue("foo", "Foo")
								 .DefineValue("bar", "Bar")
								 .DefineValue("nix", null)
								 .DefineFields(another);
			fs1.Execute(row);
			row.AssertValues("Foo", "Bar", DBNull.Value, null);

			fs1.ForgetAll().DefineFields(another);
			fs1.Execute(row);
			row.AssertValues("One", "Two", "Three", null);

			fs1.ForgetAll();
			var ex1 = Assert.Catch<EvaluationException>(() => fs1.Execute(row)); // no such field
			Console.WriteLine(@"Expected exception: {0}", ex1.Message);
		}

		#region Test utilities

		private static string ToString(IEnumerable<KeyValuePair<string, ExpressionEvaluator>> assignments)
		{
			var sb = new StringBuilder();
			foreach (var pair in assignments)
			{
				if (sb.Length > 0)
					sb.Append(";");
				sb.Append(pair.Key);
				sb.Append("=");
				sb.Append(pair.Value.Clause);
			}
			return sb.ToString();
		}

		#endregion
	}
}
