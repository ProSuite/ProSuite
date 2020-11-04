using System;
using System.Globalization;

namespace ProSuite.Processing.Evaluation
{
	public abstract class EnvironmentBase : IEvaluationEnvironment
	{
		public abstract object Lookup(string name, string qualifier);

		public abstract object Invoke(Function target, params object[] args);

		#region Operational Semantics

		public virtual object Add(object x, object y)
		{
			if (x == null || y == null)
				return null; // propagate
			if (IsNumeric(x) && IsNumeric(y))
				return ToDouble(x) + ToDouble(y);
			if (x is string || y is string)
				return string.Concat(ToString(x), ToString(y));
			throw InvalidArgumentTypes("Add", x, y);
		}

		public virtual object Sub(object x, object y)
		{
			if (x == null || y == null)
				return null; // propagate
			if (IsNumeric(x) && IsNumeric(y))
				return ToDouble(x) - ToDouble(y);
			throw InvalidArgumentTypes("Sub", x, y);
		}

		public virtual object Mul(object x, object y)
		{
			if (x == null || y == null)
				return null; // propagate
			if (IsNumeric(x) && IsNumeric(y))
				return ToDouble(x) * ToDouble(y);
			throw InvalidArgumentTypes("Mul", x, y);
		}

		public virtual object Div(object x, object y)
		{
			// x/0 is an error, even if x is null!
			if (IsZero(y))
				throw DivideByZeroError();
			if (x == null || y == null)
				return null; // propagate
			if (IsNumeric(x) && IsNumeric(y))
				return ToDouble(x) / ToDouble(y);
			throw InvalidArgumentTypes("Div", x, y);
		}

		public virtual object Rem(object x, object y)
		{
			// x%0 is an error, even if x is null!
			if (IsZero(y))
				throw DivideByZeroError();
			if (x == null || y == null)
				return null; // propagate
			if (IsNumeric(x) && IsNumeric(y))
				return ToDouble(x) % ToDouble(y);
			throw InvalidArgumentTypes("Rem", x, y);
		}

		public virtual object Pos(object value)
		{
			if (value == null)
				return null;
			if (IsNumeric(value))
				return value;
			throw InvalidArgumentType("Pos", value);
		}

		public virtual object Neg(object value)
		{
			if (value == null)
				return null;
			if (IsNumeric(value))
				return -ToDouble(value);
			throw InvalidArgumentType("Neg", value);
		}

		public virtual object Not(object value)
		{
			if (value == null)
				return null; // propagate
			return IsFalse(value);
		}

		// Truth tables for 3-valued logic
		//
		//  A  B    A and B    A or B     not A
		// -------------------------------------
		//  N  N       N         N          N
		//  N  F       F         N
		//  N  T       N         T
		//  F  N       F         N          T
		//  T  N       N         T          F
		// ---------------------------
		//  F  F       F         F
		//  F  T       F         T
		//  T  F       F         T
		//  T  T       T         T

		public virtual object And(object x, object y)
		{
			if (x == null && y == null)
				return null; // null and null
			if (x == null && IsFalse(y))
				return false; // null and false
			if (y == null && IsFalse(x))
				return false; // false and null
			if (x == null || y == null)
				return null; // either is null, but not both

			return ! IsFalse(x) && ! IsFalse(y);
		}

		public virtual object Or(object x, object y)
		{
			if (x == null && y == null)
				return null; // null or null
			if (x == null && IsTrue(y))
				return true; // null or true
			if (y == null && IsTrue(x))
				return true; // true or null
			if (x == null || y == null)
				return null; // either is null, but not both

			return IsTrue(x) || IsTrue(y);
		}

		public virtual bool IsType(object value, string type)
		{
			if (type == null)
				return false;

			// Always ignore case. Override in subclass
			// if different behaviour is desired (usually
			// because more types should be recognized).
			type = type.ToLowerInvariant();

			switch (type)
			{
				case "null":
					return value == null;
				case "bool":
				case "boolean":
					return value is bool;
				case "string":
				case "text":
					return value is string;
				case "number":
				case "numeric":
					return IsNumeric(value);
			}

			throw new EvaluationException(
				string.Format(
					"Unknown type for 'is' operator: '{0}' (expect one of: " +
					"null, boolean, number, string)", type));
		}

		public virtual bool IsFalse(object value)
		{
			if (value == null)
				return false; // null is not false (nor true)
			if (value is bool)
				return ((bool) value) == false;
			return false; // all values are "truthy" (except null and false)
		}

		public virtual bool IsTrue(object value)
		{
			if (value == null)
				return false; // null is not true (nor false)
			if (value is bool)
				return (bool) value;
			return true; // all values are "truthy" (except null and false)
		}

		/// <summary>
		/// Establish an ordering of the values in our universe.
		/// The ordering is partial, because not all values can
		/// be compared with each other. This is different from
		/// common practice in languages like Erlang, where the
		/// language defines a total ordering among all values.
		/// <para/>
		/// This method returns <c>null</c> if <paramref name="x"/>
		/// and <paramref name="y"/> are not comparable; the
		/// evaluator is designed to cope with such a return value.
		/// <para/>
		/// This method may also throw an exception if it thinks
		/// that <paramref name="x"/> and <paramref name="y"/>
		/// shall not be compared in the first place; such an
		/// exception is not handled by the evaluator.
		/// </summary>
		/// <returns>
		/// A negative, zero, or positive integer if <paramref name="x"/>
		/// is less than, equal to, or greater than <paramref name="y"/>.
		/// Or it returns null, of the two values are not comparable.
		/// </returns>
		public virtual int? Compare(object x, object y)
		{
			// Null is not comparable to any other value:

			if (x == null || y == null)
			{
				return null;
			}

			// For all decent values, define an ordering like this:
			//   false < true < numbers < strings
			// For simplicity, all numeric types are first converted to double,
			// which may loose precision (but not magnitude, except for (u)long).
			// Comparing values not listed above is considered an error
			// and an exception will be thrown.

			int ar = GetTypeRank(x);
			int br = GetTypeRank(y);

			if (ar < br)
			{
				return -1;
			}

			if (ar > br)
			{
				return +1;
			}

			// Same type rank:

			switch (ar)
			{
				case 0: // null
					return 0;

				case 1: // boolean
					return ((bool) x).CompareTo((bool) y);

				case 2: // numeric
					double ad = ToDouble(x);
					double bd = ToDouble(y);
					return ad.CompareTo(bd);

				case 4: // string
					return string.CompareOrdinal((string) x, (string) y);

				default:
					throw new ArgumentException(string.Format(
						                            "Cannot compare {0} and {1}",
						                            GetTypeName(x), GetTypeName(y)));
			}
		}

		#endregion

		#region Non-public methods

		protected static bool IsNumeric(object value)
		{
			TypeCode code = Convert.GetTypeCode(value);

			switch (code)
			{
				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
					return true;
			}

			return false;
		}

		protected static double ToDouble(object value)
		{
			return Convert.ToDouble(value, CultureInfo.InvariantCulture);
		}

		protected static bool IsZero(object value)
		{
			// Use a value larger than double.Epsilon? Configurable?
			return IsNumeric(value) && Math.Abs(ToDouble(value)) < double.Epsilon;
		}

		protected static string ToString(object value)
		{
			return Convert.ToString(value, CultureInfo.InvariantCulture);
		}

		protected static int ToInt32(object value)
		{
			return Convert.ToInt32(value, CultureInfo.InvariantCulture);
		}

		protected static EvaluationException InvalidArgumentType(string method, object value)
		{
			return new EvaluationException(
				string.Format("{0}: invalid argument type: {1}",
				              method, GetTypeName(value)));
		}

		protected static EvaluationException InvalidArgumentTypes(string method, object x, object y)
		{
			return new EvaluationException(
				string.Format("{0}: invalid argument types: {1} and {2}",
				              method, GetTypeName(x), GetTypeName(y)));
		}

		protected static EvaluationException DivideByZeroError()
		{
			return new EvaluationException("Division by zero");
		}

		private static string GetTypeName(object value)
		{
			return value == null ? "null" : value.GetType().Name;
		}

		private static int GetTypeRank(object value)
		{
			TypeCode code = Convert.GetTypeCode(value);

			switch (code)
			{
				case TypeCode.Empty:
				case TypeCode.DBNull:
					return 0;

				case TypeCode.Boolean:
					return 1;

				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
					return 2; // numeric

				case TypeCode.String:
					return 4;

				default:
					return 99; // value's type is not supported (rank it higher than anything else)
			}
		}

		#endregion
	}
}
