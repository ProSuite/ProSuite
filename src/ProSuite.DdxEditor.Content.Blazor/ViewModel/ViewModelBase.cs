using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

// todo daro rename
public abstract class ViewModelBase : Observable
{
	[CanBeNull] private object _value;

	protected ViewModelBase([NotNull] TestParameter parameter,
	                        [CanBeNull] object value,
	                        [NotNull] IViewObserver observer) : base(observer)
	{
		Assert.ArgumentNotNull(parameter, nameof(parameter));
		
		ParameterName = parameter.Name;
		Parameter = parameter;
		DataType = parameter.Type;
		_value = value ?? TestParameterTypeUtils.GetDefault(DataType);

		ComponentParameters = new Dictionary<string, object>();
	}

	[CanBeNull]
	public object Value
	{
		get => _value;
		set => SetProperty(ref _value, value);
	}

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
