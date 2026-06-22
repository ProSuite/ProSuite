using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	/// <summary>
	/// Helper methods for associating a data model with one or more (ArcGIS REST) feature
	/// services and for matching feature-service dataset names against model element names.
	/// </summary>
	/// <remarks>
	/// This is a bridge solution until the data dictionary schema has a dedicated field for the
	/// service URL(s) of a model. For now the URLs are stored as tagged lines in the model's
	/// <see cref="DdxModel.Description"/>, e.g.
	/// <code>#prosuite:serviceUrl=https://host/arcgis/rest/services/Foo/FeatureServer</code>
	/// One marker line per URL; a model may declare several.
	/// </remarks>
	public static class ModelServiceUtils
	{
		/// <summary>
		/// The marker that prefixes a service URL in the model description.
		/// </summary>
		public const string ServiceUrlMarker = "#prosuite:serviceUrl=";

		// The ArcGIS Pro SDK exposes a feature-service layer as a geodatabase table whose name
		// is "L" + the layer id + the (sanitized) layer name, e.g. layer 0
		// "Wildfire Response Points" -> "L0Wildfire_Response_Points". The matching logic strips
		// this prefix to recover the model element name.
		private static readonly Regex _layerPrefixRegex =
			new Regex(@"^L\d+", RegexOptions.Compiled);

		/// <summary>
		/// Gets the feature-service URLs declared for the given model (in its description).
		/// </summary>
		[NotNull]
		public static IEnumerable<string> GetServiceUrls([CanBeNull] DdxModel model)
		{
			return GetServiceUrlsFromDescription(model?.Description);
		}

		/// <summary>
		/// Parses the feature-service URLs from a model description text.
		/// </summary>
		[NotNull]
		public static IEnumerable<string> GetServiceUrlsFromDescription(
			[CanBeNull] string description)
		{
			if (string.IsNullOrEmpty(description))
			{
				yield break;
			}

			foreach (string rawLine in description.Split('\n'))
			{
				string line = rawLine.Trim();

				int markerIndex =
					line.IndexOf(ServiceUrlMarker, StringComparison.OrdinalIgnoreCase);

				if (markerIndex < 0)
				{
					continue;
				}

				string rest = line.Substring(markerIndex + ServiceUrlMarker.Length).Trim();

				// A URL contains no whitespace; ignore any trailing text on the same line.
				string url = rest.Split(new[] { ' ', '\t' }, 2)[0];

				if (! string.IsNullOrEmpty(url))
				{
					yield return url;
				}
			}
		}

		/// <summary>
		/// Returns true if the given model declares at least one feature-service URL.
		/// </summary>
		public static bool HasServiceUrls([CanBeNull] DdxModel model)
		{
			foreach (string _ in GetServiceUrls(model))
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Normalizes a service URL for comparison (trims surrounding whitespace and a trailing
		/// slash). Must be kept consistent with the URL stored in a service GdbWorkspace.
		/// </summary>
		[CanBeNull]
		public static string NormalizeServiceUrl([CanBeNull] string url)
		{
			return url?.Trim().TrimEnd('/');
		}

		/// <summary>
		/// Determines whether two service URLs refer to the same service (case-insensitive,
		/// ignoring a trailing slash).
		/// </summary>
		public static bool ServiceUrlEquals([CanBeNull] string url1, [CanBeNull] string url2)
		{
			return string.Equals(NormalizeServiceUrl(url1), NormalizeServiceUrl(url2),
			                     StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Strips the "L{layerId}" prefix that the ArcGIS Pro SDK adds to feature-service table
		/// names, so the remaining name can be matched against a model element name.
		/// </summary>
		/// <remarks>
		/// System limitation: it is assumed that the prefix-stripped Pro SDK table name matches
		/// the underlying database table name (which is also assumed to be the basis of the
		/// service layer name). The service layer's <i>alias</i> (display name) is NOT used for
		/// matching, as it does not correspond to the table name.
		/// </remarks>
		[NotNull]
		public static string StripFeatureServiceLayerPrefix([NotNull] string gdbDatasetName)
		{
			if (gdbDatasetName == null)
			{
				throw new ArgumentNullException(nameof(gdbDatasetName));
			}

			return _layerPrefixRegex.Replace(gdbDatasetName, string.Empty);
		}

		/// <summary>
		/// Resolves the model element name to look up for a feature-service table when the service
		/// is the model's master database (i.e. its URL is declared on the model).
		/// </summary>
		/// <remarks>
		/// This mirrors master-database matching semantics, not child-database semantics: the
		/// (prefix-stripped) table name is used as-is for unqualified models, and no child-database
		/// name transformer is applied. Feature-service table names are always unqualified, so for a
		/// model that was harvested with <em>qualified</em> element names the stripped name is
		/// qualified using the model's master database schema owner (and database name, if any).
		/// </remarks>
		/// <returns>
		/// The model element name, or <c>null</c> if it cannot be determined (a qualified-name model
		/// without a unique master database schema owner).
		/// </returns>
		[CanBeNull]
		public static string GetMasterDatabaseModelElementName(
			[NotNull] DdxModel model,
			[NotNull] string gdbDatasetName)
		{
			Assert.ArgumentNotNull(model, nameof(model));
			Assert.ArgumentNotNullOrEmpty(gdbDatasetName, nameof(gdbDatasetName));

			string strippedName = StripFeatureServiceLayerPrefix(gdbDatasetName);

			if (! model.ElementNamesAreQualified)
			{
				// Master database semantics: use the unqualified name, no name transformer.
				return ModelElementNameUtils.GetUnqualifiedName(strippedName);
			}

			// The model uses qualified element names, but feature-service table names are unqualified.

			if (ModelElementNameUtils.IsQualifiedName(strippedName))
			{
				// Already qualified (unusual for a feature service): use as-is.
				return strippedName;
			}

			if (string.IsNullOrEmpty(model.DefaultDatabaseSchemaOwner))
			{
				// No unique master database schema owner to qualify the name with: give up.
				return null;
			}

			return ModelElementNameUtils.GetQualifiedName(
				model.DefaultDatabaseName, model.DefaultDatabaseSchemaOwner, strippedName);
		}
	}
}
