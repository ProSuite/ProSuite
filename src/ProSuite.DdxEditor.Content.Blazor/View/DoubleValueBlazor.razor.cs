using System;
using Microsoft.AspNetCore.Components;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;

namespace ProSuite.DdxEditor.Content.Blazor.View;

// todo daro rename DoubletTestParameterValueBlazor etc.
public partial class DoubleValueBlazor : IDisposable
{
	[Parameter]
	public ScalarTestParameterValueViewModel ViewModel { get; set; }

	[CanBeNull]
	public object Value
	{
		get => ViewModel.Value;
		set => ViewModel.Value = value;
	}

	public double DoubleValue
	{
		get
		{
			if (Value != null)
			{
				return (double) Value;
			}

			return 0;
		}
		set => Value = value;
	}

	public void Dispose()
	{
		ViewModel?.Dispose();
	}
}
