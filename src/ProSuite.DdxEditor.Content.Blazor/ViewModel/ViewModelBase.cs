using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;
using ProSuite.DomainModel.AO.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

// todo daro rename
// todo daro property selected
public abstract class ViewModelBase : Observable
{
	[CanBeNull] private object _value;

	protected ViewModelBase([NotNull] TestParameter parameter,
	                        [CanBeNull] object value,
	                        [NotNull] IInstanceConfigurationViewModel observer) : base(observer)
	{
		Assert.ArgumentNotNull(parameter, nameof(parameter));

		ParameterName = parameter.Name;
		Parameter = parameter;
		DataType = parameter.Type;

		_value = value ?? TestParameterTypeUtils.GetDefault(DataType);
	}

	[CanBeNull]
	public object Value
	{
		get => _value;
		set => SetProperty(ref _value, value);
	}

	[NotNull]
	public string ParameterName { get; }

	public bool Editing { get; private set; }

	public Type ComponentType { get; set; }

	public IDictionary<string, object> ComponentParameters { get; } =
		new Dictionary<string, object>();

	[NotNull]
	public TestParameter Parameter { get; }

	[NotNull]
	protected Type DataType { get; }

	[CanBeNull]
	public DynamicComponent DynamicRowFilterComponent { get; set; }

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
