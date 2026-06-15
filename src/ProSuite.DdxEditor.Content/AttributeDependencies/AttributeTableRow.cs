using System.ComponentModel;
using System.Drawing;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.TableRows;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.UI.Core.DataModel.ResourceLookup;

namespace ProSuite.DdxEditor.Content.AttributeDependencies
{
	public class AttributeTableRow : SelectableTableRow, IEntityRow
	{
		[NotNull] private readonly Attribute _entity;
		private readonly string _name;
		private readonly Image _image;
		private readonly string _fieldType;

		public AttributeTableRow([NotNull] Attribute entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			_entity = entity;

			_name = entity.Name;

			if (entity.Deleted)
			{
				_image = FieldTypeImageLookup.GetImage(entity);
				_fieldType = "<deleted>";
			}
			else
			{
				try
				{
					_image = FieldTypeImageLookup.GetImage(entity);
					_fieldType = Attribute.GetTypeName(entity.FieldType);
				}
				catch (ModelElementAccessException)
				{
					_image = FieldTypeImageLookup.GetUnknownImage();
					_fieldType = "<unknown field type>";
				}
			}
		}

		[Browsable(false)]
		public Attribute Attribute => _entity;

		[DisplayName("")]
		[UsedImplicitly]
		public Image Image => _image;

		[DisplayName("Name")]
		[UsedImplicitly]
		public string Name => _name;

		[DisplayName("Field Type")]
		[UsedImplicitly]
		public string FieldType => _fieldType;

		#region IEntityRow Members

		[Browsable(false)]
		public Entity Entity => _entity;

		#endregion
	}
}
