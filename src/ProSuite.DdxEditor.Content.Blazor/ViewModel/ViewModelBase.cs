using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

// todo daro rename
public abstract class ViewModelBase : Observable
{
	protected ViewModelBase([NotNull] TestParameter parameter, [NotNull] IViewObserver observer) : base(observer)
	{
		Assert.ArgumentNotNull(parameter, nameof(parameter));

		ParameterName = parameter.Name;
		Parameter = parameter;
		DataType = parameter.Type;

		ComponentParameters = new Dictionary<string, object>();
	}

	[CanBeNull]
	public abstract object Value { get; set; }

	[CanBeNull]
	public virtual List<ViewModelBase> Values { get; set; }

	[NotNull]
	public string ParameterName { get; }

	public bool Editing { get; private set; }
	public bool Expanded { get; set; }

	public Type ComponentType { get; set; }

	public IDictionary<string, object> ComponentParameters { get; }

	[NotNull]
	public TestParameter Parameter { get; }

	[NotNull]
	public Type DataType { get; }

	public void StartEditing()
	{
		Editing = true;
	}

	public void StopEditing()
	{
		Editing = false;
	}

	public override string ToString()
	{
		return $"name: {ParameterName}, type: {DataType} ({GetType()})";
	}
}
