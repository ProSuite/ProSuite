using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Processing.Utils;

namespace ProSuite.Processing.Evaluation
{
	/// <summary>
	/// Assign values to one or more fields in a <see cref="RowBuffer"/>
	/// (that is, a table row or feature). The assignments are specified
	/// as a text string of the form
	/// <code>Field = Expr; Field = Expr; etc.</code>
	/// where Field is the name of a field in the row or feature,
	/// and Expr is an expression over the fields in the row/feature
	/// and in a syntax understood by <see cref="ExpressionEvaluator"/>.
	/// </summary>
	/// <remarks>
	/// This class is NOT thread-safe because it keeps mutable state
	/// at the instance level.
	/// </remarks>
	public class FieldSetter
	{
		private readonly Assignment[] _assignments;
		private readonly FindFieldCache _findFieldCache;
		private readonly Stack<object> _stack;
		private readonly StandardEnvironment _environment;
		private readonly object[] _values;
		private string _text;

		private FieldSetter([NotNull] IEnumerable<Assignment> assignments,
		                    FindFieldCache findFieldCache = null)
		{
			_assignments = assignments.ToArray();
			_values = new object[_assignments.Length];
			_findFieldCache = findFieldCache ?? new FindFieldCache();
			_stack = new Stack<object>();
			_environment = new StandardEnvironment();
			_text = null;
		}

		/// <summary>
		/// Create a <see cref="FieldSetter"/> instance.
		/// </summary>
		/// <param name="text">The field assignments; syntax: name = expr { ; name = expr }</param>
		/// <param name="findFieldCache">A cache for field index lookups (optional)</param>
		/// <returns>A <see cref="FieldSetter"/> instance</returns>
		public static FieldSetter Create([CanBeNull] string text,
		                                 FindFieldCache findFieldCache = null)
		{
			return new FieldSetter(Parse(text ?? string.Empty), findFieldCache);
		}

		/// <summary>
		/// Throw an exception with a descriptive message if any
		/// of the target fields in the FieldSetter's assignments
		/// is not within the given <paramref name="fields"/>.
		/// </summary>
		public void ValidateTargetFields([NotNull] IReadOnlyList<Field> fields)
		{
			Assert.ArgumentNotNull(fields, nameof(fields));

			var lookup = fields.ToLookup(f => f.Name);
			var noSuchFields = _assignments.Where(a => ! lookup.Contains(a.FieldName))
			                               .Select(a => a.FieldName).ToArray();

			if (noSuchFields.Length > 1)
			{
				string missingFields = string.Join(", ", noSuchFields);
				throw new AssertionException($"No such target fields: {missingFields}");
			}

			if (noSuchFields.Length == 1)
			{
				string missingField = noSuchFields.Single();
				throw new AssertionException($"No such target field: {missingField}");
			}
		}

		/// <summary>
		/// Seed the random number generator.
		/// See <see cref="StandardEnvironment.RandomSeed"/> for details.
		/// </summary>
		/// <param name="seed">The seed value</param>
		/// <returns>This instance (for convenience).</returns>
		public FieldSetter SetRandomSeed(int seed)
		{
			_environment.RandomSeed = seed;
			return this;
		}

		/// <summary>
		/// Define <paramref name="name"/> to be <paramref name="value"/>
		/// in the environment where <see cref="Execute">Execute</see>
		/// performs the assignments. Later definitions overwrite earlier
		/// definitions of the same name.
		/// </summary>
		/// <returns>This instance (for convenience).</returns>
		public FieldSetter DefineValue(string name, [CanBeNull] object value)
		{
			_environment.DefineValue(name, value);
			return this;
		}

		public FieldSetter ForgetValue(string name)
		{
			_environment.ForgetValue(name);
			return this;
		}

		/// <summary>
		/// Define all the values of the given <paramref name="row"/>
		/// using the given <paramref name="qualifier"/> in the environment
		/// where <see cref="Execute">Execute</see> performs the assignments.
		/// </summary>
		/// <returns>This instance (for convenience).</returns>
		public FieldSetter DefineFields([NotNull] IRowValues row,
		                                [CanBeNull] string qualifier = null)
		{
			var namedValues = (INamedValues) row;
			return DefineFields(namedValues, qualifier);
		}

		/// <summary>
		/// Define all the values that are common across all
		/// given <paramref name="rows"/>. Values that differ
		/// between the given <paramref name="rows"/> will be
		/// set to <c>null</c>.
		/// </summary>
		/// <seealso cref="DefineFields(IRowValues, string)"/>
		public FieldSetter DefineFields([NotNull] IEnumerable<IRowValues> rows,
		                                [CanBeNull] string qualifier = null)
		{
			var commonValues = new CommonValues(rows);
			return DefineFields(commonValues, qualifier);
		}

		public FieldSetter DefineFields([NotNull] INamedValues values,
		                                [CanBeNull] string qualifier = null)
		{
			_environment.DefineFields(values, qualifier);
			return this;
		}

		public FieldSetter ForgetFields(string qualifier)
		{
			_environment.ForgetFields(qualifier);
			return this;
		}

		/// <summary>
		/// Forget all definitions that were done on this FieldSetter
		/// using one of the Define methods.
		/// </summary>
		/// <returns>This instance (for convenience).</returns>
		public FieldSetter ForgetAll()
		{
			_environment.ForgetAll();
			return this;
		}

		/// <summary>
		/// Execute this field setter on the given <paramref name="row"/>,
		/// that is, perform the field value assignments. It is the caller's
		/// duty to <see cref="Row.Store">Store</see> these changes.
		/// <para/>
		/// The values of the given <paramref name="row"/> are not available
		/// in the evaluation environment unless this row has been passed
		/// to <see cref="DefineFields(IRowValues, string)"/> before.
		/// </summary>
		/// <param name="row">The row on which to operate</param>
		/// <param name="env">The environment to use (optional, defaults
		///  to the FieldSetter's internal environment that you can
		///  manipulate with the various Define methods</param>
		public void Execute(IRowValues row, IEvaluationEnvironment env = null)
		{
			Assert.ArgumentNotNull(row, nameof(row));

			Array.Clear(_values, 0, _values.Length);

			int count = _assignments.Length;

			for (int i = 0; i < count; i++)
			{
				var evaluator = _assignments[i].Evaluator;

				_values[i] = evaluator.Evaluate(env ?? _environment, _stack);
			}

			for (int i = 0; i < count; i++)
			{
				string fieldName = _assignments[i].FieldName;
				int fieldIndex = _findFieldCache.GetFieldIndex(row, fieldName);

				object value = _values[i];

				row[fieldIndex] = value ?? DBNull.Value;
			}
		}

		public IEnumerable<KeyValuePair<string, ExpressionEvaluator>> GetAssignments()
		{
			return _assignments.Select(
				a => new KeyValuePair<string, ExpressionEvaluator>(a.FieldName, a.Evaluator));
		}

		public override string ToString()
		{
			if (_text == null)
			{
				var sb = new StringBuilder();
				foreach (var pair in GetAssignments())
				{
					if (sb.Length > 0)
					{
						sb.Append("; ");
					}

					sb.AppendFormat("{0} = {1}", pair.Key, pair.Value.Clause);
				}

				_text = sb.ToString();
			}

			return _text;
		}

		#region Assignments Parser

		private static IEnumerable<Assignment> Parse([NotNull] string text)
		{
			int index = 0;

			SkipWhite(text, ref index);

			if (index >= text.Length)
			{
				yield break;
			}

			while (index < text.Length)
			{
				string name = ScanName(text, ref index);
				if (string.IsNullOrEmpty(name))
				{
					throw SyntaxError("Expect field name (position {0})", index);
				}

				SkipWhite(text, ref index);

				if (! ScanAssignmentOp(text, ref index))
				{
					throw SyntaxError("Expect '=' operator (position {0})", index);
				}

				int length;
				var evaluator = ExpressionEvaluator.Create(text, index, out length);
				index += length;

				yield return new Assignment(name, evaluator);

				SkipWhite(text, ref index);

				if (IsChar(text, index, ';'))
				{
					index += 1;
				}

				SkipWhite(text, ref index);
			}
		}

		[StringFormatMethod("format")]
		private static FormatException SyntaxError(string format, params object[] args)
		{
			return new FormatException(string.Format(format, args));
		}

		private static string ScanName(string text, ref int index)
		{
			char cc;
			if (index >= text.Length || ((cc = text[index]) != '_' && ! char.IsLetter(cc)))
			{
				return null; // not a name at text[index...]
			}

			int anchor = index;
			while (index < text.Length && ((cc = text[index]) == '_' || char.IsLetterOrDigit(cc)))
			{
				index += 1;
			}

			return text.Substring(anchor, index - anchor);
		}

		private static bool ScanAssignmentOp(string text, ref int index)
		{
			// "=" or ":=" followed by anything BUT another "=" sign

			if (IsChar(text, index, ':'))
			{
				index += 1;
			}

			if (IsChar(text, index, '=') && ! IsChar(text, index + 1, '='))
			{
				index += 1;
				return true;
			}

			return false;
		}

		private static bool IsChar(string text, int index, char expected)
		{
			return index < text.Length && text[index] == expected;
		}

		private static void SkipWhite(string text, ref int index)
		{
			while (index < text.Length && char.IsWhiteSpace(text, index))
			{
				index += 1;
			}
		}

		#endregion

		#region Nested type: Assignment

		private readonly struct Assignment
		{
			public readonly string FieldName;
			public readonly ExpressionEvaluator Evaluator;

			public Assignment(string name, ExpressionEvaluator evaluator)
			{
				FieldName = name;
				Evaluator = evaluator;
			}

			public override string ToString()
			{
				return $"{FieldName} = {Evaluator.Clause}";
			}
		}

		#endregion
	}
}
