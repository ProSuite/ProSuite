using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProSuite.Commons.UI.ManagedOptions;

/// <summary>
/// Interaction logic for NumericSpinner.xaml
/// </summary>
public partial class NumericSpinner : UserControl
{
	#region Fields

	public event EventHandler PropertyChanged;
	public event EventHandler ValueChanged;
	#endregion

	public NumericSpinner()
	{
		InitializeComponent();

		grid.SetBinding(Grid.IsEnabledProperty, new Binding("IsEnabled") {
			                                        ElementName = "root_numeric_spinner",
			                                        Mode = BindingMode.OneWay,
			                                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
		                                        });
		
		textBox.SetBinding(TextBox.TextProperty, new Binding("Value")
		                                         {
			                                         ElementName = "root_numeric_spinner",
			                                         Mode = BindingMode.TwoWay,
			                                         UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
		                                         });

		textBox.SetBinding(TextBox.FontStyleProperty, new Binding("TextFontStyle") {
			                                         ElementName = "root_numeric_spinner",
			                                         Mode = BindingMode.OneWay,
			                                         UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
		                                         });

		DependencyPropertyDescriptor.FromProperty(ValueProperty, typeof(NumericSpinner)).AddValueChanged(this, PropertyChanged);
		DependencyPropertyDescriptor.FromProperty(ValueProperty, typeof(NumericSpinner)).AddValueChanged(this, ValueChanged);
		DependencyPropertyDescriptor.FromProperty(DecimalsProperty, typeof(NumericSpinner)).AddValueChanged(this, PropertyChanged);
		DependencyPropertyDescriptor.FromProperty(MinValueProperty, typeof(NumericSpinner)).AddValueChanged(this, PropertyChanged);
		DependencyPropertyDescriptor.FromProperty(MaxValueProperty, typeof(NumericSpinner)).AddValueChanged(this, PropertyChanged);
	   DependencyPropertyDescriptor.FromProperty(FontStyleProperty, typeof(NumericSpinner)).AddValueChanged(this, PropertyChanged);
	   DependencyPropertyDescriptor.FromProperty(IsEnabledProperty, typeof(NumericSpinner)).AddValueChanged(this, PropertyChanged);

		PropertyChanged += (x, y) => validate();
	}
	

	#region ValueProperty

	public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
		"Value",
		typeof(decimal),
		typeof(NumericSpinner),
		new PropertyMetadata(new decimal(0.01)));

	public static readonly DependencyProperty TextFontStyleProperty = DependencyProperty.Register(
		"TextFontStyle", typeof(FontStyle), typeof(NumericSpinner),
		new PropertyMetadata(new FontStyle()));

	public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(
		"IsEnabled", typeof(bool), typeof(NumericSpinner),
		new PropertyMetadata(new bool()));

	public decimal Value
	{
		get { return (decimal)GetValue(ValueProperty); }
		set
		{
			if (value < MinValue)
				value = MinValue;
			if (value > MaxValue)
				value = MaxValue;
			SetValue(ValueProperty, value);
			ValueChanged?.Invoke(this, new EventArgs());
		}
	}

	public FontStyle TextFontStyle
	{
		get { return (FontStyle) GetValue(TextFontStyleProperty); }
		set
		{
			SetValue(TextFontStyleProperty, value);
		}
	}

	public new bool IsEnabled
	{
		get => (bool) GetValue(IsEnabledProperty);
		set => SetValue(IsEnabledProperty, value);
	}

	#endregion

	#region StepProperty

	public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
		"Step",
		typeof(decimal),
		typeof(NumericSpinner),
		new PropertyMetadata(new decimal(0.01))); 

	public decimal Step
	{
		get { return (decimal)GetValue(StepProperty); }
		set
		{
			SetValue(StepProperty, value);
		}
	}

	#endregion

	#region DecimalsProperty

	public static readonly DependencyProperty DecimalsProperty = DependencyProperty.Register(
		"Decimals",
		typeof(int),
		typeof(NumericSpinner),
		new PropertyMetadata(2));

	public int Decimals
	{
		get { return (int)GetValue(DecimalsProperty); }
		set
		{
			SetValue(DecimalsProperty, value);
		}
	}

	#endregion

	#region MinValueProperty

	public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
		"MinValue",
		typeof(decimal),
		typeof(NumericSpinner),
		new PropertyMetadata(decimal.MinValue));

	public decimal MinValue
	{
		get { return (decimal)GetValue(MinValueProperty); }
		set
		{
			if (value > MaxValue)
				MaxValue = value;
			SetValue(MinValueProperty, value);
		}
	}

	#endregion

	#region MaxValueProperty

	public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
		"MaxValue",
		typeof(decimal),
		typeof(NumericSpinner),
		new PropertyMetadata(decimal.MaxValue));

	public decimal MaxValue
	{
		get { return (decimal)GetValue(MaxValueProperty); }
		set
		{
			if (value < MinValue)
				value = MinValue;
			SetValue(MaxValueProperty, value);
		}
	}

	#endregion

	/// <summary>
	/// Revalidate the object, whenever a value is changed...
	/// </summary>
	private void validate()
	{
		// Logically, This is not needed at all... as it's handled within other properties...
		if (MinValue > MaxValue) MinValue = MaxValue;
		if (MaxValue < MinValue) MaxValue = MinValue;
		if (Value < MinValue) Value = MinValue;
		if (Value > MaxValue) Value = MaxValue;

		Value = decimal.Round(Value, Decimals);
	}

	private void cmdUp_Click(object sender, RoutedEventArgs e)
	{
		Value += Step;
	}

	private void cmdDown_Click(object sender, RoutedEventArgs e)
	{
		Value -= Step;
	}

	private void tb_main_Loaded(object sender, RoutedEventArgs e)
	{
		ValueChanged(this, new EventArgs());
	}
}
