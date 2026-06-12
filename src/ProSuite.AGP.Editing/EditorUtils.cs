using System;
using System.Collections.Generic;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing;

/// <summary>
/// Helpers to read an editing template's configured default attribute values from
/// its CIM definition (<see cref="CIMBasicRowTemplate.DefaultValues"/>) rather than
/// from <see cref="EditingTemplate.Inspector"/>.
/// <para>
/// The <see cref="EditingTemplate.Inspector"/> is null until the Create Features pane
/// has been opened at least once (a regression observed since Pro 3.7), whereas the
/// CIM definition always carries the template's configured defaults (including the
/// subtype code). For reading template defaults this is equivalent and more robust.
/// </para>
/// <para>
/// All methods access <see cref="EditingTemplate.GetDefinition"/> and therefore must
/// be called on the MCT (e.g. inside QueuedTask.Run).
/// </para>
/// </summary>
public static class EditorUtils
{
	/// <summary>
	/// Tries to get the template's configured default value for the given field.
	/// Returns false (with <paramref name="value"/> set to null) if the template has
	/// no row definition, no such field, or the default value is null or DBNull.
	/// Must be called on the MCT.
	/// </summary>
	public static bool TryGetDefaultValue([CanBeNull] EditingTemplate template,
	                                      [NotNull] string fieldName,
	                                      out object value)
	{
		value = null;

		if (template?.GetDefinition() is not CIMRowTemplate rowTemplate)
		{
			return false;
		}

		return TryGetTemplateDefaultValue(rowTemplate, fieldName, out value) &&
		       value != null && value != DBNull.Value;
	}

	/// <summary>
	/// Gets the subtype code configured as the template's default value, or null if
	/// the template's layer has no subtype field or the template carries no default.
	/// Must be called on the MCT (QueuedTask).
	/// </summary>
	public static int? GetTemplateSubtypeCode(EditingTemplate editTemplate)
	{
		if (editTemplate?.Layer is not FeatureLayer featureLayer)
		{
			return null;
		}

		using FeatureClass featureClass = featureLayer.GetFeatureClass();
		using FeatureClassDefinition classDefinition = featureClass?.GetDefinition();

		string subtypeField = classDefinition?.GetSubtypeField();

		return GetSubtypeCode(editTemplate, subtypeField);
	}

	/// <summary>
	/// Gets the subtype code configured as the template's default for the given
	/// subtype field, or null if there is no subtype field or no default value.
	/// Must be called on the MCT.
	/// </summary>
	public static int? GetSubtypeCode([CanBeNull] EditingTemplate template,
	                                  [CanBeNull] string subtypeField)
	{
		if (string.IsNullOrEmpty(subtypeField))
		{
			return null;
		}

		if (! TryGetDefaultValue(template, subtypeField, out object value))
		{
			return null;
		}

		// NOTE: Subtypes can be based on short integers
		return Convert.ToInt32(value);
	}

	/// <summary>
	/// Looks up a field's configured default value in the template's CIM definition.
	/// Returns true if the field is present (the value may be null), false otherwise.
	/// Field names are matched case-insensitively.
	/// </summary>
	public static bool TryGetTemplateDefaultValue([NotNull] CIMRowTemplate rowTemplate,
	                                              [NotNull] string fieldName,
	                                              out object value)
	{
		value = null;

		IDictionary<string, object> defaultValues = rowTemplate.DefaultValues;
		if (defaultValues == null)
		{
			return false;
		}

		foreach (KeyValuePair<string, object> pair in defaultValues)
		{
			if (string.Equals(pair.Key, fieldName, StringComparison.OrdinalIgnoreCase))
			{
				value = pair.Value;
				return true;
			}
		}

		return false;
	}
}
