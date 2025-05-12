using System;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.Commons.UI.ScreenBinding.Elements;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Models
{
	public partial class ModelControl<T> : UserControl, IModelView<T>, IEntityPanel<T>
		where T : DdxModel
	{
		private const string _title = "Model Properties";

		private static string _lastSelectedTabPage;

		public ModelControl()
		{
			InitializeComponent();

			_labelElementNameQualificationStatus.Text = null;
		}

		#region IEntityPanel<T> Members

		public string Title => _title;

		public void OnBindingTo(T entity) { }

		public void SetBinder(ScreenBinder<T> binder)
		{
			binder.Bind(m => m.Name)
			      .To(_textBoxName)
			      .WithLabel(_labelName);
			binder.Bind(m => m.Description)
			      .To(_textBoxDescription)
			      .WithLabel(_labelDescription);

			binder.Bind(m => m.UseDefaultDatabaseOnlyForSchema)
			      .To(_booleanComboboxUseDefaultDatabaseForSchemaOnly);
			binder.Bind(m => m.AllowUserChangingUseMasterDatabaseOnlyForSchema)
			      .ToEnabledOf(_booleanComboboxUseDefaultDatabaseForSchemaOnly);

			binder.Bind(m => m.HarvestQualifiedElementNames)
			      .To(_checkBoxHarvestQualifiedElementNames);
			binder.Bind(m => m.AllowUserChangingHarvestQualifiedElementNames)
			      .ToEnabledOf(_checkBoxHarvestQualifiedElementNames);

			binder.Bind(m => m.LastHarvestedDate)
			      .To(_textBoxLastHarvestedDate)
			      .WithLabel(_labelLastHarvestedDate)
			      .AsReadOnly();

			binder.Bind(m => m.LastHarvestedByUser)
			      .To(_textBoxLastHarvestedByUser)
			      .WithLabel(_labelLastHarvestedByUser)
			      .AsReadOnly();

			binder.Bind(m => m.LastHarvestedConnectionString)
			      .To(_textBoxLastHarvestedConnectionString)
			      .WithLabel(_labelLastHarvestedConnectionString)
			      .AsReadOnly();

			binder.Bind(m => m.DefaultDatabaseName)
			      .To(_textBoxDefaultDatabaseName)
			      .WithLabel(_labelDefaultDatabaseName)
			      .AsReadOnly();

			binder.Bind(m => m.DefaultDatabaseSchemaOwner)
			      .To(_textBoxDefaultDatabaseSchemaOwner)
			      .WithLabel(_labelDefaultDatabaseSchemaOwner)
			      .AsReadOnly();

			binder.Bind(m => m.IgnoreUnversionedDatasets)
			      .To(_checkBoxIgnoreUnversionedDatasets)
			      .RebindOnChange();

			binder.Bind(m => m.IgnoreUnregisteredTables)
			      .To(_checkboxIgnoreUnregisteredTables);

			binder.Bind(m => m.CanChangeIgnoreUnregisteredTables)
			      .ToEnabledOf(_checkboxIgnoreUnregisteredTables);

			binder.Bind(m => m.UpdateAliasNamesOnHarvest)
			      .To(_checkBoxUpdateAliasNamesOnHarvest);

			binder.Bind(m => m.DatasetExclusionCriteria)
			      .To(_textBoxDatasetExclusionCriteria)
			      .WithLabel(_labelDatasetExclusionCriteria);

			binder.Bind(m => m.DatasetInclusionCriteria)
			      .To(_textBoxDatasetInclusionCriteria)
			      .WithLabel(_labelDatasetInclusionCriteria);

			binder.AddElement(new ObjectReferenceScreenElement(
				                  binder.GetAccessor(m => m.SpatialReferenceDescriptor),
				                  _objectReferenceControlSpatialReferenceDescriptor));
			binder.AddElement(new ObjectReferenceScreenElement(
				                  binder.GetAccessor(m => m.UserConnectionProvider),
				                  _objectReferenceControlUserConnectionProvider));

			binder.AddElement(new ObjectReferenceScreenElement(
				                  binder.GetAccessor(m => m.RepositoryOwnerConnectionProvider),
				                  _objectReferenceControlRepositoryOwnerConnectionProvider));

			binder.AddElement(new ObjectReferenceScreenElement(
				                  binder.GetAccessor(m => m.SchemaOwnerConnectionProvider),
				                  _objectReferenceControlSchemaOwnerConnectionProvider));

			binder.Bind(m => m.SchemaOwner)
			      .To(_textBoxSchemaOwner)
			      .WithLabel(_labelSchemaOwner);
			binder.Bind(m => m.DatasetPrefix)
			      .To(_textBoxDatasetPrefix)
			      .WithLabel(_labelDatasetPrefix);
			binder.Bind(m => m.DefaultMinimumSegmentLength)
			      .To(_numericUpDownDefaultMinimumSegmentLength)
			      .WithLabel(_labelDefaultMinimumSegmentLength);

			binder.Bind(m => m.SqlCaseSensitivity)
			      .To(_comboBoxSqlCaseSensitivity)
			      .FillWith(GetSqlCaseSensitivityPickList())
			      .WithLabel(_labelSqlCaseSensitivity);

			binder.AddElement(new ObjectReferenceScreenElement(
				                  binder.GetAccessor(
					                  m => m.DatasetListBuilderFactoryClassDescriptor),
				                  _objectReferenceControlDatasetListBuilderFactoryClassDescriptor));

			binder.AddElement(new ObjectReferenceScreenElement(
				                  binder.GetAccessor(
					                  m => m.AttributeConfiguratorFactoryClassDescriptor),
				                  _objectReferenceControlAttributeConfiguratorFactoryClassDescriptor));

			TabControlUtils.SelectTabPage(_tabControl, _lastSelectedTabPage);

			_objectReferenceControlSpatialReferenceDescriptor.Changed -=
				_objectReferenceControlSpatialReferenceDescriptor_Changed;
			_objectReferenceControlSpatialReferenceDescriptor.Changed +=
				_objectReferenceControlSpatialReferenceDescriptor_Changed;

			_objectReferenceControlUserConnectionProvider.Changed -=
				_objectReferenceControlUserConnectionProvider_Changed;
			_objectReferenceControlUserConnectionProvider.Changed +=
				_objectReferenceControlUserConnectionProvider_Changed;
		}

		public void OnBoundTo(T entity)
		{
			bool warn;
			_labelElementNameQualificationStatus.Text =
				GetElementNameQualificationStatusText(entity, out warn);

			_labelElementNameQualificationStatus.ForeColor = warn
				                                                 ? Color.Red
				                                                 : DefaultForeColor;
		}

		#endregion

		#region IModelView Members

		public bool GoToSpatialReferenceEnabled
		{
			get => _buttonGoToSpatialReference.Enabled;
			set => _buttonGoToSpatialReference.Enabled = value;
		}

		public bool GoToUserConnectionEnabled
		{
			get => _buttonGoToUserConnectionProvider.Enabled;
			set => _buttonGoToUserConnectionProvider.Enabled = value;
		}

		public Func<object> FindUserConnectionProviderDelegate
		{
			get => _objectReferenceControlUserConnectionProvider.FindObjectDelegate;
			set => _objectReferenceControlUserConnectionProvider.FindObjectDelegate = value;
		}

		public Func<object> FindSpatialReferenceDescriptorDelegate
		{
			get => _objectReferenceControlSpatialReferenceDescriptor.FindObjectDelegate;
			set => _objectReferenceControlSpatialReferenceDescriptor.FindObjectDelegate = value;
		}

		public Func<object> FindSchemaOwnerConnectionProviderDelegate
		{
			get => _objectReferenceControlSchemaOwnerConnectionProvider.FindObjectDelegate;
			set => _objectReferenceControlSchemaOwnerConnectionProvider.FindObjectDelegate = value;
		}

		public Func<object> FindRepositoryOwnerConnectionProviderDelegate
		{
			get => _objectReferenceControlRepositoryOwnerConnectionProvider.FindObjectDelegate;
			set => _objectReferenceControlRepositoryOwnerConnectionProvider.FindObjectDelegate =
				       value;
		}

		public Func<object> FindAttributeConfiguratorFactoryDelegate
		{
			get => _objectReferenceControlAttributeConfiguratorFactoryClassDescriptor
				.FindObjectDelegate;
			set => _objectReferenceControlAttributeConfiguratorFactoryClassDescriptor
				       .FindObjectDelegate = value;
		}

		public Func<object> FindDatasetListBuilderFactoryDelegate
		{
			get => _objectReferenceControlDatasetListBuilderFactoryClassDescriptor
				.FindObjectDelegate;
			set => _objectReferenceControlDatasetListBuilderFactoryClassDescriptor
				       .FindObjectDelegate = value;
		}

		#endregion

		#region IBoundView<Model,IModelObserver> Members

		[CanBeNull]
		public IModelObserver Observer { get; set; }

		public void BindTo(T target) { }

		#endregion

		[NotNull]
		private static string GetElementNameQualificationStatusText(
			[NotNull] DdxModel model, out bool warn)
		{
			if (model.LastHarvestedDate != null)
			{
				warn = false;
				return model.ElementNamesAreQualified
					       ? "Harvested dataset/association names are qualified"
					       : "Harvested dataset/association names are not qualified";
			}

			if (model.Datasets.Count <= 0)
			{
				warn = false;
				return "The data model has not yet been harvested";
			}

			warn = true;
			return "The data model needs to be harvested due to a software upgrade";
		}

		private void _buttonHarvestingPreview_Click(object sender, EventArgs e)
		{
			Observer?.HarvestingPreviewClicked();
		}

		private void _buttonGoToSpatialReference_Click(object sender, EventArgs e)
		{
			Observer?.GoToSpatialReferenceClicked();
		}

		private void _buttonGoToUserConnectionProvider_Clicked(object sender, EventArgs e)
		{
			Observer?.GoToUserConnectionClicked();
		}

		private void _objectReferenceControlSpatialReferenceDescriptor_Changed(object sender, EventArgs e)
		{
			Observer?.SpatialReferenceDescriptorChanged();
		}

		private void _objectReferenceControlUserConnectionProvider_Changed(object sender, EventArgs e)
		{
			Observer?.UserConnectionProviderChanged();
		}

		private void _tabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			_lastSelectedTabPage = TabControlUtils.GetSelectedTabPageName(_tabControl);
		}

		[NotNull]
		private static Picklist<SqlCaseSensitivityItem> GetSqlCaseSensitivityPickList()
		{
			const bool noSort = true;
			return new Picklist<SqlCaseSensitivityItem>(
				       new[]
				       {
					       new SqlCaseSensitivityItem
					       {
						       Value = SqlCaseSensitivity.SameAsDatabase,
						       DisplayName = "Same as database"
					       },
					       new SqlCaseSensitivityItem
					       {
						       Value = SqlCaseSensitivity.CaseInsensitive,
						       DisplayName = "Not case-sensitive"
					       },
					       new SqlCaseSensitivityItem
					       {
						       Value = SqlCaseSensitivity.CaseSensitive,
						       DisplayName = "Case-sensitive"
					       }
				       }, noSort)
			       {
				       DisplayMember = "DisplayName",
				       ValueMember = "Value"
			       };
		}

		private class SqlCaseSensitivityItem
		{
			[UsedImplicitly]
			public SqlCaseSensitivity Value { get; set; }

			[UsedImplicitly]
			public string DisplayName { get; set; }
		}
	}
}
