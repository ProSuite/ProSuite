using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Text;
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

			var env = new StandardEnvironment();

			var row = new RowValuesMock("foo", "bar", "baz", "quux");

			var fsEmpty = new FieldSetter(string.Empty);
			Assert.NotNull(fsEmpty);
			row.SetValues(1.2, "2.3", DBNull.Value, null);
			fsEmpty.Execute(row, env);
			row.AssertValues(1.2, "2.3", DBNull.Value, null);

			var fsNull = new FieldSetter(null);
			Assert.NotNull(fsNull);
			row.SetValues(1.2, "2.3", DBNull.Value, null);
			fsEmpty.Execute(row, env);
			row.AssertValues(1.2, "2.3", DBNull.Value, null);

			// Target field validation must also succeed:
			fsEmpty.ValidateTargetFields(row.FieldNames);
			fsNull.ValidateTargetFields(row.FieldNames);
		}

		[Test]
		public void CanParseFieldNames()
		{
			// Field names: 1 Letter or Underscore followed by 0..N Letters, Digits, Underscores

			var fs1 = new FieldSetter("foo = 'bar'");
			Assert.AreEqual("foo", fs1.Assignments.Single().FieldName);
			var fs2 = new FieldSetter("RuleID_1 = 5");
			Assert.AreEqual("RuleID_1", fs2.Assignments.Single().FieldName);
			var fs3 = new FieldSetter("_a = 6");
			Assert.AreEqual("_a", fs3.Assignments.Single().FieldName);
			var fs4 = new FieldSetter("_7_ = 7");
			Assert.AreEqual("_7_", fs4.Assignments.Single().FieldName);

			Assert.Catch<FormatException>(() => _ = new FieldSetter("123 = 'oops'"));
			Assert.Catch<FormatException>(() => _ = new FieldSetter("7X = 77"));
			Assert.Catch<FormatException>(() => _ = new FieldSetter("foo bar"));
		}

		[Test]
		public void CanParseAssignments()
		{
			// Make sure the empty string works!
			var fs0 = new FieldSetter(string.Empty);
			Assert.AreEqual(string.Empty, Format(fs0.Assignments));

			// Assignment op is either "=" or ":=" and trailing ";" is optional:
			var fs1 = new FieldSetter("a=a+a; b:=a*a;");
			Assert.AreEqual("a=a+a;b=a*a", Format(fs1.Assignments));

			// White space between tokens shall be ignored:
			var fs2 = new FieldSetter("  a  =  'b'  ;  c  =  'd'  ;  ");
			Assert.AreEqual("a='b';c='d'", Format(fs2.Assignments));

			var fs3 = new FieldSetter(";; ;a='b' ; ; c = 'd' ;; ;");
			Assert.AreEqual("a='b';c='d'", Format(fs3.Assignments));

		var fs4 = new FieldSetter(" \t size=RAND(5,9);angle=RAND()*360.0   ;   foo = marker.size/2  ");
		Assert.AreEqual("size=RAND(5,9);angle=RAND()*360.0;foo=marker.size/2", Format(fs4.Assignments));
		}

		[Test]
		public void CanName()
		{
			// The Name is an optional property that may be useful for error messages:
			Assert.IsNull(new FieldSetter("foo = 1").Name);
			Assert.AreEqual("Hi", new FieldSetter("field = value; other=more", "Hi").Name);
		}

		[Test]
		public void CanText()
		{
			// The Text returns the parsed and reformatted assignments:
			Assert.IsEmpty(new FieldSetter(null).Text);
			Assert.IsEmpty(new FieldSetter(string.Empty).Text);
			Assert.AreEqual("foo = 'Bar'", new FieldSetter("foo = 'Bar'").Text);
		Assert.AreEqual("A = 1; B = 4/2; C = 'c'", new FieldSetter("A=1;;B=4/2 ; C=\t'c'").Text);
		}

		[Test]
		public void CanValidateTargetFields()
		{
			var fields = new RowValuesMock("a", "b", "c").FieldNames;

			// All existing target fields:
			var good = new FieldSetter("a=3;b=5;c=7");
			good.ValidateTargetFields(fields);

			// Invalid target field:
			var bad = new FieldSetter("a=3;b=5;c=7;d=a+b+c");
			Assert.Catch<Exception>(() => bad.ValidateTargetFields(fields));

			// Multiple invalid target fields:
			var oops = new FieldSetter("a=3;b=5;c=6;foo=7;bar=8");
			Assert.Catch<Exception>(() => oops.ValidateTargetFields(fields));
		}

		[Test]
		public void CanAssignFields()
		{
			var env = new StandardEnvironment();

			var row = new RowValuesMock("a", "b", "c", "d");

			// The empty assignment must not change any values:
			row.SetValues("foo", 10, 2.4, DBNull.Value);
			var fs1 = new FieldSetter(string.Empty);
			fs1.Execute(row, env);
			row.AssertValues("foo", 10, 2.4, DBNull.Value);

			// This assignment must change all values:
			var fs2 = new FieldSetter("a = 'bar'; b = 11; c = 3.5; d = d ?? 'def'");
			fs2.Execute(row, env.ForgetAll().DefineFields(row));
			row.AssertValues("bar", 11, 3.5, "def");

			// A more realistic example:
			row.SetValues(DBNull.Value, 11, 3.5, "hi");
			env.ForgetAll().DefineFields(row, "marker").SetRandom(new Random(1234));
		var fs3 = new FieldSetter("a=CONCAT(b+marker.c); b=10+10*RAND(4); c=TRUNC(1.5+b/2); d=null");
			fs3.Execute(row, env);
			row.AssertValues("14.5", 20, 7, DBNull.Value);

			// On reading, DBNull shall be mapped to null,
			// on writing, null shall be mapped to DBNull:
			row.SetValues(DBNull.Value, null, "hi", "there");
			env.ForgetAll().DefineFields(row);
			var fs4 = new FieldSetter("a=a??'null'; b=b??'null'; c=null; d=NULL");
			fs4.Execute(row, env);
			row.AssertValues("null", "null", DBNull.Value, DBNull.Value);
		}

		[Test]
	public void CanCatchNoSuchField()
	{
		var env = new StandardEnvironment();
		var row = new RowValuesMock("fieldA", "fieldB");

		var fs = new FieldSetter("fieldA = 1; fieldB = 2; oops = 42");
		Assert.Catch<EvaluationException>(() => fs.Execute(row, env));
	}

	[Test]
		public void CanReferenceManualBindings()
		{
			var env = new StandardEnvironment();

			var row = new RowValuesMock("a", "b", "c", "d");

			var fs1 = new FieldSetter("a=foo; b=bar; c=pi; d=CONCAT(pi)");
			env.ForgetAll()
			   .DefineValue("Foo", "foo")
			   .DefineValue("Bar", "bar")
			   .DefineValue(3.14159, "pi");
			fs1.Execute(row, env);
			row.AssertValues("Foo", "Bar", 3.14159, "3.14159");

			// Manual bindings override standard functions:
			env.DefineValue(123, "CONCAT");
			Assert.Catch<EvaluationException>(() => fs1.Execute(row, env)); // not a function
		}

		[Test]
		public void CanReferenceMultipleRows()
		{
			var env = new StandardEnvironment();

			var one = new RowValuesMock("a", "b", "c");
			var two = new RowValuesMock("c", "d");

			one.SetValues("1", "2", "3");
			two.SetValues("III", "IV");

			var row = new RowValuesMock("a", "b", "x");
			row.SetValues(DBNull.Value, DBNull.Value, DBNull.Value);

			var fs1 = new FieldSetter("a=one.A; b=one.B; x=x??CONCAT(one.C, two.C, D)");
			env.ForgetAll().DefineFields(one, "one").DefineFields(two, "two").DefineFields(row);
			fs1.Execute(row, env);
			row.AssertValues("1", "2", "3IIIIV");

			env.ForgetAll();
			Assert.Catch<EvaluationException>(() => fs1.Execute(row, env));

			var fs2 = new FieldSetter("a=one.D");
			env.ForgetAll().DefineFields(one, "one");
			Assert.Catch<EvaluationException>(() => fs2.Execute(row, env)); // No such field: one.D

			var fs3 = new FieldSetter("a=A");
			env.ForgetAll().DefineFields(one, "one").DefineFields(two, "two").DefineFields(row);
			Assert.Catch<EvaluationException>(() => fs3.Execute(row, env)); // Field name not unique
		}

		[Test]
		public void CanLookupPrecedence()
		{
			// Precedence: defined value < standard function < field value
			// CONCAT is a standard function.

			var env = new StandardEnvironment();

			var row = new RowValuesMock("a", "b");

			var other = new RowValuesMock("CONCAT");
			other.SetValues("Field value");

			var fs = new FieldSetter("a=CONCAT; b = other.CONCAT");
			env.ForgetAll().DefineValue("Defined value", "CONCAT").DefineFields(other, "other");
			fs.Execute(row, env);
			// Defined value takes precedence, but qualified always refers to field:
			row.AssertValues("Defined value", "Field value");

			env.DefineValue(null, "CONCAT");
			fs.Execute(row, env);
			// Defined value takes precedence, even if it is null:
			row.AssertValues(DBNull.Value, "Field value");

			env.ForgetAll().DefineFields(other, "other");
			fs.Execute(row, env);
			row.AssertValues(new Function("CONCAT"), "Field value");
		}

		[Test]
		public void CanReset()
		{
			var env = new StandardEnvironment();

			var row = new RowValuesMock("a", "b", "c", "d");

			var another = new RowValuesMock("foo", "bar", "nix");
			another.SetValues("One", "Two", "Three");

			// Explicitly defined values override field values:

			var fs1 = new FieldSetter("a=foo; b=bar; c=nix");
			env.ForgetAll().DefineValue("Foo", "foo").DefineValue("Bar", "bar")
			   .DefineValue(null, "nix").DefineFields(another);

			fs1.Execute(row, env);
			row.AssertValues("Foo", "Bar", DBNull.Value, null);

			env.ForgetAll().DefineFields(another);
			fs1.Execute(row, env);
			row.AssertValues("One", "Two", "Three", null);

			env.ForgetAll();
			Assert.Catch<EvaluationException>(() => fs1.Execute(row, env)); // no such field
		}

		[Test]
		public void CanGetFieldValue()
		{
			var env = new NullEnvironment();
			var fs = new FieldSetter("a=12; b=9+8-5;");

			var a = fs.GetFieldValue("a", env);
			Assert.AreEqual(12.0, a);

			var b = fs.GetFieldValue("b", env);
			Assert.AreEqual(12.0, b);

			var c = fs.GetFieldValue("unassigned", env);
			Assert.IsNull(c);
		}

		#region Test utilities

		private static string Format(IEnumerable<FieldSetter.Assignment> assignments)
		{
			return StringUtils.Join(";", assignments.Select(a => $"{a.FieldName}={a.Evaluator.Clause}"));
		}

		#endregion
	}
}
