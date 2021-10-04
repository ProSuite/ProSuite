using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Internal.CIM;
using ArcGIS.Desktop.Core.Geoprocessing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AGP.GP
{
	public static class GeoprocessingUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static async Task<bool> AddFieldAsync([NotNull] string table,
		                                             [NotNull] string fieldName,
		                                             [CanBeNull] string alias,
		                                             esriFieldType type,
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
		                                                 esriFieldType type =
			                                                 esriFieldType.esriFieldTypeInteger,
		                                                 DomainType domainType = DomainType.Coded)
		{
			Assert.ArgumentNotNullOrEmpty(workspace, nameof(workspace));
			Assert.ArgumentNotNullOrEmpty(domainName, nameof(domainName));

			const string tool = "management.CreateDomain";

			string fieldType;

			switch (type)
			{
				case esriFieldType.esriFieldTypeSmallInteger:
					fieldType = "SHORT";
					break;
				case esriFieldType.esriFieldTypeInteger:
					fieldType = "LONG";
					break;
				case esriFieldType.esriFieldTypeSingle:
					fieldType = "FLOAT";
					break;
				case esriFieldType.esriFieldTypeDouble:
					fieldType = "DOUBLE";
					break;
				case esriFieldType.esriFieldTypeString:
					fieldType = "TEXT";
					break;
				case esriFieldType.esriFieldTypeDate:
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

			_msg.VerboseDebug(() => $"{tool}, Parameters: {StringUtils.Concatenate(parameters, ", ")}");

			IReadOnlyList<KeyValuePair<string, string>> environments =
				Geoprocessing.MakeEnvironmentArray(overwriteoutput: true);

			IGPResult result = await Geoprocessing.ExecuteToolAsync(tool, parameters, environments);

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

		private static string Parse(esriFieldType type)
		{
			switch (type)
			{
				case esriFieldType.esriFieldTypeSmallInteger:
					return "SHORT";
				case esriFieldType.esriFieldTypeInteger:
					return "LONG";
				case esriFieldType.esriFieldTypeSingle:
					return "FLOAT";
				case esriFieldType.esriFieldTypeDouble:
					return "DOUBLE";
				case esriFieldType.esriFieldTypeString:
					return "TEXT";
				case esriFieldType.esriFieldTypeDate:
					return "DATE";
				case esriFieldType.esriFieldTypeBlob:
					return "BLOB";
				case esriFieldType.esriFieldTypeRaster:
					return "RASTER";
				case esriFieldType.esriFieldTypeGUID:
					return "GUID";
				case esriFieldType.esriFieldTypeGlobalID:
				case esriFieldType.esriFieldTypeOID:
				case esriFieldType.esriFieldTypeGeometry:
				case esriFieldType.esriFieldTypeXML:
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
}
