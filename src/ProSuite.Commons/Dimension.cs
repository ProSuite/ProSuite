using System;
using System.Globalization;

namespace ProSuite.Commons
{
	/// <summary>
	/// A dimension is a value (double) with a unit (string).
	/// The unit is an arbitrary (trimmed) string; it is the
	/// client's responsibility to impose restrictions and provide
	/// conversions.
	/// </summary>
	/// <remarks>When binding in WPF, consider using the classes
	/// DimensionConverter and DimensionValidation</remarks>
	public struct Dimension : IEquatable<Dimension>
	{
		public double Value { get; set; }
		public string Unit { get; set; }

		public Dimension(double value, string unit)
		{
			Value = value;
			Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
		}

		#region Equality

		public bool Equals(Dimension other)
		{
			return Value.Equals(other.Value) && Unit == other.Unit;
		}

		public override bool Equals(object obj)
		{
			return obj is Dimension other && Equals(other);
		}

		public override int GetHashCode()
		{
			// TODO once we're on .NET only: return HashCode.Combine(Value, Unit);

			unchecked
			{
				return (Value.GetHashCode() * 397) ^ (Unit != null ? Unit.GetHashCode() : 0);
			}
		}

		public static bool operator ==(Dimension left, Dimension right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Dimension left, Dimension right)
		{
			return !left.Equals(right);
		}

		#endregion

		public override string ToString()
		{
			return ToString(CultureInfo.CurrentCulture);
		}

		public string ToString(CultureInfo culture)
		{
			return Unit is null || double.IsNaN(Value)
				       ? Value.ToString(culture)
				       : string.Format(culture, "{0} {1}", Value, Unit);
		}

		public static bool TryParse(string text, CultureInfo culture, out Dimension value)
		{
			if (text is null)
			{
				value = new Dimension(0.0, null);
				return true;
			}

			text = text.Trim();

			var style = NumberStyles.Float & ~NumberStyles.AllowExponent;

			if (double.TryParse(text, style, culture, out double number))
			{
				// handles NaN and Infinity (without a unit)
				value = new Dimension(number, null);
				return true;
			}

			// "0"  "0.3 mm"  "1 pt"  "-10mu"  ".25"

			int i = 0;
			// Skip over numeric part:
			for (; i < text.Length; i++)
			{
				char c = text[i];
				if (char.IsDigit(c)) continue;
				if (c == '-' || c == '+' || c == '.' || c == ',' || c == '\'') continue;
				break;
			}

			if (i == 0 || i == 1 && (text[0] == '-' || text[0] == '+'))
			{
				// Skip over "NaN" and "Infinity" and "-Infinity":
				for (; i < text.Length; i++)
				{
					if (char.IsWhiteSpace(text, i)) break;
				}
			}

			string unit = text.Substring(i).Trim();

			if (double.TryParse(text.Substring(0, i), style, culture, out number))
			{
				value = new Dimension(number, unit);
				return true;
			}

			value = new Dimension(double.NaN, null);
			return false;
		}

		public static Dimension Parse(string text, CultureInfo culture)
		{
			if (TryParse(text, culture, out var value))
			{
				return value;
			}

			throw new FormatException($"Cannot parse “{text}” as a dimension");
		}
	}
}
