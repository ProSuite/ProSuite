using System;
using System.Globalization;
using System.Windows.Controls;

namespace ProSuite.Commons.UI.WPF
{
	/// <summary>
	/// Validation class for use with the <see cref="ScaleConverter"/>
	/// value converter for WPF. You may want to use the error message
	/// as the ToolTip using a Trigger as in the comment in the code.
	/// </summary>
	public class ScaleValidation : ValidationRule
	{
		// <TextBox>
		//   <TextBox.Style>
		//     <Style TargetType="{x:Type TextBox}">
		//       <Style.Triggers>
		//         <Trigger Property="Validation.HasError" Value="True">
		//           <Setter Property="ToolTip" Value="{Binding RelativeSource={RelativeSource Self}, Path=(Validation.Errors)[0].ErrorContent}"/>
		//         </Trigger>
		//       </Style.Triggers>
		//     </Style>
		//   </TextBox.Style>
		//   <TextBox.Text>...binding...</TextBox.Text>
		// </TextBox>

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
}
