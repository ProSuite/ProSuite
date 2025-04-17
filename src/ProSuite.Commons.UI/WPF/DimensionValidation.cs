using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;

namespace ProSuite.Commons.UI.WPF;

/// <summary>
/// Validation class for use with the <see cref="DimensionConverter"/>
/// value converter for WPF. Use the <see cref="UnitRequired"/> property
/// to control whether a unit is required or optional, and either the
/// <see cref="ValidUnits"/> or the <see cref="ValidUnitsText"/> property
/// to specify a list of valid units (the latter a comma-separated list
/// like "mm,pt,mu"). You may want to use the error message as a ToolTip.
/// </summary>
public class DimensionValidation : ValidationRule
{
	private UnitEqualityComparer _unitComparer;
	private string[] _validUnits;
	private string _validUnitsText;

	public string ValidUnitsText
	{
		get => _validUnitsText;
		set => SetValidUnits(value);
	}

	public string[] ValidUnits
	{
		get => _validUnits;
		set => SetValidUnits(value);
	}

	public bool UnitRequired { get; set; }

	private UnitEqualityComparer UnitComparer => _unitComparer ??= new UnitEqualityComparer();

	public override ValidationResult Validate(object value, CultureInfo cultureInfo)
	{
		try
		{
			var dim = Dimension.Parse(value as string, cultureInfo);

			if (dim.Unit is null)
			{
				var needUnit = dim.Value != 0 && !double.IsNaN(dim.Value);
				if (needUnit && UnitRequired)
				{
					var message = "Dimension requires a unit";
					if (_validUnits is { Length: > 0 })
					{
						var text = FormatValidUnits(_validUnits, "(none)");
						message += $" (one of: {text})";
					}
					return new ValidationResult(false, message);
				}
			}
			else
			{
				if (_validUnits is { Length: > 0 } &&
				    ! _validUnits.Contains(dim.Unit, UnitComparer))
				{
					var text = FormatValidUnits(_validUnits, "(none)");
					return new ValidationResult(
						false, $"Invalid unit: {dim.Unit} (expect one of: {text})");
				}
			}

			return new ValidationResult(true, null);
		}
		catch (Exception ex)
		{
			return new ValidationResult(false, ex.Message);
		}
	}

	private void SetValidUnits(string[] units)
	{
		_validUnits = units;
		_validUnitsText = FormatValidUnits(units);
	}

	private void SetValidUnits(string text)
	{
		_validUnits = ParseValidUnits(text);
		_validUnitsText = text;
	}

	private static string[] ParseValidUnits(string text)
	{
		if (string.IsNullOrEmpty(text))
			return Array.Empty<string>();
		return text.Split(',').Select(s => s.Trim()).ToArray();
	}

	private static string FormatValidUnits(string[] units, string noneText = null)
	{
		if (units is null) return string.Empty;

		IEnumerable<string> query = units;

		if (! string.IsNullOrEmpty(noneText))
		{
			query = query.Select(s => string.IsNullOrEmpty(s) ? noneText : s);
		}

		return string.Join(", ", query);
	}

	private class UnitEqualityComparer : IEqualityComparer<string>
	{
		private StringComparison Comparison { get; }

		public UnitEqualityComparer(StringComparison comparison = StringComparison.Ordinal)
		{
			Comparison = comparison;
		}

		public bool Equals(string x, string y)
		{
			if (string.IsNullOrWhiteSpace(x)) x = string.Empty;
			if (string.IsNullOrWhiteSpace(y)) y = string.Empty;
			return string.Equals(x, y, Comparison);
		}

		public int GetHashCode(string obj)
		{
			return obj.GetHashCode();
		}
	}
}
