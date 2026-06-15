using System;
using System.Collections.Generic;
using System.Drawing;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.UI.Core.DataModel.ResourceLookup;
using Attribute = ProSuite.DomainModel.Core.DataModel.Attribute;

namespace ProSuite.DdxEditor.Content.AttributeDependencies
{
	public readonly struct AttributeInfo
	{
		[NotNull] public readonly string Name;
		[NotNull] public readonly string Type;
		[NotNull] public readonly Image Image;
		public readonly bool IsNullable;
		public readonly bool IsSubtypeField;
		public readonly object DefaultValue;
		[CanBeNull] public readonly IList<CodedValue> CodedValues;
		[NotNull] public readonly string DomainName;

		public AttributeInfo([NotNull] Attribute attribute,
		                     [NotNull] ITable table)
		{
			Assert.ArgumentNotNull(attribute, nameof(attribute));
			Assert.ArgumentNotNull(table, nameof(table));

			Assert.ArgumentNotNull(table, nameof(table));

			int fieldIndex = AttributeUtils.GetFieldIndex(table, attribute);

			IField field = fieldIndex < 0
				               ? null
				               : table.Fields.Field[fieldIndex];

			Assert.NotNull(field, "field not found for attribute {0}", attribute.Name);

			Name = field.Name ?? string.Empty;
			Type = FieldUtils.GetFieldTypeDisplayText(field.Type);
			Image = FieldTypeImageLookup.GetImage(attribute);
			IsNullable = field.IsNullable;
			DefaultValue = field.DefaultValue;

			IList<CodedValue> subtypes;
			IsSubtypeField = TryGetSubtypes(attribute, table, out subtypes);

			if (IsSubtypeField)
			{
				CodedValues = subtypes;
				DomainName = "Subtypes";
			}
			else
			{
				CodedValues = field.Domain is ICodedValueDomain cvd
					              ? DomainUtils.GetCodedValueList(cvd)
					              : null;
				DomainName = field.Domain?.Name ?? string.Empty;
			}
		}

		private static bool TryGetSubtypes([NotNull] Attribute attribute,
		                                   [NotNull] ITable table,
		                                   [CanBeNull] out IList<CodedValue> result)
		{
			var oa = attribute as ObjectAttribute;
			if (oa == null)
			{
				result = null;
				return false;
			}

			ObjectDataset ds = oa.Dataset;
			if (ds == null)
			{
				result = null;
				return false;
			}

			var objectClass = table as IObjectClass;
			if (objectClass == null)
			{
				result = null;
				return false;
			}

			string subtypeFieldName = DatasetUtils.GetSubtypeFieldName(objectClass);
			if (
				! string.Equals(attribute.Name, subtypeFieldName,
				                StringComparison.OrdinalIgnoreCase))
			{
				result = null;
				return false;
			}

			IList<Subtype> subtypes = DatasetUtils.GetSubtypes(objectClass);
			result = new List<CodedValue>(subtypes.Count);
			foreach (Subtype subtype in subtypes)
			{
				result.Add(new CodedValue(subtype.Code, subtype.Name));
			}

			return true;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
