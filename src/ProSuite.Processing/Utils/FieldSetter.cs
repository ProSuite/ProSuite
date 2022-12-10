using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Processing.Evaluation;

namespace ProSuite.Processing.Utils
{
	/// <summary>
	/// Assign values to one or more fields in a table row or feature.
	/// The assignments are specified as a text string of the form
	/// <code>Field = Expr; Field = Expr; etc.</code>
	/// where Field is the name of a field in the row or feature, and
	/// Expr is an expression understood by <see cref="ExpressionEvaluator"/>.
	/// </summary>
	/// <remarks>
	/// This class is NOT thread-safe because it keeps mutable state
	/// at the instance level.
	/// </remarks>
	public class FieldSetter
	{
		private readonly Assignment[] _assignments;
		private readonly object[] _values;
		private string _text;

		public string Text => _text ?? (_text = Format());
		public string Name { get; private set; }

		/// <param name="text">Field assignments; syntax: name = expr { ; name = expr }</param>
		/// <param name="name">Optional name for this field setter</param>
		public FieldSetter(string text, string name = null)
		{
			var assignments = Parse(text ?? string.Empty);

			Name = name;

			_assignments = assignments.ToArray();
			_values = new object[_assignments.Length];
			_text = null;
		}

		/// <summary>
		/// Create a <see cref="FieldSetter"/> instance.
		/// </summary>
		/// <param name="text">The field assignments; syntax: name = expr { ; name = expr }</param>
		/// <param name="name">Optional name for created field setter</param>
		/// <returns>A <see cref="FieldSetter"/> instance</returns>
		public static FieldSetter Create([CanBeNull] string text, string name = null)
		{
			return new FieldSetter(text, name);
		}

		/// <summary>
		/// Attach a name to this field setter (may be useful
		/// for creating meaningful error messages).
		/// </summary>
		public FieldSetter SetName(string name)
		{
			Name = name;
			return this;
		}

		/// <summary>
		/// Throw an exception with a descriptive message if any
		/// of the target fields in the FieldSetter's assignments
		/// is not within the given <paramref name="fields"/>.
		/// </summary>
		public FieldSetter ValidateTargetFields([NotNull] IEnumerable<string> fields)
		{
			Assert.ArgumentNotNull(fields, nameof(fields));

			var lookup = fields.ToLookup(f => f, StringComparer.OrdinalIgnoreCase);
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

			return this;
		}

		/// <summary>
		/// Execute this field setter on the given <paramref name="row"/>,
		/// that is, perform the field value assignments. It is the caller's
		/// duty to store these changes.
		/// </summary>
		/// <param name="row">The row on which to operate</param>
		/// <param name="env">The environment to use</param>
		/// <param name="stack">An optional evaluation stack to (re)use</param>
		public void Execute(IRowValues row, IEvaluationEnvironment env, Stack<object> stack = null)
		{
			Assert.ArgumentNotNull(row, nameof(row));

			Array.Clear(_values, 0, _values.Length);

			int count = _assignments.Length;

			for (int i = 0; i < count; i++)
			{
				var evaluator = _assignments[i].Evaluator;

				if (stack == null) stack = new Stack<object>();

				_values[i] = evaluator.Evaluate(env, stack);
			}

			for (int i = 0; i < count; i++)
			{
				string fieldName = _assignments[i].FieldName;
				int fieldIndex = GetFieldIndex(row, fieldName);

				object value = _values[i];

				row[fieldIndex] = value ?? DBNull.Value;
			}
		}

		private static int GetFieldIndex(IRowValues row, string fieldName)
		{
			// Here we *may* want to cache field index (measure)
			return row.FindField(fieldName);
		}

		public IEnumerable<KeyValuePair<string, ExpressionEvaluator>> GetAssignments()
		{
			return _assignments.Select(
				a => new KeyValuePair<string, ExpressionEvaluator>(a.FieldName, a.Evaluator));
		}

		private string Format()
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

			return sb.ToString();
		}

		public override string ToString()
		{
			return Text ?? string.Empty;
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

				var evaluator = ExpressionEvaluator.Create(text, index, out int length);
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
			if (index >= text.Length || (cc = text[index]) != '_' && ! char.IsLetter(cc))
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
