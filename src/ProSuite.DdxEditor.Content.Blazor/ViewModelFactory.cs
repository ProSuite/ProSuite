using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.Blazor;

internal static class ViewModelFactory
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[NotNull]
	public static ViewModelBase CreateEmptyTestParameterViewModel(
		[NotNull] TestParameter parameter,
		[NotNull] IInstanceConfigurationViewModel instanceConfigurationViewModel)
	{
		Assert.ArgumentNotNull(parameter, nameof(parameter));
		Assert.ArgumentNotNull(instanceConfigurationViewModel,
		                       nameof(instanceConfigurationViewModel));

		TestParameterValue emptyParameterValue =
			TestParameterTypeUtils.GetEmptyParameterValue(parameter);

		return CreateTestParameterViewModel(parameter, emptyParameterValue,
		                                    instanceConfigurationViewModel);
	}

	[NotNull]
	public static ViewModelBase CreateTestParameterViewModel(
		[NotNull] TestParameter parameter,
		[NotNull] TestParameterValue value,
		[NotNull] IInstanceConfigurationViewModel instanceConfigurationViewModel)
	{
		Assert.ArgumentNotNull(parameter, nameof(parameter));
		Assert.ArgumentNotNull(value, nameof(value));
		Assert.ArgumentNotNull(instanceConfigurationViewModel,
		                       nameof(instanceConfigurationViewModel));

		ViewModelBase vm;

		if (value is DatasetTestParameterValue datasetValue)
		{
			vm = DatasetTestParameterValueViewModel.CreateInstance(
				parameter, datasetValue, instanceConfigurationViewModel);
		}
		else if (value is ScalarTestParameterValue scalarValue)
		{
			vm = new ScalarTestParameterValueViewModel(parameter, scalarValue.GetValue(),
			                                           instanceConfigurationViewModel,
			                                           parameter.IsConstructorParameter);
		}
		else
		{
			throw new ArgumentOutOfRangeException(nameof(value),
			                                      $@"Unkown {nameof(TestParameterValue)} type");
		}

		_msg.VerboseDebug(() => $"OnRowPropertyChanged register: {vm}");

		vm.PropertyChanged += instanceConfigurationViewModel.OnRowPropertyChanged;

		return vm;
	}

	[NotNull]
	public static ViewModelBase CreateCollectionViewModel(
		[NotNull] TestParameter parameter,
		[NotNull] IList<ViewModelBase> rows,
		[NotNull] IInstanceConfigurationViewModel instanceConfigurationViewModel)
	{
		Assert.ArgumentNotNull(parameter, nameof(parameter));
		Assert.ArgumentNotNull(rows, nameof(rows));
		Assert.ArgumentNotNull(instanceConfigurationViewModel,
		                       nameof(instanceConfigurationViewModel));

		var vm = new TestParameterValueCollectionViewModel(parameter, rows,
		                                                   instanceConfigurationViewModel,
		                                                   parameter.IsConstructorParameter);

		_msg.VerboseDebug(() => $"OnRowPropertyChanged register: {vm}");

		vm.PropertyChanged += instanceConfigurationViewModel.OnRowPropertyChanged;

		return vm;
	}
}
