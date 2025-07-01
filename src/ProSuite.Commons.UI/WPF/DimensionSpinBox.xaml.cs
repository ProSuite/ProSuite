using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Binding = System.Windows.Data.Binding;
using TextBox = System.Windows.Controls.TextBox;

namespace ProSuite.Commons.UI.WPF
{
	/// <summary>
	/// Interaction logic for DimensionSpinBox.xaml
	/// </summary>
	public partial class DimensionSpinBox
	{
		#region Events

		public event EventHandler PropertyChanged;
		public event EventHandler ValueChanged;

		#endregion

		public DimensionSpinBox()
		{
			InitializeComponent();

			grid.SetBinding(Grid.IsEnabledProperty, new Binding("IsEnabled")
			                                        {
				                                        ElementName = "root_dimension_spin_box",
				                                        Mode = BindingMode.OneWay,
				                                        UpdateSourceTrigger = UpdateSourceTrigger
					                                        .PropertyChanged
			                                        });

			DependencyPropertyDescriptor.FromProperty(ValueProperty, typeof(DimensionSpinBox))
			                            .AddValueChanged(
				                            this,
				                            (sender, e) =>
					                            OnValueChanged(
						                            this,
						                            new DependencyPropertyChangedEventArgs(
							                            ValueProperty, null, null)));
			DependencyPropertyDescriptor.FromProperty(MinValueProperty, typeof(DimensionSpinBox))
			                            .AddValueChanged(
				                            this,
				                            (sender, e) =>
					                            OnPropertyChanged(sender, EventArgs.Empty));
			DependencyPropertyDescriptor.FromProperty(MaxValueProperty, typeof(DimensionSpinBox))
			                            .AddValueChanged(
				                            this,
				                            (sender, e) =>
					                            OnPropertyChanged(sender, EventArgs.Empty));
			DependencyPropertyDescriptor.FromProperty(FontStyleProperty, typeof(DimensionSpinBox))
			                            .AddValueChanged(
				                            this,
				                            (sender, e) =>
					                            OnPropertyChanged(sender, EventArgs.Empty));
			DependencyPropertyDescriptor.FromProperty(IsEnabledProperty, typeof(DimensionSpinBox))
			                            .AddValueChanged(
				                            this,
				                            (sender, e) =>
					                            OnPropertyChanged(sender, EventArgs.Empty));

			DataContextChanged += DimensionSpinBox_DataContextChanged;

			Loaded += (s, e) =>
			{
				// Ensure that the values are set from the property at least once. This is needed
				// because these values are never accessed directly, but their value is used
				// implicitly in the validator and converter classes.
				SetValidUnits(ValidUnits);
				SetFormatSpecifier(FormatSpecifier);
				SetUnitRequired(UnitRequired);
			};
		}

		/// <remarks>Call from margin text boxes to make sure the binding
		/// source is up to date before up/down key triggers an increment
		/// (in case the user typed another unit or value)</remarks>
		private void PreviewKeyDownHandler(object sender, KeyEventArgs args)
		{
			if (sender is TextBox && (args.Key == Key.Enter || args.Key == Key.Up || args.Key == Key.Down))
			{
				var be = textBox.GetBindingExpression(TextBox.TextProperty);
				be?.UpdateSource();
			}

			args.Handled = false; // pass the event on!
		}

		private void DimensionSpinBox_DataContextChanged(object sender,
		                                                 DependencyPropertyChangedEventArgs e)
		{
			BindingOperations.GetBindingExpression(textBox, TextBox.TextProperty)?.UpdateTarget();
		}

		#region ValueProperty

		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
			nameof(Value), typeof(Dimension), typeof(DimensionSpinBox),
			new PropertyMetadata(new Dimension(0.01, ""), OnValueChanged));

		public Dimension Value
		{
			get => ValidateValue((Dimension) GetValue(ValueProperty));
			set => SetValue(ValueProperty, ValidateValue(value));
		}

		private static void OnValueChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is DimensionSpinBox spinner)
			{
				spinner.ValueChanged?.Invoke(spinner, EventArgs.Empty);
				spinner.PropertyChanged?.Invoke(spinner, EventArgs.Empty);
			}
		}

		#endregion

		#region TextFontStyleProperty

		public static readonly DependencyProperty TextFontStyleProperty =
			DependencyProperty.Register(
				nameof(TextFontStyle), typeof(FontStyle), typeof(DimensionSpinBox),
				new PropertyMetadata(FontStyles.Normal));

		public FontStyle TextFontStyle
		{
			get => (FontStyle) GetValue(TextFontStyleProperty);
			set => SetValue(TextFontStyleProperty, value);
		}

		#endregion

		#region IsEnabledProperty

		public new static readonly DependencyProperty IsEnabledProperty =
			DependencyProperty.Register(
				nameof(IsEnabled), typeof(bool), typeof(DimensionSpinBox),
				new PropertyMetadata(true));

		public new bool IsEnabled
		{
			get => (bool) GetValue(IsEnabledProperty);
			set => SetValue(IsEnabledProperty, value);
		}

		#endregion

		#region StepSizesProperty

		public static readonly DependencyProperty StepSizesProperty = DependencyProperty.Register(
			nameof(StepSizes), typeof(Dictionary<string, double>), typeof(DimensionSpinBox),
			new PropertyMetadata(null));

		public Dictionary<string, double> StepSizes
		{
			get => (Dictionary<string, double>) GetValue(StepSizesProperty);
			set => SetValue(StepSizesProperty, value);
		}

		#endregion

		#region MinValueProperty

		public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
			nameof(MinValue), typeof(double), typeof(DimensionSpinBox),
			new PropertyMetadata(double.MinValue));

		public double MinValue
		{
			get => (double) GetValue(MinValueProperty);
			set => SetValue(MinValueProperty, value);
		}

		#endregion

		#region MaxValueProperty

		public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
			nameof(MaxValue), typeof(double), typeof(DimensionSpinBox),
			new PropertyMetadata(double.MaxValue));

		public double MaxValue
		{
			get => (double) GetValue(MaxValueProperty);
			set => SetValue(MaxValueProperty, value);
		}

		#endregion

		#region FormatSpecifierProperty

		public static readonly DependencyProperty FormatSpecifierProperty =
			DependencyProperty.Register(
				nameof(FormatSpecifier), typeof(string), typeof(DimensionSpinBox),
				new PropertyMetadata("F3"));

		/// <summary>
		/// Controls formatting of the value part of the dimension.
		/// Use any of .NET's standard numeric format strings, as described in
		/// <see href="https://learn.microsoft.com/dotnet/standard/base-types/standard-numeric-format-strings"/>
		/// </summary>
		public string FormatSpecifier
		{
			get => (string) GetValue(FormatSpecifierProperty);

			set
			{
				SetValue(FormatSpecifierProperty, value);
				SetFormatSpecifier(value);
			}
		}

		private void SetFormatSpecifier(string formatSpecifier)
		{
			if (BindingOperations
			    .GetBindingExpression(textBox, TextBox.TextProperty)
			    ?.ParentBinding.Converter is DimensionConverter converter)
			{
				converter.FormatSpecifier = formatSpecifier;
			}
		}

		#endregion

		#region ValidUnitsProperty

		public static readonly DependencyProperty ValidUnitsProperty =
			DependencyProperty.Register(
				nameof(ValidUnits), typeof(string), typeof(DimensionSpinBox),
				new PropertyMetadata(""));

		public string ValidUnits
		{
			get => (string) GetValue(ValidUnitsProperty);

			set
			{
				SetValue(ValidUnitsProperty, value);
				SetValidUnits(value);
			}
		}

		private void SetValidUnits(string validUnits)
		{
			var validationRules = BindingOperations
			                      .GetBindingExpression(textBox, TextBox.TextProperty)
			                      ?.ParentBinding.ValidationRules;
			if (validationRules != null)
			{
				foreach (var validator in validationRules)
				{
					if (validator is DimensionValidation dimValidator)
					{
						dimValidator.ValidUnitsText = validUnits;
					}
				}
			}
		}

		#endregion

		#region UnitRequiredProperty

		public static readonly DependencyProperty UnitRequiredProperty =
			DependencyProperty.Register(
				nameof(UnitRequired), typeof(bool), typeof(DimensionSpinBox),
				new PropertyMetadata(false));

		public bool UnitRequired
		{
			get => (bool) GetValue(UnitRequiredProperty);

			set
			{
				SetValue(UnitRequiredProperty, value);
				SetUnitRequired(value);
			}
		}

		private void SetUnitRequired(bool unitRequired)
		{
			var validationRules = BindingOperations
			                      .GetBindingExpression(textBox, TextBox.TextProperty)
			                      ?.ParentBinding.ValidationRules;
			if (validationRules != null)
			{
				foreach (var validator in validationRules)
				{
					if (validator is DimensionValidation dimValidator)
					{
						dimValidator.UnitRequired = unitRequired;
					}
				}
			}
		}

		#endregion

		#region DefaultUnitProperty

		public static readonly DependencyProperty DefaultUnitProperty =
			DependencyProperty.Register(
				nameof(DefaultUnit), typeof(string), typeof(DimensionSpinBox),
				new PropertyMetadata(null));

		public string DefaultUnit
		{
			get => (string) GetValue(DefaultUnitProperty);
			set => SetValue(DefaultUnitProperty, value);
		}

		#endregion

		private void OnPropertyChanged(object sender, EventArgs e)
		{
			PropertyChanged?.Invoke(this, e);
		}

		private void IncrementValue(object sender, RoutedEventArgs e)
		{
			Value = Increment(Value);
		}

		private void DecrementValue(object sender, RoutedEventArgs e)
		{
			Value = Decrement(Value);
		}

		private Dimension ValidateValue(Dimension dimension)
		{
			double clampedValue = Math.Max(MinValue, Math.Min(MaxValue, dimension.Value));

			return dimension.Unit is null
				       ? new Dimension(clampedValue, DefaultUnit)
				       : new Dimension(clampedValue, dimension.Unit);
		}

		public ICommand IncrementValueCommand =>
			new RelayCommand<DimensionSpinBox>(ds => IncrementValue(null, null));

		public ICommand DecrementValueCommand =>
			new RelayCommand<DimensionSpinBox>(ds => DecrementValue(null, null));

		private Dimension Increment(Dimension dim)
		{
			double delta = GetStepSize(dim, DefaultUnit);
			double newValue = dim.Value + delta;
			newValue = delta * Math.Round(newValue / delta);
			return new Dimension(newValue, dim.Unit);
		}

		private Dimension Decrement(Dimension dim)
		{
			double delta = GetStepSize(dim, DefaultUnit);
			double newValue = dim.Value - delta;
			newValue = delta * Math.Round(newValue / delta);
			return new Dimension(newValue, dim.Unit);
		}

		private double GetStepSize(Dimension dim, string defaultUnit = null)
		{
			var unit = dim.Unit ?? defaultUnit;
			if (unit != null && StepSizes.ContainsKey(unit))
			{
				return StepSizes[unit];
			}

			return 1.0;
		}
	}
}
