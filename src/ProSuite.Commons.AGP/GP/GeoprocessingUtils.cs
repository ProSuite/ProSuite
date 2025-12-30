using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Core.Geoprocessing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AGP.GP;

public static class GeoprocessingUtils
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public static async Task<bool> AddFieldAsync([NotNull] string table,
	                                             [NotNull] string fieldName,
	                                             [CanBeNull] string alias,
	                                             FieldType type,
	                                             int? precision = null,
	                                             int? scale = null,
	                                             int? length = null,
	                                             bool isNullable = true,
	                                             bool isRequired = false,
	                                             string domain = null)
	{
		Assert.ArgumentNotNullOrEmpty(table, nameof(table));
		Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

		// Note daro: Named parameters don't seem to work. But maybe further
		//			  testing is required.

		IReadOnlyList<string> parameters = Geoprocessing.MakeValueArray(
			$"{table}", $"{fieldName}", $"{Parse(type)}", $"{precision}",
			$"{scale}", $"{length}", $"{alias}", $"{isNullable}",
			$"{isRequired}", domain);

		return await ExecuteAsync("management.AddField", parameters);
	}

	public static async Task<bool> DeleteFieldAsync([NotNull] string table,
	                                                [NotNull] params object[] fieldNames)
	{
		Assert.ArgumentNotNullOrEmpty(table, nameof(table));
		Assert.ArgumentNotNull(fieldNames, nameof(fieldNames));
		Assert.ArgumentCondition(fieldNames.Length > 0, "no field names");

		IReadOnlyList<string> parameters =
			Geoprocessing.MakeValueArray(table, StringUtils.Concatenate(fieldNames, ","));

		return await ExecuteAsync("management.DeleteField", parameters);
	}

	public static async Task<bool> CreateDomainAsync([NotNull] string workspace,
	                                                 [NotNull] string domainName,
	                                                 [CanBeNull] string description = null,
	                                                 FieldType type = FieldType.Integer,
	                                                 DomainType domainType = DomainType.Coded)
	{
		Assert.ArgumentNotNullOrEmpty(workspace, nameof(workspace));
		Assert.ArgumentNotNullOrEmpty(domainName, nameof(domainName));

		const string tool = "management.CreateDomain";

		string fieldType;

		switch (type)
		{
			case FieldType.SmallInteger:
				fieldType = "SHORT";
				break;
			case FieldType.Integer:
				fieldType = "LONG";
				break;
			case FieldType.Single:
				fieldType = "FLOAT";
				break;
			case FieldType.Double:
				fieldType = "DOUBLE";
				break;
			case FieldType.String:
				fieldType = "TEXT";
				break;
			case FieldType.Date:
				fieldType = "DATE";
				break;
			default:
				throw new ArgumentException($"Invalid field type '{type}' for tool {tool}",
				                            nameof(type));
		}

		IReadOnlyList<string> parameters =
			Geoprocessing.MakeValueArray(workspace, domainName, description, fieldType,
			                             $"{domainType}");

		return await ExecuteAsync(tool, parameters);
	}

	public static async Task<bool> AddCodedValueToDomainAsync(
		[NotNull] string workspace,
		[NotNull] string domainName,
		int code,
		[NotNull] string description)
	{
		Assert.ArgumentNotNullOrEmpty(workspace, nameof(workspace));
		Assert.ArgumentNotNullOrEmpty(domainName, nameof(domainName));
		Assert.ArgumentNotNullOrEmpty(description, nameof(description));

		IReadOnlyList<string> parameters =
			Geoprocessing.MakeValueArray(workspace, domainName, code, description);

		return await ExecuteAsync("management.AddCodedValueToDomain", parameters);
	}

	public static async Task<bool> AssignDefaultToFieldAsync([NotNull] string table,
	                                                         [NotNull] string fieldName,
	                                                         [CanBeNull] object defaultValue =
		                                                         null)
	{
		Assert.ArgumentNotNullOrEmpty(table, nameof(table));
		Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

		IReadOnlyList<string> parameters =
			Geoprocessing.MakeValueArray(table, fieldName, defaultValue);

		return await ExecuteAsync("management.AssignDefaultToField", parameters);
	}

	private static async Task<bool> ExecuteAsync([NotNull] string tool,
	                                             [NotNull] IReadOnlyList<string> parameters)
	{
		Assert.ArgumentNotNullOrEmpty(tool, nameof(tool));
		Assert.ArgumentNotNull(parameters, nameof(parameters));
		Assert.ArgumentCondition(parameters.Count > 0, "no parameter");

		_msg.VerboseDebug(
			() => $"{tool}, Parameters: {StringUtils.Concatenate(parameters, ", ")}");

		IReadOnlyList<KeyValuePair<string, string>> environments =
			Geoprocessing.MakeEnvironmentArray(overwriteoutput: true);

		IGPResult result =
			await Geoprocessing.ExecuteToolAsync(tool, parameters, environments, null, null,
			                                     GPExecuteToolFlags.None);

		if (! result.IsFailed)
		{
			return true;
		}

		_msg.Info(
			$"{tool} has failed: {StringUtils.Concatenate(Format(result.Messages), ", ")}, Parameters: {StringUtils.Concatenate(parameters, ", ")}");
		return false;
	}

	private static IEnumerable<string> Format([NotNull] IEnumerable<IGPMessage> messages)
	{
		var msgs = new List<string>(
			messages.Select(message => $"{message.Type} ({message.ErrorCode}) {message.Text}"));

		if (msgs.Count == 0)
		{
			msgs.Add("No GP messages");
		}

		return msgs;
	}

	private static string Parse(FieldType type)
	{
		switch (type)
		{
			case FieldType.SmallInteger:
				return "SHORT";
			case FieldType.Integer:
				return "LONG";
			case FieldType.Single:
				return "FLOAT";
			case FieldType.Double:
				return "DOUBLE";
			case FieldType.String:
				return "TEXT";
			case FieldType.Date:
				return "DATE";
			case FieldType.Blob:
				return "BLOB";
			case FieldType.Raster:
				return "RASTER";
			case FieldType.GUID:
				return "GUID";
			case FieldType.GlobalID:
			case FieldType.OID:
			case FieldType.Geometry:
			case FieldType.XML:
				throw new ArgumentException($"Invalid field type '{type}'", nameof(type));
			default:
				throw new ArgumentOutOfRangeException(nameof(type), type, null);
		}
	}
}

public enum DomainType
{
	Coded,
	Range
}
