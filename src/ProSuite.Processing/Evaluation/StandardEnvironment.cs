using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Processing.Evaluation
{
	/// <summary>
	/// A simple environment for expression evaluation.
	/// You can define named values and register functions.
	/// A set of commonly useful "standard functions" is
	/// already registered.
	/// </summary>
	public class StandardEnvironment : EnvironmentBase
	{
		private readonly IDictionary<string, object> _values;
		private readonly IDictionary<string, INamedValues> _rows;
		private readonly IDictionary<FunKey, Closure> _functions;
		private Random _random;

		public StandardEnvironment(bool ignoreCase = true)
		{
			LookupComparer = ignoreCase
				                 ? StringComparer.OrdinalIgnoreCase
				                 : StringComparer.Ordinal;

			_values = new Dictionary<string, object>(LookupComparer);
			_rows = new Dictionary<string, INamedValues>(LookupComparer);
			_functions = new Dictionary<FunKey, Closure>(new FunKeyComparer(LookupComparer));
			_random = new Random();

			RegisterStandardFunctions();
		}

		/// <summary>
		/// Use this <see cref="StringComparer"/> when comparing
		/// names in <see cref="Lookup(string,string)"/> overrides.
		/// </summary>
		protected StringComparer LookupComparer { get; }

		/// <summary>
		/// Look up a named value. If there is no value with the given
		/// <paramref name="name"/> (and optional <paramref name="qualifier"/>),
		/// an exception is thrown.
		/// </summary>
		/// <param name="name">The name, required.</param>
		/// <param name="qualifier">The qualifier, optional.</param>
		/// <returns>
		/// The value bound to the given name.
		/// </returns>
		public override object Lookup(string name, string qualifier)
		{
			if (string.IsNullOrEmpty(qualifier))
			{
				object value;
				if (_values.TryGetValue(name, out value))
				{
					return value;
				}

				var funKey = new FunKey(name);
				if (_functions.ContainsKey(funKey))
				{
					return new Function(name);
				}

				value = LookupUniqueField(name);
				return value == DBNull.Value ? null : value;
			}

			if (_rows.TryGetValue(qualifier, out INamedValues row))
			{
				if (row.Exists(name))
				{
					object value = row.GetValue(name);
					return value == DBNull.Value ? null : value;
				}
			}

			throw LookupError("No such field: {0}.{1}", qualifier, name);
		}

		private object LookupUniqueField(string name)
		{
			INamedValues uniqueRow = null;

			foreach (var row in _rows.Values)
			{
				if (row.Exists(name))
				{
					if (uniqueRow == null)
					{
						uniqueRow = row;
					}
					else
					{
						throw LookupError("Field name '{0}' is not unique; use a qualified name",
						                  name);
					}
				}
			}

			if (uniqueRow == null)
			{
				throw LookupError("No such field or function: {0}", name);
			}

			return uniqueRow.GetValue(name);
		}

		/// <summary>
		/// Invoke the given <paramref name="function"/> with the given
		/// <paramref name="args"/> and return the resulting value.
		/// </summary>
		/// <param name="function"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		/// <remarks>
		/// The function's parameters and the arguments passed must agree.
		/// If several functions with the same name were registered, the
		/// one with the same number of parameters as arguments are passed
		/// will be called. If there's no function with as many parameters
		/// as there are arguments, but there's a function with exactly
		/// one parameter of type array of object, then it will be called.
		/// Otherwise, an exception will be thrown.
		/// </remarks>
		public override object Invoke(Function function, params object[] args)
		{
			string name = function.Name;
			int arity = args?.Length ?? 0;

			Closure closure;

			var key = new FunKey(name, arity);
			if (_functions.TryGetValue(key, out closure))
			{
				return closure.Invoke(args);
			}

			key = new FunKey(name, -1); // varargs
			if (_functions.TryGetValue(key, out closure))
			{
				return closure.Invoke(new object[] {args});
			}

			throw InvocationError("No such function: {0}/{1}", function.Name, arity);
		}

		/// <summary>
		/// Create a binding in this environment that associates the
		/// given <paramref name="value"/> with the given (unqualified)
		/// <paramref name="name"/>. Later definitions replace earlier
		/// definitions of the same name.
		/// </summary>
		/// <param name="name">The name (not null, not empty)</param>
		/// <param name="value">The value (may be null)</param>
		public StandardEnvironment DefineValue(string name, object value)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));
			_values[name] = value;
			return this;
		}

		public StandardEnvironment ForgetValue(string name)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));
			_values.Remove(name);
			return this;
		}

		/// <summary>
		/// Create bindings in this environment associating the given
		/// values with their names (optionally qualified with the
		/// given <paramref name="qualifier"/>.
		/// </summary>
		/// <param name="values">The name/value pairs (required)</param>
		/// <param name="qualifier">The qualifier (optional)</param>
		public StandardEnvironment DefineFields(INamedValues values, string qualifier = null)
		{
			Assert.ArgumentNotNull(values, nameof(values));
			_rows[qualifier ?? string.Empty] = values;
			return this;
		}

		public StandardEnvironment ForgetFields(string qualifier)
		{
			_rows.Remove(qualifier ?? string.Empty);
			return this;
		}

		public StandardEnvironment ForgetAll()
		{
			_values.Clear();
			_rows.Clear();
			return this;
		}

		/// <summary>
		/// Register the given function (<paramref name="methodInfo"/>)
		/// with the given <paramref name="name"/> in this environment.
		/// <para/>
		/// You can register more than one function with the same name,
		/// if the functions differ in their number of parameters (arity).
		/// The evaluator will choose the actual function by the actual
		/// number of arguments at the time of invocation.
		/// </summary>
		/// <param name="name">The name (required)</param>
		/// <param name="methodInfo">The method (required)</param>
		/// <param name="state">The method's state (required for non-static methods)</param>
		/// <remarks>
		/// Later registrations overwrite earlier registrations
		/// for the same name/arity combination.
		/// <para/>
		/// The method can be static or instance or lambda; <paramref name="state"/>
		/// will be the implicit "this" parameter passed to all non-static methods
		/// (static methods ignore this parameter).
		/// <para/>
		/// The method need not be public (it is invoked through reflection).
		/// </remarks>
		public void Register(string name, MethodInfo methodInfo, object state = null)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));
			Assert.ArgumentNotNull(methodInfo, nameof(methodInfo));

			var nameKey = new FunKey(name);
			int arity = GetArity(methodInfo);
			_functions[nameKey] = null;

			var funKey = new FunKey(name, arity);
			_functions[funKey] = new Closure(methodInfo, state);
		}

		/// <summary>
		/// See <see cref="Register(string, MethodInfo, object)"/>.
		/// </summary>
		public void Register<TResult>(string name, Func<TResult> func, object state = null)
		{
			Assert.ArgumentNotNull(func, nameof(func));
			var methodInfo = Assert.NotNull(func.Method, "func.Method is null");

			Register(name, methodInfo, state);
		}

		/// <summary>
		/// See <see cref="Register(string, MethodInfo, object)"/>.
		/// </summary>
		public void Register<T, TResult>(string name, Func<T, TResult> func, object state = null)
		{
			Assert.ArgumentNotNull(func, nameof(func));
			var methodInfo = Assert.NotNull(func.Method, "func.Method is null");

			Register(name, methodInfo, state);
		}

		/// <summary>
		/// See <see cref="Register(string, MethodInfo, object)"/>.
		/// </summary>
		public void Register<T1, T2, TResult>(string name, Func<T1, T2, TResult> func,
		                                      object state = null)
		{
			Assert.ArgumentNotNull(func, nameof(func));
			var methodInfo = Assert.NotNull(func.Method, "func.Method is null");

			Register(name, methodInfo, state);
		}

		/// <summary>
		/// See <see cref="Register(string, MethodInfo, object)"/>.
		/// </summary>
		public void Register<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> func,
		                                          object state = null)
		{
			Assert.ArgumentNotNull(func, nameof(func));
			var methodInfo = Assert.NotNull(func.Method, "func.Method is null");

			Register(name, methodInfo, state);
		}

		public void Register<T1, T2, T3, T4, TResult>(string name,
		                                              Func<T1, T2, T3, T4, TResult> func,
		                                              object state = null)
		{
			Assert.ArgumentNotNull(func, nameof(func));
			var methodInfo = Assert.NotNull(func.Method, "func.Method is null");

			Register(name, methodInfo, state);
		}

		/// <summary>
		/// Initialize (or re-initialize) the random number generator
		/// with the given seed value. A zero or negative value will
		/// initialize the generator with a "random" (time based) seed.
		/// </summary>
		[Obsolete("Use SetRandom() instead")]
		public int RandomSeed
		{
			set => _random = value <= 0
				                 ? new Random()
				                 : new Random(value);
		}

		public StandardEnvironment SetRandom(Random random)
		{
			_random = random ?? new Random();
			return this;
		}

		#region Non-public methods

		[StringFormatMethod("format")]
		private static EvaluationException LookupError(string format, params object[] args)
		{
			return new EvaluationException(string.Format(format, args));
		}

		[StringFormatMethod("format")]
		private static EvaluationException InvocationError(string format, params object[] args)
		{
			return new EvaluationException(string.Format(format, args));
		}

		protected static string Canonical(string s)
		{
			if (s == null) return null;
			s = s.Trim();
			return s.Length > 0 ? s : null;
		}

		protected static int GetArity(MethodInfo methodInfo)
		{
			var parameters = methodInfo.GetParameters();
			int arity = parameters.Length;

			if (parameters.Length == 1)
			{
				Type parameterType = parameters[0].ParameterType;
				if (parameterType.IsArray)
				{
					Type baseType = typeof(object);
					Type elementType = parameterType.GetElementType();
					if (baseType.IsAssignableFrom(elementType))
					{
						arity = -1; // varargs
					}
				}
			}

			return arity;
		}

		#endregion

		#region The Standard Funtions

		// Functions: stay close to typical SQL scalar functions.
		// ROUND/1, ROUND/2, TRUNC/1, ABS/1, CEIL/1, FLOOR/1,
		// LCASE, UCASE, LPAD, RPAD, TRIM, LENGTH, SUBSTR, CONCAT,
		// DECODE/*, REGEX/2, REGEX/3,
		// RAND/0, RAND/1, RAND/2, RANDPICK/*, MIN/*, MAX/*
		// TODO: INSTR, COALESCE/*, NULLIF/2
		// TODO: date and time functions?
		// Note: NVL is Oracle specific; it's a special case of COALESCE
		// TODO: CHOOSE(index, v1, v2, ...) (return null if index not in 1..n)

		// Notice the frequent use of type object when we actually expect a number.
		// Reason: the value could be null! Whence the object. We could also use
		// a double? (nullable type). Notice that the cast "(int) obj" will
		// fail if the obj represents a double, because it can only be unboxed
		// to the original type (a double). Two approaches: Convert.ToInt32(obj)
		// or cast twice: "(int)(double)obj". Prefer the former, because it
		// doesn't assume any type (it uses IConvertible internally).

		private void RegisterStandardFunctions()
		{
			Register<object, object>("ABS", Abs);
			Register<object, object>("CEIL", Ceil);
			Register<object, object>("FLOOR", Floor);
			Register<object, object>("ROUND", Round);
			Register<object, object, object>("ROUND", Round);
			Register<object, object>("TRUNC", Trunc);

			Register("RAND", Rand, this);
			Register<object, object>("RAND", Rand, this);
			Register<object, object, object>("RAND", Rand, this);
			Register<object[], object>("RANDPICK", RandPick, this);

			Register<object[], object>("MIN", Min, this);
			Register<object[], object>("MAX", Max, this);

			Register<string, string>("TRIM", Trim);
			Register<string, string, string>("TRIM", Trim);
			Register<string, string>("UCASE", Upper);
			Register<string, string>("LCASE", Lower);
			Register<object, double, string>("LPAD", PadLeft);
			Register<object, double, string, string>("LPAD", PadLeft);
			Register<object, double, string>("RPAD", PadRight);
			Register<object, double, string, string>("RPAD", PadRight);
			Register<string, object, string>("SUBSTR", Substring);
			Register<string, object, object, string>("SUBSTR", Substring);
			Register<object, string>("CONCAT", Concat);
			Register<object, object, string>("CONCAT", Concat);
			Register<object[], string>("CONCAT", Concat);
			Register<string, object>("LENGTH", Length);

			Register<object[], object>("DECODE", Decode, this);
			Register<object[], object>("WHEN", When, this);

			// Conversion functions?
			// TO_CHAR, TO_NUMBER, TO_BOOL, TO_DATE?
			//
			// More mathematics?
			// Sqrt(r), Pow(r,e), Exp(r), Log(r), Sin(r), Cos(r), Tan(r), Asin(r), Acos(r), Atan(r), Atan(dy,dx)
			//
			// Choose à la Access?
			// CHOOSE(k, v1, v2, ... vn) is v1 if k=1, v2 if k=2, ... vn if k=n, null otherwise.
			// Notice that this is a special case of DECODE (but may be useful syntactic sugar).

			Register<string, string, object>("REGEX", RegexMatch);
			Register<string, string, string, string>("REGEX", RegexReplace);
		}

		private static object Abs(object value)
		{
			if (value == null)
				return null;
			if (IsNumeric(value))
				return Math.Abs(ToDouble(value));
			throw InvalidArgumentType("Abs", value);
		}

		private static object Ceil(object value)
		{
			if (value == null)
				return null;
			if (IsNumeric(value))
				return Math.Ceiling(ToDouble(value));
			throw InvalidArgumentType("Ceil", value);
		}

		private static object Floor(object value)
		{
			if (value == null)
				return null;
			if (IsNumeric(value))
				return Math.Floor(ToDouble(value));
			throw InvalidArgumentType("Floor", value);
		}

		private static object Round(object value)
		{
			if (value == null)
				return null;
			if (IsNumeric(value))
				return Math.Round(ToDouble(value));
			throw InvalidArgumentType("Round", value);
		}

		private static object Round(object value, object digits)
		{
			if (value == null)
				return null;
			if (digits == null)
				digits = 0;
			if (IsNumeric(value) && IsNumeric(digits))
			{
				int n = ToInt32(digits);
				return Math.Round(ToDouble(value), n);
			}

			throw InvalidArgumentTypes("Round", value, digits);
		}

		private static object Trunc(object value)
		{
			if (value == null)
				return null;
			if (IsNumeric(value))
				return Math.Truncate(ToDouble(value));
			throw InvalidArgumentType("Trunc", value);
		}

		public double Rand()
		{
			return _random.NextDouble();
		}

		public object Rand(object maxValue)
		{
			if (maxValue == null)
				return null;
			if (IsNumeric(maxValue))
				return _random.Next(ToInt32(maxValue));
			throw InvalidArgumentType("Rand", maxValue);
		}

		public object Rand(object minValue, object maxValue)
		{
			if (minValue == null || maxValue == null)
				return null;
			if (IsNumeric(minValue) && IsNumeric(maxValue))
				return _random.Next(ToInt32(minValue), ToInt32(maxValue));
			throw InvalidArgumentTypes("Rand", minValue, maxValue);
		}

		private object RandPick(object[] args)
		{
			if (args == null || args.Length < 1)
				return null;
			if (args.Length < 2)
				return args[0];
			int index = _random.Next(args.Length);
			return args[index];
		}

		private object Min(object[] args)
		{
			if (args == null || args.Length < 1)
				return null;
			if (args.Length < 2)
				return args[0];
			object result = args[0];
			for (int i = 1; i < args.Length; i++)
			{
				object next = args[i];
				int? order = Compare(next, result);
				if (order == null)
					return null;
				if (order < 0)
					result = next;
			}

			return result;
		}

		private object Max(object[] args)
		{
			if (args == null || args.Length < 1)
				return null;
			if (args.Length < 2)
				return args[0];
			object result = args[0];
			for (int i = 1; i < args.Length; i++)
			{
				object next = args[i];
				int? order = Compare(next, result);
				if (order == null)
					return null;
				if (order > 0)
					result = next;
			}

			return result;
		}

		private static string Trim(string value)
		{
			if (value == null)
				return null;
			return value.Trim(); // trims all white space, not just blanks!
		}

		private static string Trim(string value, string trimChars)
		{
			if (string.IsNullOrEmpty(trimChars))
				return Trim(value);
			if (value == null)
				return null;
			return value.Trim(trimChars.ToCharArray());
		}

		private static string Lower(string s)
		{
			return s?.ToLowerInvariant();
		}

		private static string Upper(string s)
		{
			return s?.ToUpperInvariant();
		}

		private static string PadLeft(object obj, double width, string padChar)
		{
			if (obj == null)
				return null;
			var text = ToString(obj);
			if (string.IsNullOrEmpty(padChar))
				padChar = " ";
			return text.PadLeft(ToInt32(width), padChar[0]);
		}

		private static string PadLeft(object obj, double width)
		{
			return PadLeft(obj, width, null);
		}

		private static string PadRight(object obj, double width, string padChar)
		{
			if (obj == null)
				return null;
			var text = ToString(obj);
			if (string.IsNullOrEmpty(padChar))
				padChar = " "; // one blank
			return text.PadRight(ToInt32(width), padChar[0]);
		}

		private static string PadRight(object obj, double width)
		{
			return PadRight(obj, width, null);
		}

		private static string Substring(string text, object index, object length)
		{
			if (length == null)
				return Substring(text, index);
			if (text == null || index == null)
				return null;
			int start = ToInt32(index);
			int count = ToInt32(length);
			if (start < 0)
			{
				count += start;
				start = 0;
			}

			if (start >= text.Length)
				return string.Empty;
			if (count < 1)
				return string.Empty;
			if (start + count >= text.Length)
				count = text.Length - start;
			return text.Substring(start, count);
		}

		private static string Substring(string text, object index)
		{
			if (text == null || index == null)
				return null;
			int start = ToInt32(index);
			if (start < 0)
				start = 0;
			if (start >= text.Length)
				return string.Empty;
			return text.Substring(start);
		}

		private static string Concat(object s)
		{
			return ToString(s);
		}

		private static string Concat(object s1, object s2)
		{
			return string.Concat(ToString(s1), ToString(s2));
		}

		private static string Concat(object[] args)
		{
			var strs = new string[args.Length];
			for (int i = 0; i < args.Length; i++)
				strs[i] = ToString(args[i]);
			return string.Concat(strs);
		}

		private static object Length(object value)
		{
			if (value == null)
				return null;
			var s = value as string;
			if (s != null)
				return s.Length;
			throw InvalidArgumentType("Length", value);
		}

		private object Decode(params object[] args)
		{
			// Note: Decode() considers two nulls to be equal!
			if (args == null || args.Length < 1)
				return null;
			object value = args[0];
			if (args.Length == 1)
				return value;
			int nPairs = (args.Length - 1) / 2;
			for (int i = 0; i < nPairs; i++)
			{
				// For consistency, use Compare(), not Equals(),
				// and treat null specially (as does Oracle):
				object candidate = args[1 + 2 * i];
				if (value == null && candidate == null)
					return args[1 + 2 * i + 1];
				int? rel = Compare(value, candidate);
				if (rel == 0)
					return args[1 + 2 * i + 1];
			}

			int iLast = 1 + nPairs * 2;
			if (iLast < args.Length)
				return args[iLast];
			return null;
		}

		// When() => null
		// When(deflt) => deflt
		// When(cond, expr) => cond ? expr : null
		// When(c1, e1, deflt) => c1 ? e1 : deflt
		// When(c1, e1, ..., deflt) => explicit default
		// When(c1, e1, ...) => NULL if no c_i matches
		private object When(params object[] args)
		{
			if (args == null || args.Length < 1)
				return null;
			int nPairs = args.Length / 2;
			for (int i = 0; i < nPairs; i++)
			{
				object cond = args[2 * i]; // cond[i]
				if (IsTrue(cond))
					return args[2 * i + 1]; // expr[i]
			}

			if (args.Length > 2 * nPairs)
				return args[args.Length - 1]; // explicit default
			return null; // implicit default
		}

		private static object RegexMatch(string pattern, string text)
		{
			if (pattern == null || text == null)
				return null;
			return Regex.Match(text, pattern).Success;
		}

		private static string RegexReplace(string pattern, string text, string replacement)
		{
			if (pattern == null || text == null || replacement == null)
				return null;
			return Regex.Replace(text, pattern, replacement);
		}

		#endregion

		#region Nested type: Closure

		private class Closure
		{
			private readonly MethodInfo _method;
			private readonly object _state;

			public Closure(MethodInfo method, object state = null)
			{
				_method = method;
				_state = state;
			}

			public object Invoke(params object[] arguments)
			{
				return _method.Invoke(_state, arguments);
			}

			public override string ToString()
			{
				return _method.Name;
			}
		}

		#endregion

		#region Nested type: FunKey

		private readonly struct FunKey
		{
			public readonly string Name;
			public readonly int? Arity;

			public FunKey(string name)
			{
				Name = Assert.NotNull(name, "name must not be null");
				Arity = null;
			}

			public FunKey(string name, int arity)
			{
				Name = Assert.NotNull(name, "name must not be null");
				Arity = arity;
			}

			public override string ToString()
			{
				if (Arity.HasValue)
				{
					return Arity < 0
						       ? string.Format("{0}/*", Name)
						       : string.Format("{0}/{1}", Name, Arity);
				}

				return Name;
			}
		}

		#endregion

		#region Nested type: FunKeyComparer

		private class FunKeyComparer : IComparer<FunKey>, IEqualityComparer<FunKey>
		{
			private readonly StringComparer _comparer;

			public FunKeyComparer(StringComparer comparer)
			{
				_comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
			}

			public bool Equals(FunKey x, FunKey y)
			{
				return _comparer.Equals(x.Name, y.Name) && x.Arity == y.Arity;
			}

			public int GetHashCode(FunKey obj)
			{
				unchecked
				{
					int code = _comparer.GetHashCode(obj.Name);
					code *= 397;
					code ^= obj.Arity.GetHashCode();
					return code;
				}
			}

			public int Compare(FunKey x, FunKey y)
			{
				int order = _comparer.Compare(x.Name, y.Name);

				if (order == 0)
				{
					if (x.Arity == null && y.Arity == null)
						return 0; // same
					if (x.Arity == null)
						return -1; // generic entry before arity entry
					if (y.Arity == null)
						return +1;

					order = x.Arity.Value - y.Arity.Value;
				}

				return Math.Sign(order);
			}
		}

		#endregion
	}
}
