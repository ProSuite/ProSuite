using System;
using System.Data;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	/// <summary>
	/// Provides the value of one computed column of a constraint expression, for a given
	/// subject (the thing the constraint is evaluated on) and a cache of derived properties
	/// of that subject.
	/// </summary>
	/// <typeparam name="TSubject">The type of the examined object.</typeparam>
	/// <typeparam name="TCache">The type of the property cache for the examined object.</typeparam>
	internal class ColumnHandler<TSubject, TCache>
	{
		[NotNull] private readonly Type _type;
		[NotNull] private readonly Func<TSubject, TCache, object> _valueFunction;

		[NotNull] private readonly Func<TSubject, object, IFormatProvider, string>
			_formatFunction;

		public ColumnHandler(
			[NotNull] string columnName,
			[NotNull] Type type,
			[NotNull] Func<TSubject, TCache, object> valueFunction,
			[CanBeNull] string valueFormat = null)
			: this(columnName, type, valueFunction, GetFormatFunction(valueFormat)) { }

		public ColumnHandler(
			[NotNull] string columnName,
			[NotNull] Type type,
			[NotNull] Func<TSubject, TCache, object> valueFunction,
			[NotNull] Func<TSubject, object, IFormatProvider, string> formatFunction)
		{
			Assert.ArgumentNotNullOrEmpty(columnName, nameof(columnName));
			Assert.ArgumentNotNull(type, nameof(type));
			Assert.ArgumentNotNull(valueFunction, nameof(valueFunction));
			Assert.ArgumentNotNull(formatFunction, nameof(formatFunction));

			ColumnName = columnName;
			_type = type;
			_valueFunction = valueFunction;
			_formatFunction = formatFunction;
		}

		[NotNull]
		public string ColumnName { get; }

		[NotNull]
		public DataColumn CreateColumn()
		{
			return new DataColumn(ColumnName, _type);
		}

		public object GetValue([CanBeNull] TSubject subject, [NotNull] TCache cache)
		{
			return _valueFunction(subject, cache);
		}

		[NotNull]
		public string FormatValue([CanBeNull] TSubject subject,
		                          [NotNull] IFormatProvider formatProvider,
		                          [NotNull] TCache cache)
		{
			object value = GetValue(subject, cache);

			return _formatFunction(subject, value, formatProvider);
		}

		/// <summary>
		/// Returns a column handler for a larger subject that contains this handler's
		/// subject, allowing the column to be reused unchanged in another constraint.
		/// </summary>
		[NotNull]
		public ColumnHandler<TOuterSubject, TOuterCache> Adapt<TOuterSubject, TOuterCache>(
			[NotNull] Func<TOuterSubject, TSubject> getSubject,
			[NotNull] Func<TOuterCache, TCache> getCache)
		{
			Assert.ArgumentNotNull(getSubject, nameof(getSubject));
			Assert.ArgumentNotNull(getCache, nameof(getCache));

			return new ColumnHandler<TOuterSubject, TOuterCache>(
				ColumnName, _type,
				(subject, cache) => _valueFunction(getSubject(subject), getCache(cache)),
				(subject, value, formatProvider) =>
					_formatFunction(getSubject(subject), value, formatProvider));
		}

		[NotNull]
		private static Func<TSubject, object, IFormatProvider, string> GetFormatFunction(
			[CanBeNull] string valueFormat)
		{
			return (subject, value, formatProvider) =>
			{
				if (value is DBNull)
				{
					return "<NULL>";
				}

				string format;
				if (valueFormat == null)
				{
					format = value is int ? "{0:N0}" : "{0}";
				}
				else
				{
					format = valueFormat;
				}

				return string.Format(
					formatProvider,
					format, value);
			};
		}
	}
}
