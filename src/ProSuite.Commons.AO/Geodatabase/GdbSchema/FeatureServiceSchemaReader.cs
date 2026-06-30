using System;
using System.Net.Http;
using System.Text.Json;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	/// <summary>
	/// Reads the schema of an ArcGIS REST Feature Service and builds a
	/// <see cref="GdbWorkspace"/> of type <see cref="ProSuite.Commons.GeoDb.WorkspaceDbType.FeatureService"/>
	/// from it. This is primarily intended for testing and learning purposes; in production a
	/// service workspace is typically created from a <c>WorkspaceMsg</c> sent by the client
	/// (which in turn is created from a Pro SDK geodatabase).
	/// </summary>
	public class FeatureServiceSchemaReader
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private static readonly HttpClient _defaultHttpClient = new HttpClient();

		private readonly HttpClient _httpClient;

		/// <summary>
		/// Initializes a new instance of the <see cref="FeatureServiceSchemaReader"/> class.
		/// </summary>
		/// <param name="httpClient">The http client to use. If null, a shared default client
		/// is used.</param>
		public FeatureServiceSchemaReader([CanBeNull] HttpClient httpClient = null)
		{
			_httpClient = httpClient ?? _defaultHttpClient;
		}

		/// <summary>
		/// Reads the schema of the feature service at the given URL and returns a workspace
		/// representing it.
		/// </summary>
		/// <param name="featureServiceUrl">The URL of the feature service, e.g.
		/// https://sampleserver6.arcgisonline.com/arcgis/rest/services/Wildfire/FeatureServer
		/// </param>
		/// <param name="workspaceHandle">Optional handle. If null, a stable handle is derived
		/// from the service URL.</param>
		[NotNull]
		public GdbWorkspace ReadWorkspace([NotNull] string featureServiceUrl,
		                                  long? workspaceHandle = null)
		{
			if (string.IsNullOrEmpty(featureServiceUrl))
			{
				throw new ArgumentNullException(nameof(featureServiceUrl));
			}

			string serviceUrl = featureServiceUrl.TrimEnd('/');

			var container = new GdbTableContainer();

			GdbWorkspace workspace =
				GdbWorkspace.CreateForFeatureService(serviceUrl, container, workspaceHandle);

			using (JsonDocument serviceDoc = GetJson(serviceUrl))
			{
				JsonElement root = serviceDoc.RootElement;

				AddLayers(root, "layers", serviceUrl, container, workspace);
				AddLayers(root, "tables", serviceUrl, container, workspace);
			}

			return workspace;
		}

		private void AddLayers([NotNull] JsonElement serviceRoot,
		                       [NotNull] string arrayPropertyName,
		                       [NotNull] string serviceUrl,
		                       [NotNull] GdbTableContainer container,
		                       [NotNull] GdbWorkspace workspace)
		{
			if (! serviceRoot.TryGetProperty(arrayPropertyName, out JsonElement layers) ||
			    layers.ValueKind != JsonValueKind.Array)
			{
				return;
			}

			foreach (JsonElement layer in layers.EnumerateArray())
			{
				if (! layer.TryGetProperty("id", out JsonElement idElement))
				{
					continue;
				}

				int layerId = idElement.GetInt32();

				string layerUrl = $"{serviceUrl}/{layerId}";

				using (JsonDocument layerDoc = GetJson(layerUrl))
				{
					GdbTable table = CreateTable(layerId, layerDoc.RootElement, workspace);

					container.TryAdd(table);
				}
			}
		}

		[NotNull]
		private static GdbTable CreateTable(int layerId,
		                                    JsonElement layerJson,
		                                    [NotNull] GdbWorkspace workspace)
		{
			string displayName = GetString(layerJson, "name") ?? $"Layer_{layerId}";

			// Reproduce the table name that the ArcGIS Pro SDK assigns to a feature-service
			// layer, so that this (REST-based) path behaves identically to the production path
			// (which receives names from a Pro SDK geodatabase). E.g. layer 0
			// "Wildfire Response Points" -> name "L0Wildfire_Response_Points",
			// alias "Wildfire Response Points".
			string name = FeatureServiceTableName(layerId, displayName);
			string alias = displayName;

			esriGeometryType geometryType = GetGeometryType(layerJson);

			GdbTable result;

			if (geometryType == esriGeometryType.esriGeometryNull)
			{
				result = new GdbTable(layerId, name, alias, null, workspace);
			}
			else
			{
				ISpatialReference spatialReference = GetSpatialReference(layerJson);

				result = new GdbFeatureClass(layerId, name, geometryType, alias, null, workspace)
				         {
					         SpatialReference = spatialReference
				         };
			}

			AddFields(layerJson, result, geometryType);

			return result;
		}

		/// <summary>
		/// Builds the geodatabase table name the ArcGIS Pro SDK uses for a feature-service
		/// layer: "L" + layer id + the layer name with spaces replaced by underscores.
		/// </summary>
		/// <remarks>
		/// System limitation: this is a best-effort reproduction of the Pro SDK naming
		/// (verified against the sample services for spaces -> underscores). Other character
		/// sanitizations the Pro SDK might perform are not reproduced here.
		/// </remarks>
		[NotNull]
		private static string FeatureServiceTableName(int layerId, [NotNull] string displayName)
		{
			return $"L{layerId}{displayName.Replace(' ', '_')}";
		}

		private static void AddFields([NotNull] JsonElement layerJson,
		                              [NotNull] GdbTable table,
		                              esriGeometryType geometryType)
		{
			bool isFeatureClass = geometryType != esriGeometryType.esriGeometryNull;
			bool shapeFieldAdded = false;

			if (layerJson.TryGetProperty("fields", out JsonElement fields) &&
			    fields.ValueKind == JsonValueKind.Array)
			{
				foreach (JsonElement fieldJson in fields.EnumerateArray())
				{
					string fieldName = GetString(fieldJson, "name");

					if (string.IsNullOrEmpty(fieldName))
					{
						continue;
					}

					esriFieldType fieldType = GetFieldType(fieldJson);

					IField field;

					if (fieldType == esriFieldType.esriFieldTypeGeometry)
					{
						field = FieldUtils.CreateShapeField(
							fieldName, geometryType, GetSpatialReference(layerJson));
						shapeFieldAdded = true;
					}
					else
					{
						field = FieldUtils.CreateField(
							fieldName, fieldType, GetString(fieldJson, "alias"));

						if (fieldType == esriFieldType.esriFieldTypeString &&
						    fieldJson.TryGetProperty("length", out JsonElement length) &&
						    length.ValueKind == JsonValueKind.Number)
						{
							((IFieldEdit) field).Length_2 = length.GetInt32();
						}
					}

					if (table.Fields.FindField(field.Name) < 0)
					{
						table.AddField(field);
					}
				}
			}

			// The service may not list the geometry field explicitly: make sure a feature
			// class has a shape field (it is relevant for Z/M awareness etc.).
			if (isFeatureClass && ! shapeFieldAdded)
			{
				IField shapeField = FieldUtils.CreateShapeField(
					geometryType, GetSpatialReference(layerJson));

				if (table.Fields.FindField(shapeField.Name) < 0)
				{
					table.AddField(shapeField);
				}
			}
		}

		private static esriGeometryType GetGeometryType([NotNull] JsonElement layerJson)
		{
			// Tables have type "Table" and no geometryType; feature layers have a geometryType.
			string geometryTypeString = GetString(layerJson, "geometryType");

			if (string.IsNullOrEmpty(geometryTypeString))
			{
				return esriGeometryType.esriGeometryNull;
			}

			if (Enum.TryParse(geometryTypeString, out esriGeometryType geometryType))
			{
				return geometryType;
			}

			_msg.DebugFormat("Unknown geometry type '{0}', treating layer as a table.",
			                 geometryTypeString);

			return esriGeometryType.esriGeometryNull;
		}

		private static esriFieldType GetFieldType([NotNull] JsonElement fieldJson)
		{
			string typeString = GetString(fieldJson, "type");

			if (! string.IsNullOrEmpty(typeString) &&
			    Enum.TryParse(typeString, out esriFieldType fieldType))
			{
				return fieldType;
			}

			_msg.DebugFormat("Unknown field type '{0}', using esriFieldTypeString.", typeString);

			return esriFieldType.esriFieldTypeString;
		}

		[NotNull]
		private static ISpatialReference GetSpatialReference([NotNull] JsonElement layerJson)
		{
			int wkid = 4326; // WGS84 fallback

			if (layerJson.TryGetProperty("extent", out JsonElement extent) &&
			    extent.ValueKind == JsonValueKind.Object &&
			    extent.TryGetProperty("spatialReference", out JsonElement sr) &&
			    sr.ValueKind == JsonValueKind.Object)
			{
				if (sr.TryGetProperty("latestWkid", out JsonElement latestWkid) &&
				    latestWkid.ValueKind == JsonValueKind.Number)
				{
					wkid = latestWkid.GetInt32();
				}
				else if (sr.TryGetProperty("wkid", out JsonElement wkidElement) &&
				         wkidElement.ValueKind == JsonValueKind.Number)
				{
					wkid = wkidElement.GetInt32();
				}
			}

			return SpatialReferenceUtils.CreateSpatialReference(wkid);
		}

		[CanBeNull]
		private static string GetString([NotNull] JsonElement element,
		                                [NotNull] string propertyName)
		{
			if (element.TryGetProperty(propertyName, out JsonElement value) &&
			    value.ValueKind == JsonValueKind.String)
			{
				return value.GetString();
			}

			return null;
		}

		[NotNull]
		private JsonDocument GetJson([NotNull] string url)
		{
			string requestUrl = url.Contains("?") ? $"{url}&f=json" : $"{url}?f=json";

			_msg.DebugFormat("Requesting {0}", requestUrl);

			string json = _httpClient.GetStringAsync(requestUrl).GetAwaiter().GetResult();

			JsonDocument document = JsonDocument.Parse(json);

			if (document.RootElement.TryGetProperty("error", out JsonElement error))
			{
				string message = GetString(error, "message") ?? "Unknown error";
				throw new InvalidOperationException(
					$"The feature service request to {requestUrl} failed: {message}");
			}

			return document;
		}
	}
}
