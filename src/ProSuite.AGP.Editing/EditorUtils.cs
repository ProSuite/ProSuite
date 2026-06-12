using System;
using System.Collections.Generic;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Editing.Templates;
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

		IDictionary<string, object> defaultValues = rowTemplate.DefaultValues;
		if (defaultValues == null)
		{
			return false;
		}

		foreach (KeyValuePair<string, object> pair in defaultValues)
		{
			// NOTE: Field names are matched case-insensitively
			if (string.Equals(pair.Key, fieldName, StringComparison.OrdinalIgnoreCase))
			{
				value = pair.Value;
				return value != null && value != DBNull.Value;
			}
		}

		return false;
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
}
