using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Finder;
using ProSuite.Commons.UI.WinForms;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.Commons.Validation;
using ProSuite.DdxEditor.Content.Datasets;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.AttributeDependencies;
using ProSuite.DomainModel.Core.AttributeDependencies.Repositories;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;
using Attribute = ProSuite.DomainModel.Core.DataModel.Attribute;

namespace ProSuite.DdxEditor.Content.AttributeDependencies
{
	public class AttributeDependencyItem :
		SimpleEntityItem<AttributeDependency, AttributeDependency>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const string _descriptionColumnName = "Description";
		[CanBeNull] private DataTable _attributeValueMappingsTable;

		private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		public AttributeDependencyItem(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] AttributeDependency attributeDependency,
			[NotNull] IRepository<AttributeDependency> repository)
			: base(attributeDependency, repository)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		#region Framework overrides

		public override Image Image => Resources.AttributeDependencyItem;

		protected override void CollectCommands(List<ICommand> commands,
		                                        IApplicationController applicationController)
		{
			base.CollectCommands(commands, applicationController);

			commands.Add(new ExportAttributeDependencyCommand(this,
			                                                  applicationController));
		}

		protected override bool AllowDelete => true;

		protected override IWrappedEntityControl<AttributeDependency>
			CreateEntityControl(IItemNavigation itemNavigation)
		{
			var control = new AttributeDependencyControl();

			new AttributeDependencyPresenter(this, control, _descriptionColumnName);

			return control;
		}

		protected override string GetText(AttributeDependency entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			Dataset dataset = entity.Dataset;
			return dataset == null
				       ? "(No Dataset)"
				       : $"{dataset.Name} [{(dataset.Model == null ? "(No Model)" : dataset.Model.Name)}]";
		}

		protected override void IsValidForPersistenceCore(AttributeDependency entity,
		                                                  Notification notification)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));
			Assert.ArgumentNotNull(notification, nameof(notification));

			if (entity.Dataset == null)
			{
				return; // already reported by entity
			}

			// check if another entity for the same dataset exists
			AttributeDependency existing =
				_modelBuilder.AttributeDependencies.Get(entity.Dataset);

			if (existing != null && existing.Id != entity.Id)
			{
				notification.RegisterMessage("Dataset",
				                             "An AttributeDependency for the same dataset already exists",
				                             Severity.Error);
			}
		}

		protected override void RequestCommitCore()
		{
			// Ensure entity's AttributeValueMappings match the grid:
			AttributeDependency entity = Assert.NotNull(GetEntity());

			ApplyMappingsTable(AttributeValueMappingsTable, entity);

			base.RequestCommitCore();
		}

		protected override void DiscardChangesCore()
		{
			InvalidateMappingsTable();

			base.DiscardChangesCore();
		}

		protected override void OnUnloaded(EventArgs e)
		{
			InvalidateMappingsTable();
		}

		public override IList<DependingItem> GetDependingItems()
		{
			return _modelBuilder.GetDependingItems(GetEntity());
		}

		#endregion

		public DataTable AttributeValueMappingsTable
		{
			get
			{
				if (_attributeValueMappingsTable == null)
				{
					AttributeDependency entity = Assert.NotNull(GetEntity());

					_attributeValueMappingsTable = CreateMappingsTable(entity);

					LoadMappingsTable(_attributeValueMappingsTable, entity);
				}

				return _attributeValueMappingsTable;
			}
		}

		#region Maintain Dataset and Attributes

		public IList<DatasetTableRow> GetDatasetTableRows()
		{
			return _modelBuilder.ReadOnlyTransaction(
				delegate
				{
					var rows = new List<DatasetTableRow>();
					IDatasetRepository datasetRepository = _modelBuilder.Datasets;

					var existing = new List<ObjectDataset>();
					foreach (
						AttributeDependency dependency in
						_modelBuilder.AttributeDependencies.GetAll())
					{
						existing.Add(dependency.Dataset);
					}

					foreach (
						ObjectDataset dataset in
						datasetRepository.GetAll<ObjectDataset>())
					{
						var tableRow = new DatasetTableRow(dataset);
						tableRow.Selectable = ! existing.Contains(dataset);
						rows.Add(tableRow);
					}

					return rows;
				});
		}

		[CanBeNull]
		public Dataset FindDataset([NotNull] IWin32Window owner,
		                           params ColumnDescriptor[] columns)
		{
			IFinder<DatasetTableRow> finder = new Finder<DatasetTableRow>();

			IList<DatasetTableRow> rows = GetDatasetTableRows();

			DatasetTableRow selected = finder.ShowDialog(owner, rows, columns);

			return selected?.Dataset;
		}

		public IList<ObjectAttribute> GetDatasetAttributes([NotNull] ObjectDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			return _modelBuilder.ReadOnlyTransaction(
				() => new List<ObjectAttribute>(dataset.GetAttributes()));
		}

		public void DatasetChanged()
		{
			AttributeDependency entity = Assert.NotNull(GetEntity());

			// Old attributes and mappings no longer apply:
			entity.AttributeValueMappings.Clear();
			entity.SourceAttributes.Clear();
			entity.TargetAttributes.Clear();

			InvalidateMappingsTable();
		}

		public void AddSourceAttribute(string name)
		{
			AttributeDependency entity = Assert.NotNull(GetEntity());
			Attribute attribute = GetAttribute(entity, name);

			if (! entity.SourceAttributes.Contains(attribute))
			{
				entity.SourceAttributes.Add(attribute);
			}

			InvalidateMappingsTable();
		}

		public void RemoveSourceAttribute(string name)
		{
			AttributeDependency entity = Assert.NotNull(GetEntity());
			Attribute attribute = GetAttribute(entity, name);

			if (entity.SourceAttributes.Contains(attribute))
			{
				entity.SourceAttributes.Remove(attribute);
			}

			InvalidateMappingsTable();
		}

		public void AddTargetAttribute(string name)
		{
			AttributeDependency entity = Assert.NotNull(GetEntity());
			Attribute attribute = GetAttribute(entity, name);

			if (! entity.TargetAttributes.Contains(attribute))
			{
				entity.TargetAttributes.Add(attribute);
			}

			InvalidateMappingsTable();
		}

		public void RemoveTargetAttribute(string name)
		{
			AttributeDependency entity = Assert.NotNull(GetEntity());
			Attribute attribute = GetAttribute(entity, name);

			if (entity.TargetAttributes.Contains(attribute))
			{
				entity.TargetAttributes.Remove(attribute);
			}

			InvalidateMappingsTable();
		}

		#endregion

		#region Import and Export

		public void ExportEntity(string xmlFilePath)
		{
			Assert.ArgumentNotNull(xmlFilePath, nameof(xmlFilePath));

			AttributeDependency entity = Assert.NotNull(GetEntity());

			using (new WaitCursor())
			{
				_modelBuilder.UseTransaction(
					delegate
					{
						_modelBuilder.Reattach(entity);

						_modelBuilder.AttributeDependenciesExporter
						             .Export(xmlFilePath, entity);
					});
			}

			_msg.InfoFormat("{0}: exported to {1}", entity, xmlFilePath);
		}

		public void ImportAttributeValueMappings([NotNull] string filePath)
		{
			Assert.ArgumentNotNullOrEmpty(filePath, nameof(filePath));

			AttributeDependency entity = Assert.NotNull(GetEntity());

			using (new WaitCursor())
			{
				// No transaction for import! Otherwise, user cannot discard the import.
				// BUT be sure the collection is initialized (it will be modified).
				_modelBuilder.UseTransaction(
					delegate
					{
						_modelBuilder.Initialize(entity.AttributeValueMappings);

						using (Stream stream = new FileStream(filePath, FileMode.Open))
						{
							using (TextReader reader = new StreamReader(stream, Encoding.UTF8))
							{
								if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
								{
									AttributeDependencyUtils.ImportMappingsCsv(entity, reader);
								}
								else
								{
									AttributeDependencyUtils.ImportMappingsTxt(entity, reader);
								}
							}
						}
					});
			}

			_msg.InfoFormat("{0}: Mappings imported from {1}", entity, filePath);

			LoadMappingsTable(AttributeValueMappingsTable, entity);
		}

		public void ExportAttributeValueMappings([NotNull] string filePath)
		{
			Assert.ArgumentNotNullOrEmpty(filePath, nameof(filePath));

			AttributeDependency entity = Assert.NotNull(GetEntity());

			using (new WaitCursor())
			{
				_modelBuilder.UseTransaction(
					delegate
					{
						_modelBuilder.Reattach(entity);

						using (Stream stream = new FileStream(filePath, FileMode.Create))
						{
							using (TextWriter writer = new StreamWriter(stream, Encoding.UTF8))
							{
								if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
								{
									AttributeDependencyUtils.ExportMappingsCsv(entity, writer);
								}
								else
								{
									AttributeDependencyUtils.ExportMappingsTxt(entity, writer);
								}
							}
						}
					});
			}

			_msg.InfoFormat("{0}: Mappings exported to {1}", entity, filePath);
		}

		#endregion

		#region Private methods

		[NotNull]
		private static DataTable CreateMappingsTable(
			[NotNull] AttributeDependency entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			var table = new DataTable("AttributeValueMappings");

			foreach (Attribute attribute in entity.SourceAttributes)
			{
				Type type = typeof(object); // allow all types!
				table.Columns.Add(attribute.Name, type);
			}

			foreach (Attribute attribute in entity.TargetAttributes)
			{
				Type type = typeof(object); // allow all types!
				table.Columns.Add(attribute.Name, type);
			}

			table.Columns.Add(_descriptionColumnName);

			return table;
		}

		private void InvalidateMappingsTable()
		{
			_attributeValueMappingsTable = null;
		}

		private static void LoadMappingsTable([NotNull] DataTable table,
		                                      [NotNull] AttributeDependency entity)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(entity, nameof(entity));

			int sourceAttributeCount = entity.SourceAttributes.Count;
			int targetAttributeCount = entity.TargetAttributes.Count;

			int attributeCount = sourceAttributeCount + targetAttributeCount;
			int columnCount = table.Columns.Count;
			Assert.True(
				columnCount == attributeCount || columnCount == attributeCount + 1,
				"DataTable has {0} columns, but we need {1} or {2} columns",
				columnCount, attributeCount, attributeCount + 1);

			table.Rows.Clear();

			foreach (AttributeValueMapping mapping in entity.AttributeValueMappings)
			{
				DataRow row = null;

				try
				{
					row = table.NewRow();

					var column = 0;
					foreach (object value in mapping.SourceValues)
					{
						row[column++] = value;
					}

					column = sourceAttributeCount;
					foreach (object value in mapping.TargetValues)
					{
						row[column++] = value;
					}

					if (columnCount > attributeCount)
					{
						column = attributeCount;
						row[column] = mapping.Description ?? string.Empty;
					}
				}
				catch (Exception ex)
				{
					throw new Exception(
						$"Error loading mappings. Parsing AttributeValueMapping failed: {mapping}",
						ex);
				}
				finally
				{
					if (row != null)
					{
						table.Rows.Add(row);
					}
				}
			}

			table.AcceptChanges();
		}

		private void ApplyMappingsTable([NotNull] DataTable table,
		                                [NotNull] AttributeDependency entity)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(entity, nameof(entity));

			var mappings = new List<AttributeValueMapping>();
			var values = new List<object>();
			var sb = new StringBuilder();

			foreach (DataRowView row in new DataView(table))
			{
				GetRowValues(row, entity.SourceAttributes, values);
				string sourceText = AttributeDependencyUtils.Format(values, sb);

				GetRowValues(row, entity.TargetAttributes, values);
				string targetText = AttributeDependencyUtils.Format(values, sb);

				var description = row[_descriptionColumnName] as string;

				mappings.Add(new AttributeValueMapping(sourceText, targetText, description));
			}

			_modelBuilder.UseTransaction(
				delegate
				{
					if (entity.IsPersistent)
					{
						_modelBuilder.Reattach(entity);
					}

					ReplaceMappings(entity, mappings,
					                _modelBuilder.AttributeValueMappings);
				});
		}

		private static void ReplaceMappings(
			AttributeDependency entity,
			IEnumerable<AttributeValueMapping> mappings,
			IAttributeValueMappingRepository repository)
		{
			foreach (AttributeValueMapping mapping in entity.AttributeValueMappings)
			{
				if (mapping.IsPersistent)
				{
					repository.Delete(mapping);
				}
			}

			entity.AttributeValueMappings.Clear();

			foreach (AttributeValueMapping mapping in mappings)
			{
				if (! mapping.IsPersistent)
				{
					repository.Save(mapping);
				}

				entity.AttributeValueMappings.Add(mapping);
			}
		}

		private static void GetRowValues(DataRowView row, IEnumerable<Attribute> attributes,
		                                 ICollection<object> result)
		{
			result.Clear();

			foreach (Attribute attribute in attributes)
			{
				string fieldName = attribute.Name;
				object value = row[fieldName];
				result.Add(value);
			}
		}

		private static Attribute GetAttribute(AttributeDependency entity, string name)
		{
			ObjectDataset dataset = entity.Dataset;
			Assert.NotNull(dataset, "entity.Dataset is null");

			Attribute attribute = dataset.GetAttribute(name);
			return Assert.NotNull(attribute, "No such attribute: {0}", name);
		}

		#endregion
	}
}
