using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Persistence.WinForms;

namespace ProSuite.DdxEditor.Framework.EntityProperties
{
	public partial class EntityPropertiesForm : Form
	{
		public EntityPropertiesForm([NotNull] Entity entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			InitializeComponent();

			var formStateManager = new BasicFormStateManager(this);
			formStateManager.RestoreState();
			FormClosed += delegate { formStateManager.SaveState(); };

			Bind(GetValues(entity));
		}

		private void Bind([NotNull] IEnumerable<PropertyValue> propertyValues)
		{
			var values = new List<PropertyValue>(propertyValues);

			_dataGridView.AutoGenerateColumns = false;

			_dataGridView.DataSource = typeof(PropertyValue);
			_dataGridView.DataSource = values;
		}

		[NotNull]
		private static IEnumerable<PropertyValue> GetValues([NotNull] Entity entity)
		{
			const string unsaved = "<unsaved entity>";

			yield return new PropertyValue("Type", entity.GetType().Name);
			yield return new PropertyValue("ID", entity.IsPersistent
				                                     ? entity.Id.ToString()
				                                     : unsaved);

			var namedEntity = entity as INamed;
			if (namedEntity != null)
			{
				yield return new PropertyValue("Name", namedEntity.Name);
			}

			var annotatedEntity = entity as IAnnotated;
			if (annotatedEntity != null)
			{
				yield return new PropertyValue("Description",
				                               annotatedEntity.Description);
			}

			var versionedEntity = entity as IVersionedEntity;
			if (versionedEntity != null)
			{
				yield return new PropertyValue("Version",
				                               entity.IsPersistent
					                               ? versionedEntity.Version.ToString()
					                               : unsaved);
			}

			var entityMetadata = entity as IEntityMetadata;
			if (entityMetadata != null)
			{
				yield return new PropertyValue("Created By",
				                               entityMetadata.CreatedByUser);
				yield return new PropertyValue("Created Date",
				                               entityMetadata.CreatedDate);
				yield return new PropertyValue("Last Changed By",
				                               entityMetadata.LastChangedByUser);
				yield return new PropertyValue("Last Changed Date",
				                               entityMetadata.LastChangedDate);
			}
		}

		private void SetStatus([NotNull] string format, params object[] args)
		{
			_statusLabel.Text = string.Format(format, args);
		}

		#region Event handlers

		private void EntityPropertiesForm_Load(object sender, EventArgs e)
		{
			_dataGridView.ClearSelection();
		}

		private void _buttonClose_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void _buttonCopy_Click(object sender, EventArgs e)
		{
			object content = _dataGridView.GetClipboardContent();

			if (content == null)
			{
				SetStatus("Nothing to copy");
				return;
			}

			Clipboard.SetDataObject(content);

			int selectedRows = _dataGridView.SelectedRows.Count;

			SetStatus(selectedRows > 1
				          ? "{0} selected rows copied"
				          : "{0} selected row copied", selectedRows);
		}

		private void _dataGridView_SelectionChanged(object sender, EventArgs e)
		{
			_buttonCopy.Enabled = _dataGridView.SelectedCells.Count > 0;
		}

		#endregion

		#region Nested types

		private class PropertyValue
		{
			public PropertyValue([NotNull] string name, [CanBeNull] object value)
			{
				Assert.ArgumentNotNullOrEmpty(name, nameof(name));

				Name = name;
				Value = value?.ToString();
			}

			[NotNull]
			[UsedImplicitly]
			public string Name { get; }

			[CanBeNull]
			[UsedImplicitly]
			public string Value { get; }
		}

		#endregion
	}
}
