using System;
using System.ComponentModel;
using System.Drawing;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.TableRows;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.UI.Core.DataModel.ResourceLookup;
using Attribute = ProSuite.DomainModel.Core.DataModel.Attribute;

namespace ProSuite.DdxEditor.Content.Attributes
{
	public class ObjectAttributeTableRow : SelectableTableRow, IEntityRow
	{
		private readonly string _name;
		[NotNull] private readonly ObjectAttribute _entity;
		private readonly string _fieldType;
		private readonly int? _length;
		private readonly string _description;
		private readonly bool _readOnly;
		private readonly bool _isObjectDefining;
		private readonly string _attributeType;
		private readonly string _attributeRole;
		private readonly Image _image;

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectAttributeTableRow"/> class.
		/// </summary>
		/// <param name="entity">The object attribute.</param>
		public ObjectAttributeTableRow([NotNull] ObjectAttribute entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			_entity = entity;

			_name = entity.Name;
			_description = entity.Description;
			_readOnly = entity.ReadOnly;
			_isObjectDefining = entity.IsObjectDefining;

			_attributeType = entity.ObjectAttributeType != null
				                 ? entity.ObjectAttributeType.Name
				                 : string.Empty;

			_attributeRole = entity.Role != null
				                 ? entity.Role.Name
				                 : string.Empty;

			if (entity.Deleted)
			{
				_length = null;
				_fieldType = "<deleted>";

				_image = FieldTypeImageLookup.GetImage(entity);
			}
			else
			{
				try
				{
					_image = FieldTypeImageLookup.GetImage(entity);

					_fieldType = Attribute.GetTypeName(entity.FieldType);

					if (entity.FieldType ==
					    Commons.GeoDb.FieldType.Text)
					{
						_length = entity.FieldLength;
					}
				}
				catch (ModelElementAccessException)
				{
					_image = FieldTypeImageLookup.GetUnknownImage();
					_fieldType = "<unknown field type>";
				}
			}
		}

		[Browsable(false)]
		public ObjectAttribute ObjectAttribute => _entity;

		[DisplayName("")]
		[UsedImplicitly]
		public Image Image => _image;

		[UsedImplicitly]
		public string Name => _name;

		[DisplayName("Field Type")]
		[UsedImplicitly]
		public string FieldType => _fieldType;

		[UsedImplicitly]
		public int? Length => _length;

		[DisplayName("Read Only")]
		[UsedImplicitly]
		public string ReadOnly => GetBooleanAsString(_readOnly);

		[DisplayName("Is Object Defining")]
		[UsedImplicitly]
		public string IsObjectDefining => GetBooleanAsString(_isObjectDefining);

		[DisplayName("Attribute Type")]
		[UsedImplicitly]
		public string AttributeType => _attributeType;

		[DisplayName("Attribute Role")]
		[UsedImplicitly]
		public string AttributeRole => _attributeRole;

		[ColumnConfiguration(MinimumWidth = 200, WrapMode = TriState.True)]
		[UsedImplicitly]
		public string Description => _description;

		[DisplayName("Created")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public DateTime? CreatedDate => _entity.CreatedDate;

		[DisplayName("Created By")]
		[ColumnConfiguration(Width = 80)]
		[UsedImplicitly]
		public string CreatedByUser => _entity.CreatedByUser;

		[DisplayName("Last Changed")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public DateTime? LastChangedDate => _entity.LastChangedDate;

		[DisplayName("Last Changed By")]
		[ColumnConfiguration(MinimumWidth = 90)]
		[UsedImplicitly]
		public string LastChangedByUser => _entity.LastChangedByUser;

		#region IEntityRow Members

		[Browsable(false)]
		public Entity Entity => _entity;

		#endregion
	}
}
