using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ProSuite.Processing.Utils;

namespace ProSuite.Processing.Evaluation
{
	/// <summary>
	/// An expression that can be evaluated to yield
	/// a value of type <typeparamref name="T"/>.
	/// </summary>
	public class ImplicitValue<T> // TODO Consider rename Expression? Or too general term?
	{
		private readonly ExpressionEvaluator _evaluator;

		public string Expression { get; }
		public string Name { get; private set; }

		public ImplicitValue(string expression, string name = null)
		{
			Expression = expression.Canonical(); // may be null (i.e., missing)
			Name = name; // may be null

			// TODO consider: allow [ <Name> = ] <Expression> and if <Name>
			//      is given, use it for Name (argument name may override)
			// This way the expression can name itself! (Also update ToString() accordingly)

			_evaluator = Expression == null
				             ? null
				             : ExpressionEvaluator.Create(Expression);
		}

		public static ImplicitValue<double> Literal(double value)
		{
			var text = Convert.ToString(value, CultureInfo.InvariantCulture);
			return new ImplicitValue<double>(text);
		}

		public static ImplicitValue<bool> Literal(bool value)
		{
			var text = Convert.ToString(value, CultureInfo.InvariantCulture);
			return new ImplicitValue<bool>(text);
		}

		public static ImplicitValue<string> Literal(string value)
		{
			var sb = new StringBuilder();
			EvaluatorEngine.FormatLiteral(value, sb);
			return new ImplicitValue<string>(sb.ToString());
		}

		public static implicit operator ImplicitValue<T>(string expr)
		{
			// important: cast null to null, not some "null expr"
			return string.IsNullOrWhiteSpace(expr) ? null : new ImplicitValue<T>(expr);
		}

		public static implicit operator string(ImplicitValue<T> expr)
		{
			return expr?.Expression;
		}

		/// <summary>
		/// Attach a name to this expression (may be useful
		/// for creating meaningful error messages).
		/// </summary>
		public ImplicitValue<T> SetName(string name)
		{
			Name = name;
			return this;
		}

		/// <summary>
		/// Evaluate this expression in the given environment.
		/// The result value will be cast to <typeparamref name="T"/>.
		/// If the expression is empty/missing or evaluates to null,
		/// the given <paramref name="defaultValue"/> will be returned.
		/// </summary>
		public T Evaluate(IEvaluationEnvironment environment, T defaultValue = default, Stack<object> stack = null)
		{
			if (_evaluator == null)
			{
				return defaultValue;
			}

			object value = Evaluate(environment, stack);

			if (value is null)
			{
				return defaultValue;
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
						return defaultValue;
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

		private object Evaluate(IEvaluationEnvironment environment, Stack<object> stack = null)
		{
			try
			{
				return _evaluator.Evaluate(environment, stack ?? new Stack<object>());
			}
			catch (EvaluationException ex)
			{
				throw EvaluationError(_evaluator.Clause, Name, ex);
			}
		}

		public override string ToString()
		{
			return Expression ?? string.Empty;
		}

		private static EvaluationException EvaluationError(
			string expr, string name = null, Exception inner = null)
		{
			return Error($"Cannot evaluate {expr}", name, inner);
		}

		private static EvaluationException ConversionError(
			object value, Type targetType, string name = null, Exception inner = null)
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
	}
}
