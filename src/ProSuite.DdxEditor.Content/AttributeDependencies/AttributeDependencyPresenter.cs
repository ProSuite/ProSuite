using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.AttributeDependencies;
using ProSuite.DomainModel.Core.DataModel;
using Attribute = ProSuite.DomainModel.Core.DataModel.Attribute;
using FieldType = ProSuite.DomainModel.Core.DataModel.FieldType;

namespace ProSuite.DdxEditor.Content.AttributeDependencies
{
	public class AttributeDependencyPresenter :
		SimpleEntityItemPresenter<AttributeDependencyItem>, IAttributeDependencyObserver
	{
		private readonly IAttributeDependencyView _view;
		private readonly string _descriptionFieldName;
		private readonly SortableBindingList<AttributeTableRow> _sourceRows;
		private readonly SortableBindingList<AttributeTableRow> _targetRows;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Construction

		public AttributeDependencyPresenter(
			[NotNull] AttributeDependencyItem item,
			[NotNull] IAttributeDependencyView view,
			[CanBeNull] string descriptionFieldName)
			: base(item)
		{
			Assert.ArgumentNotNull(view, nameof(view));

			_view = view;
			_descriptionFieldName = descriptionFieldName;
			_sourceRows = new SortableBindingList<AttributeTableRow>();
			_targetRows = new SortableBindingList<AttributeTableRow>();

			_view.Observer = this;
			_view.FindDatasetDelegate =
				() => item.FindDataset(view,
				                       new ColumnDescriptor("Image", string.Empty),
				                       new ColumnDescriptor("Name", "Dataset"),
				                       new ColumnDescriptor("ModelName", "Model"),
				                       new ColumnDescriptor("Abbreviation", "Abbr."),
				                       new ColumnDescriptor("Description"));
		}

		#endregion

		#region IAttributeDependencyObserver

		public void EntityBound()
		{
			AttributeDependency entity = Assert.NotNull(Item.GetEntity());

			if (entity.Dataset != null)
			{
				ITable table = OpenTable(entity);

				IFields fields = table.Fields;
				if (AttributesMissing(fields, entity.SourceAttributes) ||
				    AttributesMissing(fields, entity.TargetAttributes))
				{
					if (Dialog.YesNoFormat(_view, "Attribute mismatch",
					                       "One or more Source and/or Target Attributes do not exist.{0}" +
					                       "Do you want to export the existing configuration before {1} will be corrected?",
					                       Environment.NewLine, Item.Text))
					{
						ExportAttributeDependencyItem();
					}

					Item.DatasetChanged();
				}
			}

			SetupAttributeGrids(entity);
			SetupDependencyGrid(entity);

			SetViewData();
		}

		public void DatasetChanged()
		{
			Item.DatasetChanged();

			AttributeDependency entity = Assert.NotNull(Item.GetEntity());

			SetupAttributeGrids(entity);
			SetupDependencyGrid(entity);

			SetViewData();

			Item.NotifyChanged();
		}

		public void AddSourceAttributesClicked()
		{
			foreach (AttributeTableRow row in _view.GetSelectedAvailableAttributes())
			{
				Item.AddSourceAttribute(row.Name);
			}

			OnAttributesChanged();
		}

		public void RemoveSourceAttributesClicked()
		{
			foreach (AttributeTableRow row in _view.GetSelectedSourceAttributes())
			{
				Item.RemoveSourceAttribute(row.Name);
			}

			OnAttributesChanged();
		}

		public void AddTargetAttributesClicked()
		{
			foreach (AttributeTableRow row in _view.GetSelectedAvailableAttributes())
			{
				Item.AddTargetAttribute(row.Name);
			}

			OnAttributesChanged();
		}

		public void RemoveTargetAttributesClicked()
		{
			foreach (AttributeTableRow row in _view.GetSelectedTargetAttributes())
			{
				Item.RemoveTargetAttribute(row.Name);
			}

			OnAttributesChanged();
		}

		public void AttributeSelectionChanged()
		{
			SetViewData();
		}

		public void ImportMappingsClicked()
		{
			using (var dialog = new OpenFileDialog())
			{
				dialog.Multiselect = false;
				string filePath = GetFilePath(dialog, "txt",
				                              @"Csv files (*.csv)|*.csv|Text files (*.txt)|*.txt");

				if (! string.IsNullOrEmpty(filePath))
				{
					Item.ImportAttributeValueMappings(filePath);

					OnMappingsChanged();
				}
			}
		}

		public void ExportMappingsClicked()
		{
			using (var dialog = new SaveFileDialog())
			{
				string filePath = GetFilePath(dialog, "txt",
				                              @"Csv files (*.csv)|*.csv|Text files (*.txt)|*.txt");

				if (! string.IsNullOrEmpty(filePath))
				{
					Item.ExportAttributeValueMappings(filePath);
				}
			}
		}

		public object FormatMappingValue(object value, int columnIndex,
		                                 Type desiredType)
		{
			if (value == null || value == DBNull.Value)
			{
				return "NULL";
			}

			if (value == Wildcard.Value)
			{
				return Wildcard.ValueString;
			}

			return value.ToString(); // TODO Specify Culture?
		}

		public object ParseMappingValue(object formattedValue, int columnIndex,
		                                Type desiredType)
		{
			FieldType fieldType = GetFieldType(columnIndex);

			object typedValue = AttributeDependencyUtils.Convert(
				formattedValue, fieldType, CultureInfo.CurrentCulture);

			return typedValue;
		}

		public void MappingRowAdded()
		{
			OnMappingsChanged();
		}

		public void MappingRowDeleted()
		{
			OnMappingsChanged();
		}

		public void MappingValueChanged()
		{
			OnMappingsChanged();
		}

		#endregion

		#region Private methods

		private void SetupAttributeGrids([NotNull] AttributeDependency entity)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			using (_msg.IncrementIndentation())
			{
				var availableRows = new SortableBindingList<AttributeTableRow>();
				_sourceRows.Clear();
				_targetRows.Clear();

				if (entity.Dataset != null)
				{
					IList<Attribute> available = Item.GetDatasetAttributes(entity.Dataset)
					                                 .Cast<Attribute>().ToList();

					var source = new List<Attribute>(entity.SourceAttributes.Count);
					var target = new List<Attribute>(entity.TargetAttributes.Count);

					foreach (Attribute attribute in entity.SourceAttributes)
					{
						source.Add(attribute);

						if (available.Contains(attribute))
						{
							available.Remove(attribute);
						}
					}

					foreach (Attribute attribute in entity.TargetAttributes)
					{
						target.Add(attribute);

						if (available.Contains(attribute))
						{
							available.Remove(attribute);
						}
					}

					CreateAttributeRows(availableRows, available);
					CreateAttributeRows(_sourceRows, source);
					CreateAttributeRows(_targetRows, target);
				}

				_view.BindToAvailableAttributeRows(availableRows);
				_view.BindToSourceAttributeRows(_sourceRows);
				_view.BindToTargetAttributeRows(_targetRows);
			}

			_msg.DebugStopTiming(watch, "SetupAttributeGrids");
		}

		private void SetupDependencyGrid([NotNull] AttributeDependency entity)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			if (entity.Dataset != null)
			{
				ITable table = OpenTable(entity);

				IList<AttributeInfo> sources = GetFieldInfoList(entity.SourceAttributes, table);
				IList<AttributeInfo> targets = GetFieldInfoList(entity.TargetAttributes, table);

				_view.SetupMappingGrid(sources, targets, _descriptionFieldName);
			}

			var mappings = new DataView(Item.AttributeValueMappingsTable);
			_view.BindToAttributeValueMappings(mappings);

			_msg.DebugStopTiming(watch, "SetupDependencyGrid");
		}

		[NotNull]
		private static ITable OpenTable([NotNull] AttributeDependency entity)
		{
			ObjectDataset dataset = entity.Dataset;
			Assert.NotNull(dataset, "Dataset not defined for attribute dependency");
			Assert.NotNull(dataset.Model, "Model not defined for attribute dependency");

			IWorkspaceContext workspaceContext =
				ModelElementUtils.GetMasterDatabaseWorkspaceContext(dataset);

			Assert.NotNull(workspaceContext, "The model master database is not accessible");

			return Assert.NotNull(workspaceContext.OpenTable(dataset),
			                      "Dataset not found in model");
		}

		private static bool AttributesMissing(IFields fields,
		                                      IEnumerable<Attribute> attributes)
		{
			return attributes.Select(attribute => attribute.Name)
			                 .Any(fieldName => fields.FindField(fieldName) < 0);
		}

		[NotNull]
		private static IList<AttributeInfo> GetFieldInfoList(
			[NotNull] IList<Attribute> attributes, [NotNull] ITable table)
		{
			var result = new List<AttributeInfo>(attributes.Count);
			result.AddRange(
				attributes.Select(attribute => new AttributeInfo(attribute, table)));

			return result;
		}

		private static void CreateAttributeRows([NotNull] IList<AttributeTableRow> rows,
		                                        [NotNull] IEnumerable<Attribute> attributes)
		{
			rows.Clear();

			foreach (Attribute attribute in attributes)
			{
				rows.Add(new AttributeTableRow(attribute));
			}
		}

		private FieldType GetFieldType(int columnIndex)
		{
			if (0 <= columnIndex && columnIndex < _sourceRows.Count)
			{
				return _sourceRows[columnIndex].Attribute.FieldType;
			}

			columnIndex -= _sourceRows.Count;

			if (0 <= columnIndex && columnIndex < _targetRows.Count)
			{
				return _targetRows[columnIndex].Attribute.FieldType;
			}

			// Neither source nor target attribute:
			// Must be the description column, which is string
			return FieldType.Text;
		}

		private void OnAttributesChanged()
		{
			AttributeDependency entity = Assert.NotNull(Item.GetEntity());

			SetupAttributeGrids(entity);
			SetupDependencyGrid(entity);

			Item.NotifyChanged();

			// After NotifyChanged: it refers to the dirty flag!
			SetViewData();
		}

		private void OnMappingsChanged()
		{
			Item.NotifyChanged();

			// After NotifyChanged: it refers to the dirty flag!
			SetViewData();
		}

		private void SetViewData()
		{
			bool enable = _view.SelectedAvailableAttributeCount > 0;
			_view.AddTargetAttributesEnabled = enable;
			_view.AddSourceAttributesEnabled = enable;

			enable = _view.SelectedSourceAttributeCount > 0;
			_view.RemoveSourceAttributesEnabled = enable;

			enable = _view.SelectedTargetAttributeCount > 0;
			_view.RemoveTargetAttributesEnabled = enable;

			// This exports the entity, not the grid; therefore, if the
			// item is dirty, what is exported isn't what the user sees!
			_view.ExportMappingsEnabled = ! Item.IsDirty;
			_view.ImportMappingsEnabled = true; // always allowed!
		}

		private void ExportAttributeDependencyItem()
		{
			using (var dialog = new SaveFileDialog())
			{
				string filePath = GetFilePath(
					dialog,
					ExchangeAttributeDependenciesCommand<AttributeDependencyItem>.DefaultExtension,
					ExchangeAttributeDependenciesCommand<AttributeDependencyItem>.FileFilter);

				if (! string.IsNullOrEmpty(filePath))
				{
					Item.ExportEntity(filePath);
				}
			}
		}

		[CanBeNull]
		private string GetFilePath(FileDialog dialog, string defaultExt, string filter)
		{
			dialog.CheckPathExists = true;
			dialog.AddExtension = true;
			dialog.DereferenceLinks = true;
			dialog.RestoreDirectory = true;
			dialog.SupportMultiDottedExtensions = true;
			dialog.ValidateNames = true;
			dialog.DefaultExt = defaultExt;
			dialog.Filter = filter;
			dialog.FilterIndex = 0;

			// TODO restore/remember directory?

			DialogResult result = dialog.ShowDialog(_view);

			return result == DialogResult.OK
				       ? dialog.FileName
				       : null;
		}

		#endregion
	}
}
