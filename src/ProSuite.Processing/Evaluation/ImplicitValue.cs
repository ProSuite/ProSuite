using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.Processing.Utils;

namespace ProSuite.Processing.Evaluation
{
	/// <summary>
	/// A wrapper around <see cref="ExpressionEvaluator"/>.
	/// Useful to represent parameters that are specified
	/// as expressions (strings) instead of actual values.
	/// </summary>
	public class ImplicitValue
	{
		private readonly ExpressionEvaluator _evaluator;
		private readonly Stack<object> _stack;
		private StandardEnvironment _environment;

		private ImplicitValue(
			[NotNull] ExpressionEvaluator evaluator, bool isMissing, string name = null)
		{
			Name = name;
			IsMissing = isMissing;

			_evaluator = evaluator;
			_stack = new Stack<object>();
			_environment = new StandardEnvironment();
		}

		#region Factory

		/// <summary>
		/// An implicit value for the given <paramref name="expression"/>.
		/// If <paramref name="expression"/> is missing (<c>null</c> or empty),
		/// the implicit value will always evaluate to <c>null</c>.
		/// The <paramref name="name"/>, if present, serves as a prefix
		/// in exception messages.
		/// </summary>
		public static ImplicitValue Create(
			[CanBeNull] string expression, string name = null)
		{
			expression = StringUtils.Trim(expression);

			try
			{
				if (string.IsNullOrEmpty(expression))
				{
					var evaluator = ExpressionEvaluator.CreateConstant(null);
					return new ImplicitValue(evaluator, true, name);
				}
				else
				{
					var evaluator = ExpressionEvaluator.Create(expression);
					return new ImplicitValue(evaluator, false, name);
				}
			}
			catch (Exception ex)
			{
				throw Error($"Invalid expression: {ex.Message}", name);
			}
        }

		/// <summary>
		/// An implicit value that always evaluates to the given
		/// <paramref name="value"/>. The <paramref name="name"/>,
		/// if present, serves as a prefix in exception messages.
		/// </summary>
		/// <remarks>
		/// Use this when you have an actual value but need an
		/// <see cref="ImplicitValue"/> instance.
		/// </remarks>
		public static ImplicitValue CreateConstant(object value, string name = null)
		{
			var evaluator = ExpressionEvaluator.CreateConstant(value);
			return new ImplicitValue(evaluator, false, name);
		}

		/// <summary>
		/// An implicit value that always evaluates to <c>null</c>.
		/// </summary>
		public static ImplicitValue Null { get; } = CreateConstant(null, "null");

		#endregion

		/// <summary>
		/// The name of this implicit value.
		/// Useful for composing good error messages.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Returns true iff the expression was missing
		/// and therefore always evaluates to <c>null</c>.
		/// </summary>
		public bool IsMissing { get; }

		public StandardEnvironment Environment
		{
			[NotNull] get { return _environment; }
			set { _environment = value ?? new StandardEnvironment(); }
		}

		/// <summary>
		/// Evaluate the expression in the current environment.
		/// </summary>
		/// <returns>The actual value (can be null)</returns>
		public object Evaluate()
		{
			return _evaluator.Evaluate(_environment, _stack);
		}

		/// <summary>
		/// Evaluate the expression in the current environment.
		/// Cast the result to <typeparamref name="T"/> and return it.
		/// </summary>
		/// <typeparam name="T">Any value type (like int, double, bool)</typeparam>
		/// <param name="nullValue">The value to return if expression is null (optional)</param>
		/// <returns>The actual value (of type <typeparamref name="T"/>)</returns>
		public T Evaluate<T>(T? nullValue = null) where T : struct
		{
			object value = _evaluator.Evaluate(_environment, _stack);

			if (value == null)
			{
				return ConvertNull(nullValue);
			}

			try
			{
				return (T) value;
			}
			catch (InvalidCastException)
			{
				try
				{
					// Beware that Convert.ChangeType() converts quite liberally;
					// for example, it happily converts "123" to int

					object result = Convert.ChangeType(value, typeof(T), null);

					if (result == null)
					{
						return ConvertNull(nullValue);
					}

					return (T) result;
				}
				catch (Exception ex)
				{
					// Convert.ChangeType() may throw exceptions besides
					// InvalidCastException if it cannot change the type

					throw ConversionError(value, typeof(T), Name, ex);
				}
			}
		}

		private T ConvertNull<T>(T? nullValue) where T : struct
		{
			if (nullValue.HasValue)
			{
				return nullValue.Value;
			}

			throw ConversionError(null, typeof(T), Name);
		}

		private static EvaluationException ConversionError(object value, Type targetType, string name = null, Exception inner = null)
		{
			string message;

			if (value == null)
			{
				message = $"Cannot convert null to {targetType.Name}";
			}
			else
			{
				string actualType = value.GetType().Name;
				message = $"Cannot convert {value} (of type {actualType}) to {targetType.Name}";
			}

			return Error(message, name, inner);
		}

		private static EvaluationException Error(
			string message, string name = null, Exception inner = null)
		{
			if (! string.IsNullOrEmpty(name))
			{
				message = string.Concat(name, ": ", message);
			}

			if (inner != null)
			{
				message = string.Concat(message, ": ", inner.Message);
			}

			return new EvaluationException(message, inner);
		}

		[Obsolete]
		public ImplicitValue SetRandomSeed(int seed)
		{
			_environment.RandomSeed = seed;
			return this;
		}

		/// <summary>
		/// Define <paramref name="name"/> to be <paramref name="value"/>
		/// in the environment where the expression will be evaluated.
		/// Later definitions overwrite earlier definitions of the same name.
		/// </summary>
		/// <returns>This instance (for convenience).</returns>
		public ImplicitValue DefineValue(string name, [CanBeNull] object value)
		{
			_environment.DefineValue(name, value);
			return this;
		}

		public ImplicitValue ForgetValue(string name)
		{
			_environment.ForgetValue(name);
			return this;
		}

		/// <summary>
		/// Define all the values of the given <paramref name="row"/>
		/// using the given <paramref name="qualifier"/> in the environment
		/// where the expression will be evaluated.
		/// </summary>
		/// <returns>This instance (for convenience).</returns>
		public ImplicitValue DefineFields([NotNull] IRowValues row,
		                                  [CanBeNull] string qualifier = null)
		{
			var namedValues = (INamedValues) row;
			return DefineFields(namedValues, qualifier);
		}

		/// <summary>
		/// Define all the common values of the given <paramref name="rows"/>
		/// using the given <paramref name="qualifier"/> in the environment
		/// where the expression will be evaluated. If the value of a field
		/// is not constant among all the <paramref name="rows"/>, it will
		/// be undefined in the environment.
		/// </summary>
		/// <returns>This instance (for convenience).</returns>
		public ImplicitValue DefineFields([NotNull] IEnumerable<IRowValues> rows,
		                                     [CanBeNull] string qualifier = null)
		{
			var commonValues = new CommonValues(rows);
			return DefineFields(commonValues, qualifier);
		}

		public ImplicitValue DefineFields([NotNull] INamedValues values,
		                                  [CanBeNull] string qualifier = null)
		{
			_environment.DefineFields(values, qualifier);
			return this;
		}

		public ImplicitValue ForgetFields(string qualifier)
		{
			_environment.ForgetFields(qualifier);
			return this;
		}

		/// <summary>
		/// Forget (undefine) all definitions, that is, undo all
		/// calls to <see cref="DefineValue">DefineValue</see> and
		/// <see cref="DefineFields(IRowValues, string)">DefineFields</see>.
		/// </summary>
		/// <returns>This instance (for convenience).</returns>
		public ImplicitValue ForgetAll()
		{
			_environment.ForgetAll();
			return this;
		}

		public override string ToString()
		{
			string clause = _evaluator.Clause ?? "<missing>";
			return string.IsNullOrEmpty(Name) ? clause : $"{Name} = {clause}";
		}
	}
}
