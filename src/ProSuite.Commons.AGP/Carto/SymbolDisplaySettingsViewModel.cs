using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Data;

namespace ProSuite.Commons.AGP.Carto;

public class SymbolDisplaySettingsViewModel : INotifyPropertyChanged
{
	private string _scopeMessage;
	private bool _avoidSLMWithoutSLD;
	private bool _useScaleRange;
	private double _minScaleDenominator;
	private double _maxScaleDenominator;

	public string ScopeMessage
	{
		get => _scopeMessage;
		set
		{
			if (_scopeMessage != value)
			{
				_scopeMessage = value;
				OnPropertyChanged();
			}
		}
	}

	public bool AvoidSLMWithoutSLD
	{
		get => _avoidSLMWithoutSLD;
		set
		{
			if (_avoidSLMWithoutSLD != value)
			{
				_avoidSLMWithoutSLD = value;
				OnPropertyChanged();
			}
		}
	}

	public bool UseScaleRange
	{
		get => _useScaleRange;
		set
		{
			if (_useScaleRange != value)
			{
				_useScaleRange = value;
				OnPropertyChanged();
			}
		}
	}

	public double MinScaleDenominator
	{
		get => _minScaleDenominator;
		set
		{
			if (Math.Abs(_minScaleDenominator - value) > double.Epsilon)
			{
				_minScaleDenominator = value;
				OnPropertyChanged();
			}
		}
	}

	public double MaxScaleDenominator
	{
		get => _maxScaleDenominator;
		set
		{
			if (Math.Abs(_maxScaleDenominator - value) > double.Epsilon)
			{
				_maxScaleDenominator = value;
				OnPropertyChanged();
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}

[ValueConversion(typeof(double), typeof(string))]
public class ScaleConverter : IValueConverter // TODO move to Commons.UI/WPF?
{
	// Exceptions in Convert and ConvertBack crash the app!
	// Use validation so that ConvertBack is only called for valid input:
	// <TextBox>
	//   <TextBox.Text>
	//     <Binding Path="ScaleDenomDoubleProperty"
	//              UpdateSourceTrigger="Default"> <!--or PropertyChanged-->
	//       <Binding.ValidationRules>
	//         <namespace:ScaleValidation/>
	//       </Binding.ValidationRules>
	//       <Binding.Converter>
	//         <namespace:ScaleConverter/>
	//       </Binding.Converter>
	//     </Binding>
	//   </TextBox.Text>
	// </TextBox>
	// https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/?view=netdesktop-9.0#associating-validation-rules-with-a-binding
	// https://stackoverflow.com/questions/6123880/how-to-handle-exception-in-value-converter-so-that-custom-error-message-can-be-d

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is null) return string.Empty;
		var denom = System.Convert.ToDouble(value);
		if (denom <= 0 || ! double.IsFinite(denom)) return "None";
		return string.Create(culture, $"1:{value}");
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return Parse(value as string, culture);
	}

	public static double Parse(string text, CultureInfo culture)
	{
		const double none = 0.0;

		text = text.Trim();
		if (string.IsNullOrEmpty(text)) return none;

		if (string.Equals(text, "None", StringComparison.OrdinalIgnoreCase))
		{
			return none;
		}

		int index = text.IndexOf(':');

		if (index < 0)
		{
			return double.Parse(text.Trim(), culture);
		}

		string numerText = text.Substring(0, index).Trim();

		string denomText = text.Substring(index + 1).Trim();

		double numer = double.Parse(numerText, culture);
		double denom = double.Parse(denomText, culture);

		denom /= numer;

		return double.IsNaN(denom) ? none : denom;
	}
}

public class ScaleValidation : ValidationRule // TODO move to Commons.UI/WPF?
{
	public override ValidationResult Validate(object value, CultureInfo cultureInfo)
	{
		try
		{
			ScaleConverter.Parse(value as string, cultureInfo);
			return new ValidationResult(true, null);
		}
		catch (Exception ex)
		{
			return new ValidationResult(false, ex.Message);
		}
	}
}
