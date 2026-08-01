using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ProSuite.Commons.UI.ManagedOptions
{
	/// <summary>
	/// Interaction logic for CentralizableSettingSliderWithCheckbox.xaml. A checkbox-gated slider
	/// composite preserving the single-row layout (checkbox + slider + value textbox) under a
	/// label. The checkbox binds to <see cref="CheckboxSetting"/> and the slider/textbox to
	/// <see cref="SliderSetting"/> — each a centralizable setting view model (passed as
	/// <see cref="object"/> to avoid a dependency on the editing assembly). The slider's
	/// disable-when-unchecked behaviour comes from the <see cref="SliderSetting"/> wrapper, which
	/// the caller constructs with the checkbox setting as a controlling parent.
	/// </summary>
	public partial class CentralizableSettingSliderWithCheckbox : UserControl
	{
		public CentralizableSettingSliderWithCheckbox()
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
			nameof(Label), typeof(string), typeof(CentralizableSettingSliderWithCheckbox),
			new PropertyMetadata(string.Empty));

		public string Label
		{
			get => (string) GetValue(LabelProperty);
			set => SetValue(LabelProperty, value);
		}

		#endregion

		#region CheckboxSettingProperty

		public static readonly DependencyProperty CheckboxSettingProperty =
			DependencyProperty.Register(
				nameof(CheckboxSetting), typeof(object),
				typeof(CentralizableSettingSliderWithCheckbox), new PropertyMetadata(null));

		public object CheckboxSetting
		{
			get => GetValue(CheckboxSettingProperty);
			set => SetValue(CheckboxSettingProperty, value);
		}

		#endregion

		#region SliderSettingProperty

		public static readonly DependencyProperty SliderSettingProperty =
			DependencyProperty.Register(
				nameof(SliderSetting), typeof(object),
				typeof(CentralizableSettingSliderWithCheckbox),
				new PropertyMetadata(null, OnSliderSettingChanged));

		public object SliderSetting
		{
			get => GetValue(SliderSettingProperty);
			set => SetValue(SliderSettingProperty, value);
		}

		private static void OnSliderSettingChanged(DependencyObject d,
		                                           DependencyPropertyChangedEventArgs e)
		{
			if (! (d is CentralizableSettingSliderWithCheckbox control))
			{
				return;
			}

			// Track the wrapper's Decimals (duck-typed) so the textbox can format accordingly.
			if (e.NewValue != null)
			{
				BindingOperations.SetBinding(control, DecimalsProperty,
				                             new Binding("Decimals") { Source = e.NewValue });
			}
			else
			{
				BindingOperations.ClearBinding(control, DecimalsProperty);
			}

			control.UpdateTextBoxBinding();
		}

		#endregion

		#region IsSnapToTickEnabledProperty

		public static readonly DependencyProperty IsSnapToTickEnabledProperty =
			DependencyProperty.Register(
				nameof(IsSnapToTickEnabled), typeof(bool),
				typeof(CentralizableSettingSliderWithCheckbox), new PropertyMetadata(true));

		public bool IsSnapToTickEnabled
		{
			get => (bool) GetValue(IsSnapToTickEnabledProperty);
			set => SetValue(IsSnapToTickEnabledProperty, value);
		}

		#endregion

		#region DecimalsProperty

		private static readonly DependencyProperty DecimalsProperty = DependencyProperty.Register(
			"Decimals", typeof(int), typeof(CentralizableSettingSliderWithCheckbox),
			new PropertyMetadata(2, OnDecimalsChanged));

		private int Decimals
		{
			get => (int) GetValue(DecimalsProperty);
			set => SetValue(DecimalsProperty, value);
		}

		private static void OnDecimalsChanged(DependencyObject d,
		                                      DependencyPropertyChangedEventArgs e)
		{
			if (d is CentralizableSettingSliderWithCheckbox control)
			{
				control.UpdateTextBoxBinding();
			}
		}

		#endregion

		private void UpdateTextBoxBinding()
		{
			if (ValueTextBox == null)
			{
				return;
			}

			// The textbox mirrors the slider's (double) value, formatted to the wrapper's decimals.
			var binding = new Binding(nameof(Slider.Value))
			              {
				              Source = ValueSlider,
				              Mode = BindingMode.TwoWay,
				              UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
				              StringFormat = "F" + Math.Max(0, Decimals)
			              };

			ValueTextBox.SetBinding(TextBox.TextProperty, binding);
		}
	}
}
