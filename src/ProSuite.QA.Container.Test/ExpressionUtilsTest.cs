using System.Collections.Generic;
using NUnit.Framework;

namespace ProSuite.QA.Container.Test
{
	[TestFixture]
	public class ExpressionUtilsTest
	{
		[Test]
		public void CanReplaceTableNames()
		{
			const string exeSql = @"a_ds.f1 > b_ds.f1 and (a_ds.f2 * b_ds.f2 < 100)";
			const string mdlSql = @"a_mdl.f1 > b_mdl.f1 and (a_mdl.f2 * b_mdl.f2 < 100)";

			var replacements = new Dictionary<string, string>
			                   {
				                   { "a_mdl", "a_ds" },
				                   { "b_mdl", "b_ds" }
			                   };

			Assert.AreEqual(exeSql,
			                ExpressionUtils.ReplaceTableNames(mdlSql, replacements));
		}

		[Test]
		public void CanReplaceTableNames_NoModelNamesToReplace()
		{
			const string sql = @"a_ds.f1 > b_ds.f1 and (a_ds.f2 * b_ds.f2 < 100)";

			var replacements = new Dictionary<string, string>
			                   {
				                   { "a_mdl", "a_ds" },
				                   { "b_mdl", "b_ds" }
			                   };

			Assert.AreEqual(sql,
			                ExpressionUtils.ReplaceTableNames(sql, replacements));
		}

		[Test]
		public void CanReplaceWithQualifiedTableNames()
		{
			const string exeSql =
				@"xx.a_ds.f1 > yy.b_ds.f1 and (xx.a_ds.f2 * yy.b_ds.f2 < 100)";
			const string mdlSql = @"a_mdl.f1 > b_mdl.f1 and (a_mdl.f2 * b_mdl.f2 < 100)";

			var replacements = new Dictionary<string, string>
			                   {
				                   { "a_mdl", "xx.a_ds" },
				                   { "b_mdl", "yy.b_ds" }
			                   };

			Assert.AreEqual(exeSql,
			                ExpressionUtils.ReplaceTableNames(mdlSql, replacements));
		}

		[Test]
		public void CanReplaceWithQualifiedTableNames_AmbiguousNames()
		{
			const string exeSql =
				@"(a.a.a > a.b.b) and (a.a.a * a.b.b < 100) or (a.a.a * 10 < a.b.b)";
			const string mdlSql = @"(a.a > b.b) and (a.a * b.b < 100) or (a.a * 10 < b.b)";

			var replacements = new Dictionary<string, string>
			                   {
				                   { "a", "a.a" },
				                   { "b", "a.b" }
			                   };

			Assert.AreEqual(exeSql,
			                ExpressionUtils.ReplaceTableNames(mdlSql, replacements));
		}

		[Test]
		public void CanReplaceWithUnqualifiedTableNames()
		{
			const string exeSql = @"a_ds.f1 > b_ds.f1 and (a_ds.f2 * b_ds.f2 < 100)";
			const string modelSql =
				@"x.a_mdl.f1 > x.b_mdl.f1 and (x.a_mdl.f2 * x.b_mdl.f2 < 100)";
			var replacements = new Dictionary<string, string>
			                   {
				                   { "x.a_mdl", "a_ds" },
				                   { "x.b_mdl", "b_ds" }
			                   };

			Assert.AreEqual(exeSql,
			                ExpressionUtils.ReplaceTableNames(modelSql, replacements));
		}

		[Test]
		public void CanParseNoHint()
		{
			const string expression = "aa";

			bool? caseSensitive;
			Assert.AreEqual(expression,
			                ExpressionUtils.ParseCaseSensitivityHint(expression,
				                out caseSensitive));
			Assert.IsNull(caseSensitive);
		}

		[Test]
		public void CanParseEmptyString()
		{
			string expression = string.Empty;

			bool? caseSensitive;
			Assert.AreEqual(expression,
			                ExpressionUtils.ParseCaseSensitivityHint(expression,
				                out caseSensitive));
			Assert.IsNull(caseSensitive);
		}

		[Test]
		public void CanParseCaseSensitivityHint()
		{
			const string expression = "aa";

			bool? caseSensitive;
			Assert.AreEqual(expression,
			                ExpressionUtils.ParseCaseSensitivityHint(
				                expression + ExpressionUtils.CaseSensitivityHint,
				                out caseSensitive));
			Assert.True(caseSensitive != null && caseSensitive.Value);
		}

		[Test]
		public void CanParseIgnoreCaseHint()
		{
			const string expression = "aa";

			bool? caseSensitive;
			Assert.AreEqual(expression,
			                ExpressionUtils.ParseCaseSensitivityHint(
				                expression + ExpressionUtils.IgnoreCaseHint,
				                out caseSensitive));
			Assert.False(caseSensitive != null && caseSensitive.Value);
		}

		[Test]
		public void CanParseCaseSensitivityHintWithTrailingBlank()
		{
			const string expression = "aa";

			bool? caseSensitive;
			Assert.AreEqual(expression,
			                ExpressionUtils.ParseCaseSensitivityHint(
				                expression + ExpressionUtils.CaseSensitivityHint + " ",
				                out caseSensitive));
			Assert.True(caseSensitive != null && caseSensitive.Value);
		}

		[Test]
		public void CanParseIgnoreCaseHintWithTrailingBlank()
		{
			const string expression = "aa";

			bool? caseSensitive;
			Assert.AreEqual(expression,
			                ExpressionUtils.ParseCaseSensitivityHint(
				                expression + ExpressionUtils.IgnoreCaseHint + " ",
				                out caseSensitive));
			Assert.False(caseSensitive != null && caseSensitive.Value);
		}

		[Test]
		public void CanParseCaseSensitivityHintForEmptyExpression()
		{
			string expression = string.Empty;

			bool? caseSensitive;
			Assert.AreEqual(expression,
			                ExpressionUtils.ParseCaseSensitivityHint(
				                expression + ExpressionUtils.CaseSensitivityHint,
				                out caseSensitive));
			Assert.True(caseSensitive != null && caseSensitive.Value);
		}

		[Test]
		public void CanParseIgnoreCaseHintForEmptyExpression()
		{
			string expression = string.Empty;

			bool? caseSensitive;
			Assert.AreEqual(expression,
			                ExpressionUtils.ParseCaseSensitivityHint(
				                expression + ExpressionUtils.IgnoreCaseHint,
				                out caseSensitive));
			Assert.False(caseSensitive != null && caseSensitive.Value);
		}
	}
}
