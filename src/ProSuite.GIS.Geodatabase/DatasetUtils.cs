using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase
{
	public static class DatasetUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Gets the name of the dataset.
		/// </summary>
		/// <param name="dataset">The dataset.</param>
		/// <returns>fully qualified name ({database.}owner.table) of the dataset.</returns>
		[NotNull]
		public static string GetName([NotNull] IDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			return dataset.Name;
		}

		[NotNull]
		public static string GetAliasName([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			try
			{
				string aliasName = objectClass.AliasName;

				return StringUtils.IsNotEmpty(aliasName)
					       ? aliasName
					       : objectClass.Name;
			}
			catch (NotImplementedException)
			{
				return objectClass.Name;
			}
		}

		[CanBeNull]
		public static IField GetAreaField([NotNull] IFeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			try
			{
				return featureClass.AreaField;
			}
			catch (NotImplementedException)
			{
				// property is not implemented for feature classes from non-Gdb workspaces 
				// ("query layers")
				return null;
			}
		}

		[CanBeNull]
		public static IField GetLengthField([NotNull] IFeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			try
			{
				return featureClass.LengthField;
			}
			catch (NotImplementedException)
			{
				// property is not implemented for feature classes from non-Gdb workspaces 
				// ("query layers")
				return null;
			}
		}

		/// <summary>
		/// Get the index of the named field or throw an exception
		/// if there is no such field.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <param name="fieldName">Name of the field.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">Field not found in table.</exception>
		public static int GetFieldIndex([NotNull] IObjectClass objectClass,
		                                [NotNull] string fieldName)
		{
			return GetFieldIndex((ITable) objectClass, fieldName);
		}

		/// <summary>
		/// Get the index of the named field or throw an exception
		/// if there is no such field.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="fieldName">Name of the field.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">Field not found in table.</exception>
		public static int GetFieldIndex([NotNull] ITable table,
		                                [NotNull] string fieldName)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

			int fieldIndex = table.FindField(fieldName);

			if (fieldIndex < 0)
			{
				throw new ArgumentException(
					$"Field '{fieldName}' not found in '{table.Name}'",
					nameof(fieldName));
			}

			return fieldIndex;
		}

		/// <summary>
		/// Gets the index of the subtype field in a given table.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <returns>The index of the subtype field, or -1 
		/// if the table has no subtype field.</returns>
		public static int GetSubtypeFieldIndex([NotNull] ITable table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			var subtypes = table as ISubtypes;

			return subtypes != null && subtypes.HasSubtype
				       ? subtypes.SubtypeFieldIndex
				       : -1;
		}

		/// <summary>
		/// Gets the index of the subtype field in a given object class.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <returns>The index of the subtype field, or -1 
		/// if the object class has no subtype field.</returns>
		public static int GetSubtypeFieldIndex([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			var subtypes = objectClass as ISubtypes;

			return subtypes != null && subtypes.HasSubtype
				       ? subtypes.SubtypeFieldIndex
				       : -1;
		}

		/// <summary>
		/// Gets the name of the subtype field in a given object class.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <returns>The name of the subtype field, or an empty string
		/// if the object class has no subtype field.</returns>
		[NotNull]
		public static string GetSubtypeFieldName([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			var subtypes = objectClass as ISubtypes;

			return subtypes == null
				       ? string.Empty
				       : subtypes.SubtypeFieldName;
		}

		/// <summary>
		/// Gets the named field or throw an exception if the there is no such field.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="fieldName">Name of the field.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">Field not found in table.</exception>
		[NotNull]
		public static IField GetField([NotNull] ITable table,
		                              [NotNull] string fieldName)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

			IField field = table.Fields.Field[GetFieldIndex(table, fieldName)];

			return Assert.NotNull(field, "field '{0}' not found in '{1}'",
			                      fieldName, GetName(table));
		}

		/// <summary>
		/// Gets the fields.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <returns></returns>
		[NotNull]
		public static IList<IField> GetFields([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			return GetFields((ITable) objectClass);
		}

		/// <summary>
		/// Gets the fields.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <returns></returns>
		[NotNull]
		public static IList<IField> GetFields([NotNull] ITable table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			return GetFields(table.Fields);
		}

		/// <summary>
		/// Gets the fields.
		/// </summary>
		/// <param name="fields">The fields.</param>
		/// <returns></returns>
		[NotNull]
		public static IList<IField> GetFields([NotNull] IFields fields)
		{
			Assert.ArgumentNotNull(fields, nameof(fields));

			int fieldCount = fields.FieldCount;

			var result = new List<IField>(fieldCount);

			result.AddRange(EnumFields(fields));

			return result;
		}

		[NotNull]
		public static IEnumerable<IField> EnumFields([NotNull] IFields fields)
		{
			Assert.ArgumentNotNull(fields, nameof(fields));

			int fieldCount = fields.FieldCount;

			for (var fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
			{
				IField field = fields.Field[fieldIndex];

				yield return field;
			}
		}

		/// <summary>
		/// Gets the display value for a given subtype value
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <param name="subtypeValue">The subtype value.</param>
		/// <returns></returns>
		[CanBeNull]
		public static object GetDisplayValue([NotNull] IObjectClass objectClass,
		                                     int subtypeValue)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			int subtypeFieldIndex = GetSubtypeFieldIndex(objectClass);

			Assert.True(subtypeFieldIndex >= 0, "Object class has no subtypes");

			return GetDisplayValue(objectClass, subtypeFieldIndex,
			                       subtypeValue, subtypeValue);
		}

		/// <summary>
		/// Gets the display value for a given field value
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <param name="fieldIndex">Index of the field.</param>
		/// <param name="fieldValue">The field value.</param>
		/// <param name="subtypeValue">The subtype value.</param>
		/// <returns></returns>
		[CanBeNull]
		public static object GetDisplayValue([NotNull] IObjectClass objectClass,
		                                     int fieldIndex,
		                                     [CanBeNull] object fieldValue,
		                                     [CanBeNull] int? subtypeValue)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			IField field = objectClass.Fields.Field[fieldIndex];
			var subtypes = objectClass as ISubtypes;

			if (fieldValue == null || fieldValue is DBNull)
			{
				return null;
			}

			if (subtypes == null || ! subtypes.HasSubtype)
			{
				var fieldCodedValueDomain = field.Domain as ICodedValueDomain;

				return fieldCodedValueDomain != null
					       ? DomainUtils.GetCodedValueName(fieldCodedValueDomain, fieldValue)
					       : fieldValue;
			}

			if (subtypes.SubtypeFieldIndex == fieldIndex)
			{
				// This is the subtype field. Get name for subtype value
				return GetSubtypeName(objectClass, subtypes, fieldValue);
			}

			// get the field domain for the current subtype value
			IDomain domain;
			if (! subtypeValue.HasValue)
			{
				_msg.Debug("Subtype is null, using default domain for field");
				domain = field.Domain;
			}
			else
			{
				int subtypeCode = subtypeValue.Value;
				try
				{
					domain = subtypes.get_Domain(subtypeCode, field.Name);
				}
				catch (Exception e)
				{
					_msg.Debug(
						string.Format("Error getting domain of subtype {0}; " +
						              "using default domain for field",
						              fieldValue), e);
					domain = field.Domain;
				}
			}

			var codedValueDomain = domain as ICodedValueDomain;
			if (codedValueDomain != null)
			{
				return DomainUtils.GetCodedValueName(codedValueDomain, fieldValue);
			}

			// TODO: Remove that block, after the CONFLICT_ROLE / CONFLICT_ID /
			// and the two INTEGRATION_* attributes are stored with the
			// right domain for every subtype
			var codedValueDomainNoSub = field.Domain as ICodedValueDomain;

			object result = codedValueDomainNoSub != null
				                ? DomainUtils.GetCodedValueName(codedValueDomainNoSub, fieldValue)
				                : fieldValue;
			// Block end

			// TODO: Uncomment when removing the block before...
			//result = value;

			return result;
		}

		public static bool HasSubtypes([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			var subtypes = objectClass as ISubtypes;

			return subtypes != null && subtypes.HasSubtype;
		}

		/// <summary>
		/// Gets the subtypes defined for the object class.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <returns>list of subtypes.</returns>
		[NotNull]
		public static IList<Subtype> GetSubtypes([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));
			return GetSubtypes(objectClass as ISubtypes);
		}

		[NotNull]
		public static IList<Subtype> GetSubtypes([CanBeNull] ISubtypes subtypes)
		{
			var result = new List<Subtype>();

			if (subtypes == null || ! subtypes.HasSubtype)
			{
				return result;
			}

			foreach (KeyValuePair<int, string> subtypeByCode in subtypes.Subtypes)
			{
				int subtypeCode = subtypeByCode.Key;
				string subtypeName = subtypeByCode.Value;

				result.Add(new Subtype(subtypeCode, subtypeName));
			}

			// The enumerator returns subtypes in the order that they are defined.
			// Sort on the subtype code.
			result.Sort(CompareSubtypes);

			return result;
		}

		/// <summary>
		/// Gets a dictionary of subtype names by code for the object class.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <returns>Dictionary of subtype names by code.</returns>
		[NotNull]
		public static IDictionary<int, string> GetSubtypeNamesByCode(
			[NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			var subtypes = objectClass as ISubtypes;

			return GetSubtypeNamesByCode(subtypes);
		}

		public static IDictionary<int, string> GetSubtypeNamesByCode(
			[CanBeNull] ISubtypes subtypes)
		{
			var result = new Dictionary<int, string>();

			if (subtypes == null || ! subtypes.HasSubtype)
			{
				return result;
			}

			foreach (KeyValuePair<int, string> subtypeByCode in subtypes.Subtypes)
			{
				int subtypeCode = subtypeByCode.Key;
				string subtypeName = subtypeByCode.Value;

				result.Add(subtypeCode, subtypeName);
			}

			return result;
		}

		/// <summary>
		/// Gets a dictionary of subtypes by code for the object class.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <returns>Dictionary of subtypes by code.</returns>
		[NotNull]
		public static IDictionary<int, Subtype> GetSubtypesByCode(
			[NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			var result = new Dictionary<int, Subtype>();

			var subtypes = objectClass as ISubtypes;

			if (subtypes == null || ! subtypes.HasSubtype)
			{
				return result;
			}

			foreach (KeyValuePair<int, string> subtypeByCode in subtypes.Subtypes)
			{
				int subtypeCode = subtypeByCode.Key;
				string subtypeName = subtypeByCode.Value;

				result.Add(subtypeCode, new Subtype(subtypeCode, subtypeName));
			}

			return result;
		}

		[NotNull]
		internal static string GetSubtypeName([NotNull] IObjectClass objectClass,
		                                      [NotNull] ISubtypes subtypes,
		                                      [NotNull] object fieldValue)
		{
			try
			{
				int subtypeCode = GetSubtypeCode(fieldValue);

				return subtypes.get_SubtypeName(subtypeCode);
			}
			catch (Exception e)
			{
				// probably an illegal subtype
				if (_msg.IsVerboseDebugEnabled)
				{
					_msg.DebugFormat("Error getting name of subtype {0} for class {1}: {2}",
					                 fieldValue, (objectClass).Name, e.Message);
				}

				return string.Format("<unknown subtype: {0}>", fieldValue);
			}
		}

		private static int GetSubtypeCode([NotNull] object subtypeFieldValue)
		{
			Assert.ArgumentNotNull(subtypeFieldValue, nameof(subtypeFieldValue));

			return subtypeFieldValue as int? ?? Convert.ToInt32(subtypeFieldValue);
		}

		private static int CompareSubtypes([CanBeNull] Subtype x,
		                                   [CanBeNull] Subtype y)
		{
			if (x == null)
			{
				if (y == null)
				{
					// If x is null and y is null, they're equal. 
					return 0;
				}

				// If x is null and y is not null, y is greater. 
				return -1;
			}

			// If x is not null...
			if (y == null)
				// ...and y is null, x is greater.
			{
				return 1;
			}

			// ...and y is not null, compare the subtypes by their code
			// lengths of the two strings.
			return x.Code.CompareTo(y.Code);
		}
	}
}
