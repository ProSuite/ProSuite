using System;
using System.Diagnostics;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Essentials.Assertions
{
	[DebuggerStepThrough]
	public static class Assert
	{
		[ContractAnnotation("condition: true => halt")]
		public static void False(bool condition, [NotNull] string message)
		{
			if (condition)
			{
				throw new AssertionException(message);
			}
		}

		[StringFormatMethod("format")]
		[ContractAnnotation("condition: true => halt")]
		public static void False(bool condition, string format, params object[] args)
		{
			if (condition)
			{
				throw new AssertionException(string.Format(format, args));
			}
		}

		[ContractAnnotation("condition: false => halt")]
		public static void True(bool condition, [NotNull] string message)
		{
			if (! condition)
			{
				throw new AssertionException(message);
			}
		}

		[StringFormatMethod("format")]
		[ContractAnnotation("condition: false => halt")]
		public static void True(bool condition, string format, params object[] args)
		{
			if (! condition)
			{
				throw new AssertionException(string.Format(format, args));
			}
		}

		/// <summary>
		/// Asserts that the specified instance is assignable to a type (i.e. <c>targetType variable = o</c> succeeds).
		/// </summary>
		/// <param name="o">The instance.</param>
		/// <param name="targetType">The type the instance should be assignable to.</param>
		public static void IsAssignable([NotNull] object o,
		                                [NotNull] Type targetType)
		{
			if (targetType.IsInstanceOfType(o))
			{
				return;
			}

			string message = string.Format("type {0} is not assignable from type {1}",
			                               targetType.FullName, o.GetType().FullName);
			throw new AssertionException(message);
		}

		/// <summary>
		/// Asserts that the specified instance is assignable to a type (i.e. <c>targetType variable = o</c> succeeds).
		/// </summary>
		/// <param name="o">The instance.</param>
		/// <param name="targetType">The type the instance should be assignable to.</param>
		/// <param name="message">The assertion message.</param>
		public static void IsAssignable([NotNull] object o,
		                                [NotNull] Type targetType,
		                                [NotNull] string message)
		{
			if (! targetType.IsInstanceOfType(o))
			{
				throw new AssertionException(message);
			}
		}

		/// <summary>
		/// Asserts that the specified target type is assignable from a given source type
		/// </summary>
		/// <param name="targetType">Type of the target.</param>
		/// <param name="fromType">From type.</param>
		public static void IsAssignableFrom([NotNull] Type targetType,
		                                    [NotNull] Type fromType)
		{
			if (targetType.IsAssignableFrom(fromType))
			{
				return;
			}

			string message = string.Format("type {0} is not assignable from type {1}",
			                               targetType.FullName, fromType.FullName);
			throw new AssertionException(message);
		}

		public static void IsAssignableFrom([NotNull] Type targetType,
		                                    [NotNull] Type fromType,
		                                    [NotNull] string message)
		{
			if (! targetType.IsAssignableFrom(fromType))
			{
				throw new AssertionException(message);
			}
		}

		/// <summary>
		/// Determines whether an object is of a specified type (exact type match)
		/// </summary>
		/// <param name="o">The object to test.</param>
		/// <param name="type">The type.</param>
		public static void IsExactType([NotNull] object o, [NotNull] Type type)
		{
			if (o.GetType() == type)
			{
				return;
			}

			string message = string.Format("expected type: {0} - actual type: {1}",
			                               type.FullName, o.GetType().FullName);
			throw new AssertionException(message);
		}

		/// <summary>
		/// Determines whether an object is of a specified type (exact type match)
		/// </summary>
		/// <param name="o">The object to test.</param>
		/// <param name="type">The type.</param>
		/// <param name="message">Error message if assertion fails</param>
		public static void IsExactType([NotNull] object o,
		                               [NotNull] Type type,
		                               [NotNull] string message)
		{
			if (o.GetType() == type)
			{
				return;
			}

			throw new AssertionException(message);
		}

		[NotNull]
		[ContractAnnotation("s: null => halt")]
		public static string NotNullOrEmpty(string s)
		{
			if (string.IsNullOrEmpty(s))
			{
				throw new AssertionException("not null/empty assertion failed");
			}

			return s;
		}

		[NotNull]
		[ContractAnnotation("s: null => halt")]
		public static string NotNullOrEmpty(string s, [NotNull] string message)
		{
			if (string.IsNullOrEmpty(s))
			{
				throw new AssertionException(message);
			}

			return s;
		}

		[NotNull]
		[StringFormatMethod("format")]
		[ContractAnnotation("s: null => halt")]
		public static string NotNullOrEmpty(string s, string format, params object[] args)
		{
			// DON'T delegate formatted message to NotNullOrEmpty, to save
			// cost of formatting if not null or empty
			if (string.IsNullOrEmpty(s))
			{
				throw new AssertionException(string.Format(format, args));
			}

			return s;
		}

		[NotNull]
		[ContractAnnotation("null => halt")]
		public static T NotNull<T>(T o) where T : class
		{
			if (o == null)
			{
				throw new AssertionException("not null assertion failed");
			}

			return o;
		}

		[NotNull]
		[ContractAnnotation("o: null => halt")]
		public static T? NotNull<T>(T? o) where T : struct
		{
			if (o == null)
			{
				throw new AssertionException("not null assertion failed");
			}

			return o;
		}

		[NotNull]
		[ContractAnnotation("o: null => halt")]
		public static T NotNull<T>(T o, [NotNull] string message) where T : class
		{
			if (o == null)
			{
				throw new AssertionException(message);
			}

			return o;
		}

		[NotNull]
		[ContractAnnotation("o: null => halt")]
		public static T NotNull<T>(T o, [NotNull] FormattableString message) where T : class
		{
			if (o == null)
			{
				throw new AssertionException(message.ToString());
			}

			return o;
		}

		[NotNull]
		[ContractAnnotation("o: null => halt")]
		public static T? NotNull<T>(T? o, [NotNull] string message) where T : struct
		{
			if (o == null)
			{
				throw new AssertionException(message);
			}

			return o;
		}

		[StringFormatMethod("format")]
		[ContractAnnotation("o: null => halt")]
		[NotNull]
		public static T NotNull<T>(T o, string format, params object[] args) where T : class
		{
			// DON'T delegate formatted message to NotNull, to save
			// cost of formatting if not null
			if (o == null)
			{
				throw new AssertionException(string.Format(format, args));
			}

			return o;
		}

		[StringFormatMethod("format")]
		[ContractAnnotation("o: null => halt")]
		[NotNull]
		public static T? NotNull<T>(T? o, string format, params object[] args)
			where T : struct
		{
			// DON'T delegate formatted message to NotNull, to save
			// cost of formatting if not null
			if (o == null)
			{
				throw new AssertionException(string.Format(format, args));
			}

			return o;
		}

		/// <summary>
		/// Asserts that a double is not NaN (Not a Number)
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns></returns>
		public static double NotNaN(double value)
		{
			if (double.IsNaN(value))
			{
				throw new AssertionException("not NaN assertion failed");
			}

			return value;
		}

		/// <summary>
		/// Asserts that a double is not NaN (Not a Number)
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="message">The message.</param>
		/// <returns></returns>
		public static double NotNaN(double value, [NotNull] string message)
		{
			if (double.IsNaN(value))
			{
				throw new AssertionException(message);
			}

			return value;
		}

		/// <summary>
		/// Asserts that a double is not NaN (Not a Number)
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="format">A composite format string.</param>
		/// <param name="args">An array containing zero or more objects to format. </param>
		/// <returns></returns>
		[StringFormatMethod("format")]
		public static double NotNaN(double value, string format, params object[] args)
		{
			if (double.IsNaN(value))
			{
				throw new AssertionException(string.Format(format, args));
			}

			return value;
		}

		/// <summary>
		/// Asserts that a float is not NaN (Not a Number)
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns></returns>
		public static float NotNaN(float value)
		{
			if (double.IsNaN(value))
			{
				throw new AssertionException("not NaN assertion failed");
			}

			return value;
		}

		/// <summary>
		/// Asserts that a float is not NaN (Not a Number)
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="message">The message.</param>
		/// <returns></returns>
		public static float NotNaN(float value, [NotNull] string message)
		{
			if (double.IsNaN(value))
			{
				throw new AssertionException(message);
			}

			return value;
		}

		/// <summary>
		/// Asserts that a float is not NaN (Not a Number)
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="format">A composite format string.</param>
		/// <param name="args">An array containing zero or more objects to format. </param>
		/// <returns></returns>
		[StringFormatMethod("format")]
		public static float NotNaN(float value, string format, params object[] args)
		{
			if (double.IsNaN(value))
			{
				throw new AssertionException(string.Format(format, args));
			}

			return value;
		}

		/// <summary>
		/// Asserts that an argument is not null. 
		/// </summary>
		/// <param name="value">The argument value.</param>
		[ContractAnnotation("value: null => halt")]
		public static void ArgumentNotNull(object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException();
			}
		}

		/// <summary>
		/// Asserts that an argument is not null. 
		/// </summary>
		/// <param name="value">The argument value.</param>
		/// <param name="paramName">Name of the parameter.</param>
		[ContractAnnotation("value: null => halt")]
		public static void ArgumentNotNull(
			object value, [InvokerParameterName] [CanBeNull] string paramName)
		{
			if (value == null)
			{
				throw new ArgumentNullException(paramName);
			}
		}

		/// <summary>
		/// Asserts that a string argument is not null or empty.
		/// </summary>
		/// <param name="value">The argument value.</param>
		[ContractAnnotation("value: null => halt")]
		public static void ArgumentNotNullOrEmpty(string value)
		{
			if (value == null)
			{
				throw new ArgumentNullException();
			}

			if (value.Length == 0)
			{
				throw new ArgumentException("String argument must not be empty");
			}
		}

		/// <summary>
		/// Asserts that a string argument is not null or empty.
		/// </summary>
		/// <param name="value">The argument value.</param>
		/// <param name="paramName">Name of the parameter.</param>
		[ContractAnnotation("value: null => halt")]
		public static void ArgumentNotNullOrEmpty(
			string value, [InvokerParameterName] [CanBeNull] string paramName)
		{
			if (value == null)
			{
				throw new ArgumentNullException(paramName);
			}

			if (value.Length == 0)
			{
				throw new ArgumentException("String argument must not be empty", paramName);
			}
		}

		/// <summary>
		/// Asserts that a double argument is not NaN (Not a Number)
		/// </summary>
		/// <param name="value">The argument value.</param>
		/// <param name="paramName">Name of the parameter.</param>
		public static void ArgumentNotNaN(
			double value, [InvokerParameterName] [CanBeNull] string paramName)
		{
			if (double.IsNaN(value))
			{
				throw new ArgumentException("double argument must not be NaN", paramName);
			}
		}

		/// <summary>
		/// Asserts that a float argument is not NaN (Not a Number)
		/// </summary>
		/// <param name="value">The argument value.</param>
		/// <param name="paramName">Name of the parameter.</param>
		public static void ArgumentNotNaN(
			float value, [InvokerParameterName] [CanBeNull] string paramName)
		{
			if (float.IsNaN(value))
			{
				throw new ArgumentException("float argument must not be NaN", paramName);
			}
		}

		/// <summary>
		/// Asserts that a given condition on arguments is true
		/// </summary>
		/// <param name="condition">The condition to assert.</param>
		/// <param name="message">The message.</param>
		[ContractAnnotation("condition: false => halt")]
		public static void ArgumentCondition(bool condition, [NotNull] string message)
		{
			if (! condition)
			{
				throw new ArgumentException(message);
			}
		}

		/// <summary>
		/// Asserts that a given condition on a specified argument is true
		/// </summary>
		/// <param name="condition">The condition to assert.</param>
		/// <param name="message">The message.</param>
		/// <param name="paramName">Name of the parameter.</param>
		[ContractAnnotation("condition: false => halt")]
		public static void ArgumentCondition(
			bool condition, [CanBeNull] string message,
			[InvokerParameterName] [CanBeNull] string paramName)
		{
			if (! condition)
			{
				throw new ArgumentException(message, paramName);
			}
		}

		[StringFormatMethod("format")]
		[ContractAnnotation("condition: false => halt")]
		public static void ArgumentCondition(bool condition, string format,
		                                     params object[] args)
		{
			if (! condition)
			{
				throw new ArgumentException(string.Format(format, args));
			}
		}

		[ContractAnnotation("=> halt")]
		public static void CantReach(string message)
		{
			throw new UnreachableCodeException(message);
		}

		[StringFormatMethod("format")]
		[ContractAnnotation("=> halt")]
		public static void CantReach(string format, params object[] args)
		{
			// ReSharper disable once RedundantStringFormatCall
			CantReach(string.Format(format, args));
		}

		[ContractAnnotation("o: notnull => halt")]
		public static void Null(object o)
		{
			if (o != null)
			{
				throw new AssertionException("null assertion failed");
			}
		}

		[ContractAnnotation("o: notnull => halt")]
		public static void Null(object o, [NotNull] string message)
		{
			if (o != null)
			{
				throw new AssertionException(message);
			}
		}

		[StringFormatMethod("format")]
		[ContractAnnotation("o: notnull => halt")]
		public static void Null(object o, string format, params object[] args)
		{
			if (o != null)
			{
				throw new AssertionException(string.Format(format, args));
			}
		}

		public static void AreEqual(int expected, int actual, [CanBeNull] string message)
		{
			AreEqual<int>(expected, actual, message);
		}

		public static void AreEqual(double expected, double actual,
		                            [CanBeNull] string message)
		{
			AreEqual<double>(expected, actual, message);
		}

		public static void AreEqual(string expected, string actual,
		                            [CanBeNull] string message)
		{
			AreEqual<string>(expected, actual, message);
		}

		public static void AreEqual<T>(T expected, T actual, [CanBeNull] string message)
		{
			if (Equals(expected, actual))
			{
				return;
			}

			string fullMessage = string.Format("{0}; expected: {1} - actual: {2}",
			                                   message, expected, actual);
			throw new AssertionException(fullMessage);
		}

		[StringFormatMethod("format")]
		public static void AreEqual<T>(T expected, T actual, string format,
		                               params object[] args)
		{
			if (Equals(expected, actual))
			{
				return;
			}

			string fullMessage = string.Format("{0}; expected: {1} - actual: {2}",
			                                   string.Format(format, args),
			                                   expected, actual);

			throw new AssertionException(fullMessage);
		}

		[ContractAnnotation("=> halt")]
		public static void Fail([NotNull] string message)
		{
			throw new AssertionException(message);
		}

		[StringFormatMethod("format")]
		[ContractAnnotation("=> halt")]
		public static void Fail(string format, params object[] args)
		{
			throw new AssertionException(string.Format(format, args));
		}
	}
}
