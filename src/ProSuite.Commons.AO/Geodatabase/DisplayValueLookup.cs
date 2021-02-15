using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	/// <summary>
	/// Allows efficient lookup of display values for geodatabase fields. Maintains
	/// cached lookup tables for subtypes and coded value domains.
	/// </summary>
	public class DisplayValueLookup
	{
		#region Fields

		private readonly IObjectClass _objectClass;
		private readonly bool _hasSubtypes;
		private readonly int _subtypeFieldIndex;

		private readonly Dictionary<int, FieldLookup> _fieldLookupByIndex =
			new Dictionary<int, FieldLookup>();

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DisplayValueLookup"/> class.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		public DisplayValueLookup([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			_objectClass = objectClass;

			var subtypes = (ISubtypes) _objectClass;
			_hasSubtypes = subtypes.HasSubtype;
			_subtypeFieldIndex = subtypes.SubtypeFieldIndex;

			for (var fieldIndex = 0;
			     fieldIndex < _objectClass.Fields.FieldCount;
			     fieldIndex++)
			{
				_fieldLookupByIndex.Add(fieldIndex, CreateFieldLookup(fieldIndex));
			}
		}

		#endregion

		/// <summary>
		/// Gets the display value for the field at a given index, for a given row.
		/// </summary>
		/// <param name="row">The row.</param>
		/// <param name="fieldIndex">Index of the field.</param>
		/// <returns></returns>
		public object GetDisplayValue([NotNull] IRow row, int fieldIndex)
		{
			FieldLookup fieldLookup = _fieldLookupByIndex[fieldIndex];

			return fieldLookup.GetDisplayValue(row);
		}

		#region Non-public members

		[NotNull]
		private FieldLookup CreateFieldLookup(int fieldIndex)
		{
			// TODO revise: handle situation where field has no domain assigned globally, but the field has domains for (some) subtypes.
			// TODO: make use of generics to avoid boxing

			IField field = _objectClass.Fields.Field[fieldIndex];

			if (_hasSubtypes && fieldIndex == _subtypeFieldIndex)
			{
				return new FieldLookupSubtypes(_objectClass, fieldIndex);
			}

			if (field.Domain is ICodedValueDomain)
			{
				if (_hasSubtypes)
				{
					return new FieldLookupSubtypeDomain(_objectClass, fieldIndex);
				}

				return new FieldLookupFixedDomain((ICodedValueDomain) field.Domain,
				                                  fieldIndex);
			}

			return new FieldLookupPassthrough(fieldIndex);
		}

		#endregion

		#region Nested types

		/// <summary>
		/// Base class for field lookup classes
		/// </summary>
		private abstract class FieldLookup
		{
			private readonly int _fieldIndex;
			private const string _nullDisplayValue = "<Null>";

			/// <summary>
			/// Initializes a new instance of the <see cref="FieldLookup"/> class.
			/// </summary>
			/// <param name="fieldIndex">Index of the field.</param>
			protected FieldLookup(int fieldIndex)
			{
				_fieldIndex = fieldIndex;
			}

			public abstract object GetDisplayValue([NotNull] IRow row);

			protected object GetValue([NotNull] IRow row)
			{
				return row.Value[_fieldIndex];
			}

			protected static string NullDisplayValue => _nullDisplayValue;
		}

		/// <summary>
		/// Field lookup class for a subtype field
		/// </summary>
		private class FieldLookupSubtypes : FieldLookup
		{
			private readonly Dictionary<object, string> _nameByValue =
				new Dictionary<object, string>();

			/// <summary>
			/// Initializes a new instance of the <see cref="FieldLookupSubtypes"/> class.
			/// </summary>
			/// <param name="objectClass">The object class.</param>
			/// <param name="fieldIndex">Index of the field.</param>
			public FieldLookupSubtypes([NotNull] IObjectClass objectClass, int fieldIndex)
				: base(fieldIndex)
			{
				Assert.ArgumentNotNull(objectClass, nameof(objectClass));

				foreach (
					KeyValuePair<int, string> pair in DatasetUtils.GetSubtypeNamesByCode(
						objectClass)
				)
				{
					_nameByValue.Add(pair.Key, pair.Value);
				}
			}

			public override object GetDisplayValue(IRow row)
			{
				object value = GetValue(row);

				if (value == null || value is DBNull)
				{
					return NullDisplayValue;
				}

				string displayValue;
				bool ok = _nameByValue.TryGetValue(value, out displayValue);

				return ok
					       ? displayValue
					       : string.Format("<Unknown: {0}>", value);
			}
		}

		/// <summary>
		/// Base class for lookup of fields with a coded value domain.
		/// </summary>
		private abstract class FieldLookupDomainBase : FieldLookup
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="FieldLookupDomainBase"/> class.
			/// </summary>
			/// <param name="fieldIndex">Index of the field.</param>
			protected FieldLookupDomainBase(int fieldIndex) : base(fieldIndex) { }

			[NotNull]
			protected static Dictionary<object, string> GetCodedValueMap(
				[NotNull] ICodedValueDomain domain)
			{
				Assert.ArgumentNotNull(domain, nameof(domain));

				IList<CodedValue> codedValues = DomainUtils.GetCodedValueList(domain);

				var namesByValue =
					new Dictionary<object, string>(codedValues.Count);

				foreach (CodedValue codedValue in codedValues)
				{
					namesByValue.Add(codedValue.Value, codedValue.Name);
				}

				// add special DBNull value
				namesByValue.Add(DBNull.Value, string.Empty);

				return namesByValue;
			}

			protected static object Lookup(object value,
			                               [NotNull] IDictionary<object, string> nameValueMap)
			{
				string displayValue;
				bool ok = nameValueMap.TryGetValue(value, out displayValue);

				return ok
					       ? displayValue
					       : string.Format("<Unknown: {0}", value);
			}
		}

		/// <summary>
		/// Field lookup class for a field with a subtype-dependent coded value domain.
		/// </summary>
		private class FieldLookupSubtypeDomain : FieldLookupDomainBase
		{
			private readonly int _subtypeFieldIndex;

			[NotNull] private readonly Dictionary<object, string> _nameByValueDefaultDomain;

			[NotNull] private readonly Dictionary<int, Dictionary<object, string>>
				_displayValueMap =
					new Dictionary<int, Dictionary<object, string>>();

			/// <summary>
			/// Initializes a new instance of the <see cref="FieldLookupSubtypeDomain"/> class.
			/// </summary>
			/// <param name="objectClass">The object class.</param>
			/// <param name="fieldIndex">Index of the field.</param>
			public FieldLookupSubtypeDomain([NotNull] IObjectClass objectClass, int fieldIndex)
				: base(fieldIndex)
			{
				Assert.ArgumentNotNull(objectClass, nameof(objectClass));

				IField field = objectClass.Fields.Field[fieldIndex];

				var classSubtypes = (ISubtypes) objectClass;
				_subtypeFieldIndex = classSubtypes.SubtypeFieldIndex;

				IList<Subtype> subtypes = DatasetUtils.GetSubtypes(objectClass);

				foreach (Subtype subtype in subtypes)
				{
					ICodedValueDomain domain =
						(ICodedValueDomain) classSubtypes.Domain[subtype.Code, field.Name]
						??
						(ICodedValueDomain) field.Domain;

					Dictionary<object, string> namesByValue = GetCodedValueMap(domain);

					_displayValueMap.Add(subtype.Code, namesByValue);
				}

				// get default domain values
				_nameByValueDefaultDomain = GetCodedValueMap(
					(ICodedValueDomain) field.Domain);
			}

			public override object GetDisplayValue(IRow row)
			{
				object value = GetValue(row);

				if (value == null || value is DBNull)
				{
					return NullDisplayValue;
				}

				// TODO: read this only once for row --> use something like a RowContext
				// Decide after profiling.
				object subtypeValue = row.Value[_subtypeFieldIndex];

				if (subtypeValue == null || subtypeValue is DBNull)
				{
					return Lookup(value, _nameByValueDefaultDomain);
				}

				Dictionary<object, string> nameByValue;
				bool ok = _displayValueMap.TryGetValue((int) subtypeValue,
				                                       out nameByValue);

				return ok
					       ? Lookup(value, nameByValue)
					       : Lookup(value, _nameByValueDefaultDomain);
			}
		}

		/// <summary>
		/// Field lookup class for a field with a fixed coded value domain.
		/// </summary>
		private class FieldLookupFixedDomain : FieldLookupDomainBase
		{
			private readonly Dictionary<object, string> _nameByValue;

			/// <summary>
			/// Initializes a new instance of the <see cref="FieldLookupFixedDomain"/> class.
			/// </summary>
			/// <param name="domain">The domain.</param>
			/// <param name="fieldIndex">Index of the field.</param>
			public FieldLookupFixedDomain(ICodedValueDomain domain, int fieldIndex)
				: base(fieldIndex)
			{
				_nameByValue = GetCodedValueMap(domain);
			}

			public override object GetDisplayValue(IRow row)
			{
				object value = GetValue(row);

				if (value == null || value is DBNull)
				{
					return NullDisplayValue;
				}

				return Lookup(value, _nameByValue);
			}
		}

		/// <summary>
		/// Field lookup class that simply returns the original value.
		/// </summary>
		private class FieldLookupPassthrough : FieldLookup
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="FieldLookupPassthrough"/> class.
			/// </summary>
			/// <param name="fieldIndex">Index of the field.</param>
			public FieldLookupPassthrough(int fieldIndex) : base(fieldIndex) { }

			public override object GetDisplayValue(IRow row)
			{
				return GetValue(row);
			}
		}

		#endregion
	}
}
