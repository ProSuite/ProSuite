using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class EnumTestParameterValueBlazor : TestParameterValueBlazorBase<int>
{
	private Type _dataType;

	[Parameter]
	public Type DataType
	{
		get => _dataType;
		set
		{
			_dataType = value;
			
			Array values = Enum.GetValues(_dataType);

			IntValues = new List<int>(values.Length);
			
			for (var i = 0; i < values.Length; i++)
			{
				object enumValue = values.GetValue(i);

				if (enumValue != null)
				{
					IntValues.Add((int) enumValue);
				}
			}
		}
	}

	public IList<int> IntValues { get; set; }

	public int IntValue
	{
		get => GetValue();
		set => SetValue(value);
	}
}
