using System;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	/// <summary>
	/// Factory to create variant values based on geodatabase field types.
	/// </summary>
	public static class VariantValueFactory
	{
		[NotNull]
		public static VariantValue Create([NotNull] Attribute attribute,
		                                  [CanBeNull] object attributeValue)
		{
			Assert.ArgumentNotNull(attribute, nameof(attribute));

			return Create(attribute.FieldType, attributeValue);
		}

		[NotNull]
		public static VariantValue Create(FieldType fieldType,
		                                  [CanBeNull] object value)
		{
			VariantValueType valueType = GetValueType(fieldType);

			return new VariantValue(value, valueType);
		}

		public static VariantValueType GetValueType(FieldType fieldType)
		{
			VariantValueType valueType;
			if (TryGetValueType(fieldType, out valueType))
			{
				return valueType;
			}

			throw new ArgumentOutOfRangeException(
				nameof(fieldType), fieldType,
				string.Format("Unsupported field type for VariantValues: {0}",
				              fieldType));
		}

		public static bool IsSupported(FieldType fieldType)
		{
			return TryGetValueType(fieldType, out VariantValueType _);
		}

		private static bool TryGetValueType(FieldType fieldType,
		                                    out VariantValueType valueType)
		{
			switch (fieldType)
			{
				case FieldType.ShortInteger:
				case FieldType.Integer:
					valueType = VariantValueType.Integer;
					return true;

				case FieldType.Float:
				case FieldType.Double:
					valueType = VariantValueType.Double;
					return true;

				case FieldType.Text:
					valueType = VariantValueType.String;
					return true;

				case FieldType.Date:
					valueType = VariantValueType.DateTime;
					return true;

				case FieldType.ObjectID:
				case FieldType.Geometry:
				case FieldType.Blob:
				case FieldType.Raster:
				case FieldType.Guid:
				case FieldType.GlobalID:
				case FieldType.Xml:
					valueType = VariantValueType.Null;
					return false;

				default:
					throw new ArgumentOutOfRangeException(
						nameof(fieldType), fieldType,
						string.Format("Unknown field type: {0}", fieldType));
			}
		}

		public static VariantValueType GetValueType([NotNull] Attribute attribute)
		{
			Assert.ArgumentNotNull(attribute, nameof(attribute));

			return GetValueType(attribute.FieldType);
		}
	}
}
