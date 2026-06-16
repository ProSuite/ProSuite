using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ProSuite.Commons;

namespace ProSuite.Commons.UI.ManagedOptions
{
	/// <summary>
	/// Interaction logic for CentralizableSettingSlider.xaml. An override-aware label + slider +
	/// value textbox, mirroring the dependency-property surface of <see cref="NumericSpinner"/>.
	/// The value is hard-clamped to [<see cref="MinValue"/>, <see cref="MaxValue"/>] and rounded
	/// to <see cref="Decimals"/> places. Bind <see cref="Value"/> to a centralizable setting's
	/// <c>CurrentValue</c> (via <see cref="NumericToDoubleConverter"/> for int-valued settings).
	/// </summary>
	public partial class CentralizableSettingSlider : UserControl
	{
		public CentralizableSettingSlider()
		{
			InitializeComponent();
			Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			UpdateTextBoxBinding();
		}

		#region LabelProperty

		public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
			nameof(Label), typeof(string), typeof(CentralizableSettingSlider),
			new PropertyMetadata(string.Empty));

		public string Label
		{
			get => (string) GetValue(LabelProperty);
			set => SetValue(LabelProperty, value);
		}

		#endregion

		#region ValueProperty

		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
			nameof(Value), typeof(double), typeof(CentralizableSettingSlider),
			new FrameworkPropertyMetadata(
				0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

		public double Value
		{
			get => (double) GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is CentralizableSettingSlider slider)
			{
				slider.ClampValue();
			}
		}

		#endregion

		#region MinValueProperty

		public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
			nameof(MinValue), typeof(double), typeof(CentralizableSettingSlider),
			new PropertyMetadata(0.0, OnRangeChanged));

		public double MinValue
		{
			get => (double) GetValue(MinValueProperty);
			set => SetValue(MinValueProperty, value);
		}

		#endregion

		#region MaxValueProperty

		public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
			nameof(MaxValue), typeof(double), typeof(CentralizableSettingSlider),
			new PropertyMetadata(100.0, OnRangeChanged));

		public double MaxValue
		{
			get => (double) GetValue(MaxValueProperty);
			set => SetValue(MaxValueProperty, value);
		}

		private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is CentralizableSettingSlider slider)
			{
				slider.ClampValue();
			}
		}

		#endregion

		#region StepProperty

		public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
			nameof(Step), typeof(double), typeof(CentralizableSettingSlider),
			new PropertyMetadata(1.0));

		public double Step
		{
			get => (double) GetValue(StepProperty);
			set => SetValue(StepProperty, value);
		}

		#endregion

		#region DecimalsProperty

		public static readonly DependencyProperty DecimalsProperty = DependencyProperty.Register(
			nameof(Decimals), typeof(int), typeof(CentralizableSettingSlider),
			new PropertyMetadata(2, OnDecimalsChanged));

		public int Decimals
		{
			get => (int) GetValue(DecimalsProperty);
			set => SetValue(DecimalsProperty, value);
		}

		private static void OnDecimalsChanged(DependencyObject d,
		                                      DependencyPropertyChangedEventArgs e)
		{
			if (d is CentralizableSettingSlider slider)
			{
				slider.UpdateTextBoxBinding();
				slider.ClampValue();
			}
		}

		#endregion

		#region IsSnapToTickEnabledProperty

		public static readonly DependencyProperty IsSnapToTickEnabledProperty =
			DependencyProperty.Register(
				nameof(IsSnapToTickEnabled), typeof(bool), typeof(CentralizableSettingSlider),
				new PropertyMetadata(true));

		public bool IsSnapToTickEnabled
		{
			get => (bool) GetValue(IsSnapToTickEnabledProperty);
			set => SetValue(IsSnapToTickEnabledProperty, value);
		}

		#endregion

		#region IsEnabledProperty

		public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(
			nameof(IsEnabled), typeof(bool), typeof(CentralizableSettingSlider),
			new PropertyMetadata(true));

		public new bool IsEnabled
		{
			get => (bool) GetValue(IsEnabledProperty);
			set => SetValue(IsEnabledProperty, value);
		}

		#endregion

		#region TextFontStyleProperty

		public static readonly DependencyProperty TextFontStyleProperty =
			DependencyProperty.Register(
				nameof(TextFontStyle), typeof(FontStyle), typeof(CentralizableSettingSlider),
				new PropertyMetadata(FontStyles.Normal));

		public FontStyle TextFontStyle
		{
			get => (FontStyle) GetValue(TextFontStyleProperty);
			set => SetValue(TextFontStyleProperty, value);
		}

		#endregion

		private void ClampValue()
		{
			double clamped = Value;

			if (double.IsNaN(clamped) || double.IsInfinity(clamped))
			{
				clamped = MinValue;
			}

			if (clamped < MinValue)
			{
				clamped = MinValue;
			}

			if (clamped > MaxValue)
			{
				clamped = MaxValue;
			}

			double rounded = Math.Round(clamped, Math.Max(0, Decimals));

			if (! MathUtils.AreEqual(Value, rounded))
			{
				Value = rounded;
			}
		}

		private void UpdateTextBoxBinding()
		{
			if (ValueTextBox == null)
			{
				return;
			}

			var binding = new Binding(nameof(Value))
			              {
				              Source = this,
				              Mode = BindingMode.TwoWay,
				              UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
				              StringFormat = "F" + Math.Max(0, Decimals)
			              };

			ValueTextBox.SetBinding(TextBox.TextProperty, binding);
		}
	}
}
