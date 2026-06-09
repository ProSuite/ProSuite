using System;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class DateTimeValueBlazor : TestParameterValueBlazorBase<DateTime?>
{
	public DateTime? DateTimeValue
	{
		get => GetValue();
		set => SetValue(value);
	}
}
