using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.View;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

// todo daro rename
public abstract class ViewModelBase : Observable
{
	protected ViewModelBase([NotNull] string name, [NotNull] IViewModel observer) : base(observer)
	{
		Assert.ArgumentNotNullOrEmpty(name, nameof(name));

		ParameterName = name;

		ComponentType = typeof(StringValueBlazor);
		ComponentParameters = new Dictionary<string, object>();
	}

	protected ViewModelBase() : base(null)
	{
		ComponentParameters = new Dictionary<string, object>();

		Values = new List<ViewModelBase>();
	}

	[UsedImplicitly]
	public abstract object Value { get; set; }

	[UsedImplicitly]
	public string ParameterName { get; }

	public bool Editing { get; private set; }

	public string ImageSource { get; set; }

	public Type ComponentType { get; set; }

	public IDictionary<string, object> ComponentParameters { get; }

	public List<ViewModelBase> Values { get; set; }

	public void StartEditing()
	{
		Editing = true;
	}

	public void StopEditing()
	{
		Editing = false;
	}
}
