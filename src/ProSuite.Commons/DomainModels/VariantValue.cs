using System;
using System.Globalization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.DomainModels
{
	public class VariantValue : IEquatable<VariantValue>
	{
		// TODO revise culture!!
		private static readonly CultureInfo _culture = new CultureInfo("de-CH", false);
		private string _stringValue;
		private VariantValueType _type = VariantValueType.Null;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="VariantValue"/> class.
		/// </summary>
		/// <remarks>for nhibernate</remarks>
		protected VariantValue() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="VariantValue"/> class.
		/// </summary>
		/// <param name="value">The value.</param>
		public VariantValue([CanBeNull] object value) : this(value, VariantValueType.Null) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="VariantValue"/> class.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="type">The type.</param>
		public VariantValue([CanBeNull] object value, VariantValueType type)
		{
			_type = type;

			Value = value;
		}

		#endregion

		[CanBeNull]
		public object Value
		{
			get
			{
				Type type = GetType(_type);

				if (type == null)
				{
					return null;
				}

				const bool nullAlwaysParses = true;
				object result;
				ConversionUtils.ParseTo(type, _stringValue, _culture,
				                        nullAlwaysParses, out result);
				return result;
			}
			set
			{
				Assert.True(IsValidValue(value),
				            "Value {0} is not valid for current type {1}", value, _type);

				if (_type == VariantValueType.Null)
				{
					// infer type from value
					_type = GetValueType(value);
				}

				_stringValue = GetStringValue(value);
			}
		}

		[UsedImplicitly]
		private string StringValue => _stringValue;

		public VariantValueType Type
		{
			get { return _type; }
			set
			{
				if (Equals(_type, value))
				{
					return;
				}

				Assert.True(CanChangeTo(value),
				            "Unable to change to type {0} for current value ({1})",
				            value, _stringValue);
				_type = value;
			}
		}

		#region Object overrides

		public bool Equals(VariantValue variantValue)
		{
			if (variantValue == null)
			{
				return false;
			}

			return Equals(_stringValue, variantValue._stringValue) &&
			       Equals(_type, variantValue._type);
		}

		public override string ToString()
		{
			return string.Format("Type={0} Value={1}", _type, _stringValue ?? "<null>");
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			return Equals(obj as VariantValue);
		}

		public override int GetHashCode()
		{
			return (_stringValue != null
				        ? _stringValue.GetHashCode()
				        : 0) +
			       29 * _type.GetHashCode();
		}

		#endregion

		#region Non-public members

		private static string GetStringValue(object value)
		{
			return value == null
				       ? null
				       : string.Format(_culture, "{0}", value);
		}

		private static Type GetType(VariantValueType valueType)
		{
			switch (valueType)
			{
				case VariantValueType.Null:
					return null;

				case VariantValueType.Boolean:
					return typeof(bool);

				case VariantValueType.DateTime:
					return typeof(DateTime);

				case VariantValueType.Double:
					return typeof(double);

				case VariantValueType.Integer:
					return typeof(int);

				case VariantValueType.String:
					return typeof(string);

				default:
					throw new ArgumentException(
						string.Format("Unknown value type: {0}", valueType));
			}
		}

		private static VariantValueType GetValueType(object value)
		{
			VariantValueType variantType;
			if (! TryGetValueType(value, out variantType))
			{
				throw new ArgumentException("Unsupported type: {0}", value.GetType().Name);
			}

			return variantType;
		}

		private static bool TryGetValueType(object value, out VariantValueType variantType)
		{
			bool ok = true;
			if (value == null)
			{
				variantType = VariantValueType.Null;
			}
			else
			{
				Type type = value.GetType();

				if (type == typeof(double))
				{
					variantType = VariantValueType.Double;
				}
				else if (type == typeof(int))
				{
					variantType = VariantValueType.Integer;
				}
				else if (type == typeof(bool))
				{
					variantType = VariantValueType.Boolean;
				}
				else if (type == typeof(string))
				{
					variantType = VariantValueType.String;
				}
				else if (type == typeof(DateTime))
				{
					variantType = VariantValueType.DateTime;
				}
				else if (type.IsEnum)
				{
					variantType = VariantValueType.Integer;
				}
				else
				{
					variantType = VariantValueType.Null;
					ok = false;
				}
			}

			return ok;
		}

		#endregion

		public bool CanChangeTo(VariantValueType type)
		{
			if (_stringValue == null)
			{
				return true;
			}

			if (type == VariantValueType.Null)
			{
				return true;
			}

			object result;
			return ConversionUtils.TryParseTo(GetType(type),
			                                  _stringValue, _culture, out result);
		}

		public bool IsValidValue([CanBeNull] object value)
		{
			if (value == null)
			{
				return true;
			}

			if (value == DBNull.Value)
			{
				return true;
			}

			if (_type == VariantValueType.Null)
			{
				VariantValueType variantType;
				return TryGetValueType(value, out variantType);
			}

			object result;
			return ConversionUtils.TryParseTo(GetType(_type),
			                                  GetStringValue(value), _culture,
			                                  out result);
		}
	}
}