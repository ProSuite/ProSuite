using System;

namespace ProSuite.Commons.AGP.Core.Geodatabase;

/// <summary>
/// Operations on database table names
/// </summary>
/// <remarks>Do NOT merge with ModelElementNameUtils (they are for
/// model element names, not for database table names). Beware that
/// the code here does not work properly with "delimited identifiers"
/// (like "OWNER"."NAME") of SQL.</remarks>
public static class DatasetNameUtils
{
	private const char QualifierSeparator = '.'; // for all supported DBMS

	public static bool HasDatasetPrefix(string datasetName, string prefix)
	{
		if (prefix is null) return false;
		ParseDatasetName(datasetName, out string tableName, out _);
		if (tableName is null) return false;
		return tableName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
	}

	public static string ChangeDatasetPrefix(string datasetName, string fromPrefix, string toPrefix)
	{
		ParseDatasetName(datasetName, out string tableName, out string qualifier);

		if (tableName is null) return null;

		if (tableName.StartsWith(fromPrefix, StringComparison.OrdinalIgnoreCase))
		{
			var prefixLen = fromPrefix.Length;
			tableName = string.Concat(toPrefix, tableName.Substring(prefixLen));
		}
		else
		{
			throw new InvalidOperationException(
				$"Dataset name '{datasetName}' does not have the '{fromPrefix}' prefix");
		}

		return QualifyDatasetName(tableName, qualifier);
	}

	public static void ParseDatasetName(
		string fullName, out string tableName, out string qualifier)
	{
		// Syntax: [[DATABASE.]SCHEMA.]TABLE_NAME where [DATABASE.]SCHEMA is the qualifier!
		// Example: DATABASE.SCHEMA.KRM25_TABLE or SCHEMA.KRM25_TABLE or KRM25_TABLE
		// Beware: Full SQL allows "delimited" identifiers, which we are ignorant of!
		// Note: only if need be: Parse(fullName, out tableName, out ownerName, out dbName)

		if (fullName is null)
		{
			tableName = null;
			qualifier = null;
			return;
		}

		int index = fullName.LastIndexOf(QualifierSeparator);

		if (index < 0)
		{
			tableName = fullName.Trim();
			qualifier = null;
		}
		else if (index == 0)
		{
			// The invalid ".NAME" case but do the best:
			tableName = fullName.Substring(1).Trim();
			qualifier = null;
		}
		else
		{
			tableName = fullName.Substring(index + 1).Trim();
			qualifier = fullName.Substring(0, index).Trim();
		}
	}

	public static string QualifyDatasetName(string datasetName, string qualifier = null)
	{
		// Note: only if need be: Parse(fullName, out tableName, out ownerName, out dbName)

		ParseDatasetName(datasetName, out var tableName, out _);

		if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(qualifier))
		{
			return tableName;
		}

		qualifier = qualifier.Trim();

		if (qualifier.EndsWith(QualifierSeparator))
		{
			qualifier = qualifier.Substring(0, qualifier.Length - 1).Trim();
		}

		return qualifier.EndsWith(QualifierSeparator)
			       ? string.Concat(qualifier, tableName)
			       : string.Concat(qualifier, QualifierSeparator, tableName);
	}

	public static string UnqualifyDatasetName(string datasetName)
	{
		return QualifyDatasetName(datasetName);
	}

	public static string GetQualifier(string datasetName)
	{
		ParseDatasetName(datasetName, out _, out string qualifier);
		return qualifier;
	}
}
