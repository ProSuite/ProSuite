using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

// todo daro rename
// todo daro property selected
public abstract class ViewModelBase : Observable
{
	[CanBeNull] private object _value;
	private IDataContainer _uncachedContainer;

	// todo daro refactor parameter order
	protected ViewModelBase([NotNull] TestParameter parameter,
	                        [CanBeNull] object value,
	                        [NotNull] IInstanceConfigurationViewModel observer,
	                        bool required = false,
	                        [CanBeNull] string customErrorMessage = null) : base(
		observer, customErrorMessage, required)
	{
		Assert.ArgumentNotNull(parameter, nameof(parameter));

		ParameterName = Required ? parameter.Name : $"[{parameter.Name}]";
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

	public Type ComponentType { get; protected init; }

	[NotNull]
	public IDictionary<string, object> ComponentParameters { get; } =
		new Dictionary<string, object>();

	[NotNull]
	public TestParameter Parameter { get; }

	[NotNull]
	public Type DataType { get; }

	public void ResetValue()
	{
		ResetValueCore();
	}

	protected override bool ValidateCore()
	{
		return Value != null;
	}

	public override string ToString()
	{
		string value;
		if (Value is ICollection<ViewModelBase> collection)
		{
			value = StringUtils.Concatenate(collection.Select(vm => vm.Value), ", ");
		}
		else if (Value == null)
		{
			value = "<null>";
		}
		else
		{
			value = $"{Value}";
		}

		return $"{GetType().Name}: {value} ({ParameterName}, {DataType.Name})";
	}

	protected virtual void ResetValueCore()
	{
		Value = null;
	}

	protected ITableSchemaDef GetTransformedTableSchemaDef(
		[NotNull] TransformerConfiguration transformerConfiguration)
	{
		var datasetParameter =
			transformerConfiguration.GetDatasetParameterValues(true, true)
			                        .FirstOrDefault(d => d is IObjectDataset);

		Assert.NotNull(datasetParameter, "No dataset parameter found");

		DdxModel dataModel = datasetParameter.Model;

		if (! dataModel.IsMasterDatabaseAccessible())
		{
			throw new InvalidOperationException(
				$"Master database is not accessible. Reason: {dataModel.GetMasterDatabaseNoAccessReason()}");
		}

		IWorkspaceContext masterDbContext =
			Assert.NotNull(dataModel.GetMasterDatabaseWorkspaceContext());

		IOpenDataset datasetOpener = new SimpleDatasetOpener(masterDbContext);

		IReadOnlyTable transformedTable = InstanceFactory.CreateTransformedTable(
			transformerConfiguration, datasetOpener);

		if (transformedTable is IReadOnlyFeatureClass fc && _uncachedContainer == null)
		{
			_uncachedContainer = new UncachedDataContainer(fc.Extent);
		}

		if (transformedTable is IDataContainerAware transformed)
		{
			transformed.DataContainer = _uncachedContainer;

			TestUtils.SetContainer(_uncachedContainer, transformed.InvolvedTables);
		}

		return (ITableSchemaDef) transformedTable;
	}
}
