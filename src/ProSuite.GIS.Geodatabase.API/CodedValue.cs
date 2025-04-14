using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.GIS.Geodatabase.API
{
	/// <summary>
	/// An coded value pair (key/name). Immutable value type.
	/// </summary>
	public class CodedValue
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="CodedValue"/> class.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="name">The name.</param>
		public CodedValue([NotNull] object value, [NotNull] string name)
		{
			Assert.ArgumentNotNull(value, nameof(value));
			Assert.ArgumentNotNull(name, nameof(name));

			Value = value;
			Name = name;
		}

		#endregion

		[NotNull]
		public object Value { get; }

		[NotNull]
		public string Name { get; }

		public bool ValueEquals([CanBeNull] object value)
		{
			if (value == null)
			{
				return false;
			}

			object compareValue = Value;

			if (value is short && compareValue is int)
			{
				return compareValue.Equals(Convert.ToInt32(value));
			}

			if (value is int && compareValue is short)
			{
				return Convert.ToInt32(compareValue).Equals(value);
			}

			return value.Equals(compareValue);
		}

		#region Object overrides

		public override string ToString()
		{
			// suitable for display in UI
			return Name;
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}

			var codedValue = obj as CodedValue;
			if (codedValue == null)
			{
				return false;
			}

			return Equals(Value, codedValue.Value) && Equals(Name, codedValue.Name);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode() + 29 * Name.GetHashCode();
		}

		#endregion
	}
}
