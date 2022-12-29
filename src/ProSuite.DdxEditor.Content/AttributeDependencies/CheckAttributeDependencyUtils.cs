using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AttributeDependencies;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.AttributeDependencies;
using Attribute = ProSuite.DomainModel.Core.DataModel.Attribute;

namespace ProSuite.DdxEditor.Content.AttributeDependencies
{
	public static class CheckAttributeDependencyUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static void CheckAttributeDependency(
			[NotNull] AttributeDependency attributeDependency)
		{
			Assert.ArgumentNotNull(attributeDependency, nameof(attributeDependency));

			IObjectClass objectClass = CheckObjectClassIsValid(
				attributeDependency, out var sourceFields, out var targetFields);
			if (objectClass == null)
			{
				return;
			}

			if (attributeDependency.AttributeValueMappings.Count < 1)
			{
				_msg.WarnFormat("No mapping (attribute value combination) defined ({0})",
				                attributeDependency);
				return;
			}

			CheckUniqueSourceValueCombinations(attributeDependency);

			IField configuredSubtypeField = GetConfiguredSubtypeField(
				attributeDependency, objectClass, sourceFields, targetFields);

			if (! DatasetUtils.HasSubtypes(objectClass) || configuredSubtypeField == null)
			{
				foreach (var mapping in attributeDependency.AttributeValueMappings)
				{
					CheckFieldValuesAreValid(attributeDependency, mapping, objectClass,
					                         sourceFields, mapping.SourceValues);
					CheckFieldValuesAreValid(attributeDependency, mapping, objectClass,
					                         targetFields, mapping.TargetValues);
				}

				return;
			}

			int index =
				attributeDependency.GetAttributeIndex(configuredSubtypeField.Name, out bool source);

			foreach (var mapping in attributeDependency.AttributeValueMappings)
			{
				object subtypeValue = source
					                      ? mapping.SourceValues[index]
					                      : mapping.TargetValues[index];
				if (subtypeValue == Wildcard.Value)
				{
					_msg.WarnFormat(
						"ObjectClass uses subtypes but subtype-field {0} is configured with value {1}. " +
						"Can not check subtype-specific domains for {2} ({3})",
						configuredSubtypeField.Name, Wildcard.Value, mapping, attributeDependency);
					CheckFieldValuesAreValid(attributeDependency, mapping, objectClass,
					                         sourceFields, mapping.SourceValues);
					CheckFieldValuesAreValid(attributeDependency, mapping, objectClass,
					                         targetFields, mapping.TargetValues);
				}
				else
				{
					int subtypeCode = GetSubtypeCode(objectClass, subtypeValue);
					if (subtypeCode == -1)
					{
						_msg.WarnFormat(
							"Subtype-field {0} does not contain subtype-code matching {1} in {2} ({3})",
							configuredSubtypeField.Name, subtypeValue,
							mapping, attributeDependency);
					}

					CheckFieldValuesAreValid(attributeDependency, mapping, objectClass,
					                         sourceFields, mapping.SourceValues, subtypeCode);
					CheckFieldValuesAreValid(attributeDependency, mapping, objectClass,
					                         targetFields, mapping.TargetValues, subtypeCode);
				}
			}
		}

		[CanBeNull]
		private static IObjectClass CheckObjectClassIsValid(
			[NotNull] AttributeDependency attributeDependency,
			[CanBeNull] out IList<IField> sourceFields,
			[CanBeNull] out IList<IField> targetFields)
		{
			sourceFields = null;
			targetFields = null;
			if (attributeDependency.Dataset == null)
			{
				_msg.WarnFormat("Dataset is null ({0})", attributeDependency);
				return null;
			}

			if (attributeDependency.SourceAttributes.Count < 1)
			{
				_msg.WarnFormat("No Source Attribute defined ({0})", attributeDependency);
				return null;
			}

			if (attributeDependency.TargetAttributes.Count < 1)
			{
				_msg.WarnFormat("No Target Attribute defined ({0})", attributeDependency);
				return null;
			}

			sourceFields = new List<IField>(attributeDependency.SourceAttributes.Count);
			targetFields = new List<IField>(attributeDependency.TargetAttributes.Count);

			var anyFieldMissing = false;

			IObjectClass objectClass = TryOpenObjectClass(attributeDependency);
			if (objectClass == null)
			{
				return null;
			}

			IFields fields = objectClass.Fields;
			foreach (Attribute sourceAttribute in attributeDependency.SourceAttributes)
			{
				int index = fields.FindField(sourceAttribute.Name);
				if (index < 0)
				{
					_msg.WarnFormat("Source Attribute {0} not found ({1})", sourceAttribute.Name,
					                attributeDependency);
					anyFieldMissing = true;
				}

				sourceFields.Add(fields.Field[index]);
			}

			foreach (Attribute targetAttribute in attributeDependency.TargetAttributes)
			{
				int index = fields.FindField(targetAttribute.Name);
				if (index < 0)
				{
					_msg.WarnFormat("Target Attribute {0} not found ({1})", targetAttribute.Name,
					                attributeDependency);
					anyFieldMissing = true;
				}

				targetFields.Add(fields.Field[index]);
			}

			return anyFieldMissing ? null : objectClass;
		}

		[CanBeNull]
		private static IObjectClass TryOpenObjectClass(AttributeDependency attributeDependency)
		{
			try
			{
				IWorkspaceContext workspaceContext =
					ModelElementUtils.GetAccessibleMasterDatabaseWorkspaceContext(
						attributeDependency.Dataset);

				IObjectClass objectClass =
					Assert.NotNull(workspaceContext).OpenObjectClass(attributeDependency.Dataset);

				if (objectClass == null)
				{
					throw new Exception("ObjectClass could not be opened.");
				}

				return objectClass;
			}
			catch (Exception ex)
			{
				_msg.Error(
					string.Format("Error checking {0}: {1}", attributeDependency, ex.Message), ex);
				return null;
			}
		}

		private static void CheckUniqueSourceValueCombinations(
			AttributeDependency attributeDependency)
		{
			IList<string> sources = new List<string>();
			foreach (var mapping in attributeDependency.AttributeValueMappings)
			{
				if (sources.Contains(mapping.SourceText))
				{
					_msg.WarnFormat("Duplicate source value combination in {0} ({1})", mapping,
					                attributeDependency);
				}

				sources.Add(mapping.SourceText);
			}
		}

		private static IField GetConfiguredSubtypeField(
			AttributeDependency attributeDependency, IObjectClass objectClass,
			IEnumerable<IField> sourceFields, IEnumerable<IField> targetFields)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			if (! DatasetUtils.HasSubtypes(objectClass))
			{
				return null;
			}

			IField configuredSubtypeField = null;
			string subtypeFieldName = DatasetUtils.GetSubtypeFieldName(objectClass);
			foreach (IField sourceField in sourceFields)
			{
				if (sourceField.Name.Equals(subtypeFieldName, StringComparison.OrdinalIgnoreCase))
				{
					configuredSubtypeField = sourceField;
				}
			}

			foreach (IField targetField in targetFields)
			{
				if (targetField.Name.Equals(subtypeFieldName, StringComparison.OrdinalIgnoreCase))
				{
					configuredSubtypeField = targetField;
				}
			}

			if (configuredSubtypeField == null)
			{
				_msg.WarnFormat(
					"ObjectClass uses subtypes but subtype-field {0} is neither configured as " +
					"Source Attribute nor Target Attribute. Can not check subtype-specific domains ({1})",
					subtypeFieldName, attributeDependency);
			}

			return configuredSubtypeField;
		}

		private static int GetSubtypeCode(IObjectClass objectClass, object subtypeValue)
		{
			foreach (Subtype subtype in DatasetUtils.GetSubtypes(objectClass))
			{
				if (AttributeDependencyUtils.Compare(subtypeValue, subtype.Code) == 0)
				{
					return subtype.Code;
				}
			}

			return -1;
		}

		private static void CheckFieldValuesAreValid(
			AttributeDependency attributeDependency, AttributeValueMapping mapping,
			IObjectClass objectClass, IList<IField> fields, IList<object> values,
			int subtypeCode = -1)
		{
			if (fields.Count != values.Count)
			{
				_msg.WarnFormat("Field.Count != Value.Count in {0} ({1})", mapping,
				                attributeDependency);
				return;
			}

			for (var i = 0; i < fields.Count; i++)
			{
				IField field = fields[i];
				object value = values[i];

				if (value == null || value is DBNull)
				{
					if (! field.IsNullable)
					{
						_msg.WarnFormat(
							"Field {0} is not nullable but value is <NULL> in {1} ({2})",
							field.Name, mapping, attributeDependency);
					}

					continue;
				}

				if (value == Wildcard.Value)
				{
					continue;
				}

				if (! field.CheckValue(value))
				{
					_msg.WarnFormat("Field {0} CheckValue failed for value {1} in {2} ({3})",
					                field.Name, value, mapping, attributeDependency);
					continue;
				}

				IDomain domain = subtypeCode == -1
					                 ? field.Domain
					                 : ((ISubtypes) objectClass).get_Domain(
						                 subtypeCode, field.Name);
				if (domain != null)
				{
					CheckValueIsValidForDomain(attributeDependency, mapping, field, domain, value);
				}
			}
		}

		private static void CheckValueIsValidForDomain(
			AttributeDependency attributeDependency, AttributeValueMapping mapping,
			IField field, IDomain domain, object value)
		{
			if (domain is ICodedValueDomain codedValueDomain)
			{
				var anyMatch = false;
				foreach (CodedValue codedValue in DomainUtils.GetCodedValueList(codedValueDomain))
				{
					if (AttributeDependencyUtils.Compare(codedValue.Value, value) == 0)
					{
						anyMatch = true;
						break;
					}
				}

				if (! anyMatch)
				{
					_msg.WarnFormat(
						"Coded Value Domain {0} defined for {1} does not contain value {2} in {3} ({4})",
						domain.Name, field.Name, value, mapping, attributeDependency);
				}

				return;
			}

			if (domain is IRangeDomain rangeDomain)
			{
				object minValue = rangeDomain.MinValue;
				object maxValue = rangeDomain.MaxValue;

				if (AttributeDependencyUtils.Compare(minValue, value) == 1 ||
				    AttributeDependencyUtils.Compare(maxValue, value) == -1)
				{
					_msg.WarnFormat(
						"Range Domain {0} (minValue={1}, maxValue={2}) defined for {3} does not contain value {4} in {5} ({6})",
						domain.Name, minValue, maxValue, field.Name, value, mapping,
						attributeDependency);
				}

				return;
			}

			_msg.WarnFormat("Domain {0} is neither Coded Value Domain nor Range Domain.",
			                domain.Name);
		}
	}
}
