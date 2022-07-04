using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;

namespace ProSuite.DdxEditor.Content.Blazor.View
{
	// todo daro make generic for double and integer?
	public partial class IntegerValueBlazor : IDisposable
	{
		[Parameter]
		public ScalarTestParameterValueViewModel ViewModel { get; set; }

		[CanBeNull]
		public object Value
		{
			get => ViewModel.Value;
			set => ViewModel.Value = value;
		}

		public int IntegerValue
		{
			get
			{
				if (Value != null)
				{
					return (int) Value;
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
}
