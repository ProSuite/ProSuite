using System;
using System.Collections.Generic;
using System.Globalization;
using ProSuite.Commons.AttributeDependencies;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;
using ProSuite.DomainModel.Core.DataModel;
using Attribute = ProSuite.DomainModel.Core.DataModel.Attribute;

namespace ProSuite.DomainModel.Core.AttributeDependencies
{
	/// <summary>
	/// Domain data model class for AttributeDependency
	/// </summary>
	public class AttributeDependency : VersionedEntityWithMetadata
	{
		[UsedImplicitly] private ObjectDataset _dataset;

		// TODO Consider lists of attribute names, ie, strings
		[UsedImplicitly] private readonly IList<Attribute> _sourceAttributes =
			new List<Attribute>();

		[UsedImplicitly] private readonly IList<Attribute> _targetAttributes =
			new List<Attribute>();

		[UsedImplicitly] private readonly IList<AttributeValueMapping>
			_attributeValueMappings = new List<AttributeValueMapping>();

		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		public AttributeDependency() { }

		public AttributeDependency([NotNull] ObjectDataset dataset) : this()
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			_dataset = dataset;
		}

		#endregion

		[Required]
		public ObjectDataset Dataset
		{
			get { return _dataset; }
			[UsedImplicitly] set { _dataset = value; }
		}

		[NotNull]
		public IList<Attribute> SourceAttributes
		{
			get { return _sourceAttributes; }
		}

		[NotNull]
		public IList<Attribute> TargetAttributes
		{
			get { return _targetAttributes; }
		}

		[NotNull]
		public IList<AttributeValueMapping> AttributeValueMappings
		{
			get { return _attributeValueMappings; }
		}

		/// <summary>
		/// Can we go from target values to source values?
		/// </summary>
		public bool CanReverse
		{
			get { return true; }
			// Once we've source queries (instead of discrete values), this will be false
		}

		public override string ToString()
		{
			return string.Format("AttributeDependency for {0}",
			                     _dataset?.ToString() ?? "<dataset not assigned>");
		}

		/// <summary>
		/// Convert <i>value</i> to a type suitable for <i>fieldType</i>.
		/// If <i>culture</i> is null, use InvariantCulture.
		/// Impossible conversions will throw an exception.
		/// </summary>
		public static object Convert([CanBeNull] object value, FieldType fieldType,
		                             [CanBeNull] IFormatProvider culture)
		{
			if (culture == null)
			{
				culture = CultureInfo.InvariantCulture;
			}

			if (value is string s)
			{
				s = s.Trim();

				if (string.Equals(s, "null", StringComparison.OrdinalIgnoreCase))
				{
					return DBNull.Value;
				}

				if (string.Equals(s, Wildcard.ValueString))
				{
					return Wildcard.Value;
				}

				if (fieldType == FieldType.ShortInteger ||
				    fieldType == FieldType.Integer ||
				    fieldType == FieldType.BigInteger)
				{
					if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase))
					{
						return 0;
					}

					if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase))
					{
						return 1;
					}
				}

				if (fieldType == FieldType.Text)
				{
					if (s.Length > 1 && s[0] == '"' && s[s.Length - 1] == '"')
					{
						s = s.Substring(1, s.Length - 2); // strip quotes
					}

					return s; // trimmed, double quotes stripped
				}
			}

			if (value == null || value == DBNull.Value || value == Wildcard.Value)
			{
				return value; // don't convert these special values
			}

			switch (fieldType)
			{
				case FieldType.ShortInteger:
				case FieldType.Integer:
					return System.Convert.ToInt32(value, culture);
				case FieldType.BigInteger:
					return System.Convert.ToInt64(value, culture);
				case FieldType.Float:
					return System.Convert.ToSingle(value, culture);
				case FieldType.Double:
					return System.Convert.ToDouble(value, culture);

				case FieldType.Text:
					return System.Convert.ToString(value, culture);

				case FieldType.Date:
					return System.Convert.ToDateTime(value, culture);

				case FieldType.Guid:
					throw new NotImplementedException(); // TODO string => GUID

				//case esriFieldType.esriFieldTypeOID:
				//case esriFieldType.esriFieldTypeGeometry:
				//case esriFieldType.esriFieldTypeBlob:
				//case esriFieldType.esriFieldTypeRaster:
				//case esriFieldType.esriFieldTypeGlobalID:
				//case esriFieldType.esriFieldTypeXML:

				default:
					throw new NotImplementedException("fieldType not supported");
			}
		}

		public int GetAttributeIndex(string fieldName, out bool source)
		{
			// TODO Instead of out bool source, consider an enum { None, Source, Target }
			var comparison = StringComparison.Ordinal;

			int index = GetAttributeIndex(SourceAttributes, fieldName, comparison);
			if (index >= 0)
			{
				source = true;
				return index;
			}

			index = GetAttributeIndex(TargetAttributes, fieldName, comparison);
			if (index >= 0)
			{
				source = false;
				return index;
			}

			comparison = StringComparison.OrdinalIgnoreCase;

			index = GetAttributeIndex(SourceAttributes, fieldName, comparison);
			if (index >= 0)
			{
				source = true;
				return index;
			}

			index = GetAttributeIndex(TargetAttributes, fieldName, comparison);
			if (index >= 0)
			{
				source = false;
				return index;
			}

			source = false; // ignored
			return -1; // not found
		}

		private static int GetAttributeIndex(IList<Attribute> attributes, string name,
		                                     StringComparison comparison)
		{
			int count = attributes.Count;

			for (var index = 0; index < count; index++)
			{
				if (attributes[index] is ObjectAttribute attribute &&
				    string.Equals(attribute.Name, name, comparison))
				{
					return index;
				}
			}

			return -1; // not found
		}
	}
}
