using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class EnumTestParameterValueBlazor : IDisposable
{
	private Type _dataType;
	private Dictionary<object, EnumValueItem> _enumValuesByObject;

	[Parameter]
	public ScalarTestParameterValueViewModel ViewModel { get; set; }

	[Parameter]
	public Type DataType
	{
		get => _dataType;
		set
		{
			_dataType = value;
			
			Array values = Enum.GetValues(_dataType);

			IntValues = new List<int>(values.Length);

			_enumValuesByObject = new Dictionary<object, EnumValueItem>(values.Length);
			
			for (var i = 0; i < values.Length; i++)
			{
				object enumValue = values.GetValue(i);

				if (enumValue != null)
				{
					IntValues.Add((int) enumValue);
					_enumValuesByObject.Add(enumValue, new EnumValueItem(enumValue));
				}
			}
			
			EnumValues = _enumValuesByObject.Values;
		}
	}

	public IList<int> IntValues { get; set; }
	public IEnumerable<EnumValueItem> EnumValues { get; set; }

	public int IntValue { get; set; }

	[CanBeNull]
	public object Value
	{
		get
		{
			return ViewModel.Value;
		}
		set
		{
			ViewModel.Value = Enum.ToObject(DataType, IntValue);
		}
	}

	public void Dispose()
	{
		ViewModel?.Dispose();
	}

	public class EnumValueItem
	{
		private readonly int _enumValue;
		private readonly string _enumName;

		public EnumValueItem([NotNull] object enumValue)
		{
			_enumValue = (int) enumValue;
			_enumName = $"{enumValue}";
		}

		[UsedImplicitly]
		public int EnumValue => _enumValue;

		[UsedImplicitly]
		public string EnumName => _enumName;
	}
}
