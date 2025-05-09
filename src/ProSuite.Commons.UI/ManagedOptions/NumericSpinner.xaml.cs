using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ProSuite.Commons.UI.ManagedOptions
{
	/// <summary>
	/// Interaction logic for NumericSpinner.xaml
	/// </summary>
	public partial class NumericSpinner : UserControl
	{
		#region Events

		public event EventHandler PropertyChanged;
		public event EventHandler ValueChanged;

		#endregion

		public NumericSpinner()
		{
			InitializeComponent();

			grid.SetBinding(Grid.IsEnabledProperty, new Binding("IsEnabled")
			                                        {
				                                        ElementName = "root_numeric_spinner",
				                                        Mode = BindingMode.OneWay,
				                                        UpdateSourceTrigger = UpdateSourceTrigger
					                                        .PropertyChanged
			                                        });

			var textBinding = new Binding("TextValue")
			                  {
				                  ElementName = "root_numeric_spinner",
				                  Mode = BindingMode.TwoWay,
				                  UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
				                  ValidatesOnDataErrors = true,
				                  NotifyOnValidationError = true
			                  };
			textBinding.ValidationRules.Add(new DoubleValidationRule());
			textBox.SetBinding(TextBox.TextProperty, textBinding);

			textBox.SetBinding(FontStyleProperty, new Binding("TextFontStyle")
			                                      {
				                                      ElementName = "root_numeric_spinner",
				                                      Mode = BindingMode.OneWay,
				                                      UpdateSourceTrigger =
					                                      UpdateSourceTrigger.PropertyChanged
			                                      });

			textBox.LostFocus += TextBox_LostFocus;
			textBox.GotFocus += TextBox_GotFocus;

			DependencyPropertyDescriptor.FromProperty(ValueProperty, typeof(NumericSpinner))
			                            .AddValueChanged(
				                            this,
				                            (sender, e) =>
					                            OnValueChanged(
						                            this,
						                            new DependencyPropertyChangedEventArgs(
							                            ValueProperty, null, null)));
			DependencyPropertyDescriptor.FromProperty(TextValueProperty, typeof(NumericSpinner))
			                            .AddValueChanged(
				                            this,
				                            (sender, e) =>
					                            OnTextValueChanged(
						                            this,
						                            new DependencyPropertyChangedEventArgs(
							                            TextValueProperty, null, null)));
			DependencyPropertyDescriptor.FromProperty(DecimalsProperty, typeof(NumericSpinner))
			                            .AddValueChanged(
				                            this,
				                            (sender, e) => OnPropertyChanged(sender, EventArgs.Empty));
			DependencyPropertyDescriptor.FromProperty(MinValueProperty, typeof(NumericSpinner))
			                            .AddValueChanged(
				                            this,
				                            (sender, e) => OnPropertyChanged(sender, EventArgs.Empty));
			DependencyPropertyDescriptor.FromProperty(MaxValueProperty, typeof(NumericSpinner))
			                            .AddValueChanged(
				                            this,
				                            (sender, e) => OnPropertyChanged(sender, EventArgs.Empty));
			DependencyPropertyDescriptor.FromProperty(FontStyleProperty, typeof(NumericSpinner))
			                            .AddValueChanged(
				                            this,
				                            (sender, e) => OnPropertyChanged(sender, EventArgs.Empty));
			DependencyPropertyDescriptor.FromProperty(IsEnabledProperty, typeof(NumericSpinner))
			                            .AddValueChanged(
				                            this,
				                            (sender, e) => OnPropertyChanged(sender, EventArgs.Empty));

			DataContextChanged += NumericSpinner_DataContextChanged;

			TextValue = Value.ToString(CultureInfo.CurrentCulture);
		}

		private void NumericSpinner_DataContextChanged(object sender,
		                                               DependencyPropertyChangedEventArgs e)
		{
			UpdateTextValue();
		}

		private void UpdateTextValue()
		{
			string textValue = Value.ToString(CultureInfo.CurrentCulture);

			if (textValue == textBox.Text)
			{
				return;
			}

			BindingExpression binding = GetBindingExpression(ValueProperty);
			if (binding != null)
			{
				TextValue = textValue;
				BindingOperations.GetBindingExpression(textBox, TextBox.TextProperty)?.UpdateTarget();
			}
		}

		private void TextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			string newText = ((TextBox) sender).Text;
			string decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
			if (! double.TryParse(newText, NumberStyles.Any, CultureInfo.CurrentCulture,
			                      out double parsed) &&
			    ! newText.EndsWith(decimalSeparator) && newText != "-")
			{
				UpdateTextValue();
			}
			else
			{
				double roundedValue = Math.Round(parsed, Decimals);

				if (! MathUtils.AreEqual(Value, roundedValue))
				{
					Value = roundedValue;
					UpdateTextValue();
				}
			}
		}

		private void TextBox_GotFocus(object sender, RoutedEventArgs e)
		{
			BindingExpression binding = textBox.GetBindingExpression(TextBox.TextProperty);
			binding?.UpdateSource();
		}

		#region ValueProperty

		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
			nameof(Value), typeof(double), typeof(NumericSpinner),
			new PropertyMetadata(0.01, OnValueChanged));

		public double Value
		{
			get => (double) GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		private static void OnValueChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is NumericSpinner spinner)
			{
				spinner.Validate();
				spinner.ValueChanged?.Invoke(spinner, EventArgs.Empty);
				spinner.PropertyChanged?.Invoke(spinner, EventArgs.Empty);

				spinner.UpdateTextValue();
			}
		}

		#endregion

		#region TextValueProperty

		public static readonly DependencyProperty TextValueProperty = DependencyProperty.Register(
			nameof(TextValue), typeof(string), typeof(NumericSpinner),
			new PropertyMetadata("0.01", OnTextValueChanged));

		public string TextValue
		{
			get => (string) GetValue(TextValueProperty);
			set => SetValue(TextValueProperty, value);
		}

		private static void OnTextValueChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is NumericSpinner spinner)
			{
				spinner.TryUpdateValueFromText();
				spinner.PropertyChanged?.Invoke(spinner, EventArgs.Empty);
			}
		}

		private void TryUpdateValueFromText()
		{
			if (double.TryParse(TextValue, NumberStyles.Any, CultureInfo.CurrentCulture,
			                    out double parsed))
			{
				double clampedValue = Math.Max(MinValue, Math.Min(MaxValue, parsed));

				if (! MathUtils.AreEqual(parsed, clampedValue) ||
				    ! MathUtils.AreEqual(Value, clampedValue))
				{
					Value = clampedValue;
				}
			}
		}

		#endregion

		#region TextFontStyleProperty

		public static readonly DependencyProperty TextFontStyleProperty = DependencyProperty.Register(
			nameof(TextFontStyle), typeof(FontStyle), typeof(NumericSpinner),
			new PropertyMetadata(FontStyles.Normal));

		public FontStyle TextFontStyle
		{
			get => (FontStyle) GetValue(TextFontStyleProperty);
			set => SetValue(TextFontStyleProperty, value);
		}

		#endregion

		#region IsEnabledProperty

		public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(
			nameof(IsEnabled), typeof(bool), typeof(NumericSpinner),
			new PropertyMetadata(true));

		public new bool IsEnabled
		{
			get => (bool) GetValue(IsEnabledProperty);
			set => SetValue(IsEnabledProperty, value);
		}

		#endregion

		#region StepProperty

		public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
			nameof(Step), typeof(double), typeof(NumericSpinner),
			new PropertyMetadata(0.01));

		public double Step
		{
			get => (double) GetValue(StepProperty);
			set => SetValue(StepProperty, value);
		}

		#endregion

		#region DecimalsProperty

		public static readonly DependencyProperty DecimalsProperty = DependencyProperty.Register(
			nameof(Decimals), typeof(int), typeof(NumericSpinner),
			new PropertyMetadata(2));

		public int Decimals
		{
			get => (int) GetValue(DecimalsProperty);
			set => SetValue(DecimalsProperty, value);
		}

		#endregion

		#region MinValueProperty

		public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
			nameof(MinValue), typeof(double), typeof(NumericSpinner),
			new PropertyMetadata(double.MinValue));

		public double MinValue
		{
			get => (double) GetValue(MinValueProperty);
			set => SetValue(MinValueProperty, value);
		}

		#endregion

		#region MaxValueProperty

		public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
			nameof(MaxValue), typeof(double), typeof(NumericSpinner),
			new PropertyMetadata(double.MaxValue));

		public double MaxValue
		{
			get => (double) GetValue(MaxValueProperty);
			set => SetValue(MaxValueProperty, value);
		}

		#endregion

		private void Validate()
		{
			double testValue = Value;

			if (double.IsNaN(testValue) || double.IsInfinity(testValue))
				testValue = 0;
			if (testValue < MinValue) testValue = MinValue;
			if (testValue > MaxValue) testValue = MaxValue;

			double roundedValue = Math.Round(testValue, Decimals);

			if (! MathUtils.AreEqual(Value, roundedValue))
			{
				Value = roundedValue;
			}

			PropertyChanged?.Invoke(this, EventArgs.Empty);
		}

		private void OnPropertyChanged(object sender, EventArgs e)
		{
			Validate();
			PropertyChanged?.Invoke(this, e);
		}

		private void cmdUp_Click(object sender, RoutedEventArgs e)
		{
			Value += Step;
		}

		private void cmdDown_Click(object sender, RoutedEventArgs e)
		{
			Value -= Step;
		}
	}
}
