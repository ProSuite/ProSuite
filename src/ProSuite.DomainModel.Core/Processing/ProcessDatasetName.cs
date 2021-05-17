using System.Globalization;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.Processing
{
	public class ProcessDatasetName
	{
		#region Constructors

		public ProcessDatasetName([NotNull] VectorDataset vectorDataset,
		                          [CanBeNull] string whereClause,
		                          [CanBeNull] string representationClassName = null)
		{
			Assert.ArgumentNotNull(vectorDataset, nameof(vectorDataset));

			VectorDataset = vectorDataset;
			WhereClause = whereClause ?? string.Empty;
			RepresentationClassName = representationClassName;
		}

		#endregion

		#region Properties

		[NotNull]
		public VectorDataset VectorDataset { get; }

		[NotNull]
		public string WhereClause { get; }

		[CanBeNull]
		public string RepresentationClassName { get; }

		#endregion

		/// <summary>
		/// Create a new <see cref="ProcessDatasetName"/> instance from
		/// <paramref name="spec"/> using the <paramref name="model"/>
		/// for resolving the dataset.
		/// <para/>
		/// Format of <paramref name="spec"/>:
		/// DatasetName [ ':' RepClassName ] [ ';' WhereClause ]
		/// <para/>
		/// The dataset name is resolved to a dataset
		/// using <see cref="GetDataset(DdxModel, string)"/>.
		/// </summary>
		/// <returns>
		/// A new <see cref="ProcessDatasetName"/> instance,
		/// or null if there is an error creating it.
		/// </returns>
		[CanBeNull]
		public static ProcessDatasetName TryCreate([NotNull] DdxModel model,
		                                           [NotNull] string spec, out string message)
		{
			Assert.ArgumentNotNull(model, nameof(model));
			Assert.ArgumentNotNullOrEmpty(spec, nameof(spec));

			int whereIndex = spec.IndexOf(';');

			string datasetSpec = whereIndex < 0
				                     ? spec.Trim()
				                     : spec.Substring(0, whereIndex).Trim();

			string whereClause = whereIndex < 0
				                     ? null
				                     : spec.Substring(whereIndex + 1).Trim();

			return TryCreate(model, datasetSpec, whereClause, out message);
		}

		/// <summary>
		/// Create a new <see cref="ProcessDatasetName"/> instance from
		/// <paramref name="datasetSpec"/> and <paramref name="whereClause"/>
		/// using <paramref name="model"/> for resolving the dataset.
		/// <para/>
		/// Format of <paramref name="datasetSpec"/>:
		/// DatasetName [ ':' RepClassName ]
		/// <para/>
		/// The dataset name is resolved to a dataset
		/// using <see cref="GetDataset(DdxModel, string)"/>.
		/// </summary>
		/// <returns>
		/// A new <see cref="ProcessDatasetName"/> instance,
		/// or null if there is an error creating it.
		/// </returns>
		[CanBeNull]
		public static ProcessDatasetName TryCreate([NotNull] DdxModel model,
		                                           [NotNull] string datasetSpec,
		                                           [CanBeNull] string whereClause,
		                                           out string message)
		{
			Assert.ArgumentNotNull(model, nameof(model));
			Assert.ArgumentNotNullOrEmpty(datasetSpec, nameof(datasetSpec));

			int colonIndex = datasetSpec.IndexOf(':');

			string datasetName = colonIndex < 0
				                     ? datasetSpec.Trim()
				                     : datasetSpec.Substring(0, colonIndex).Trim();

			string repClassName = colonIndex < 0
				                      ? null
				                      : datasetSpec.Substring(colonIndex + 1).Trim();

			return TryCreate(model, datasetName, repClassName, whereClause, out message);
		}

		/// <summary>
		/// Create a new <see cref="ProcessDatasetName"/> instance.
		/// <para/>
		/// The dataset name is resolved to a dataset
		/// using <see cref="GetDataset(DdxModel, string)"/>.
		/// </summary>
		/// <returns>
		/// A new <see cref="ProcessDatasetName"/> instance,
		/// or null if there is an error creating it.
		/// </returns>
		[CanBeNull]
		public static ProcessDatasetName TryCreate([NotNull] DdxModel model, string datasetName,
		                                           string repClassName, string whereClause,
		                                           out string message)
		{
			Dataset dataset = GetDataset(model, datasetName);
			if (dataset == null)
			{
				message = $"Dataset '{datasetName}' not found in model '{model.Name}'";
				return null;
			}

			var vectorDataset = dataset as VectorDataset;
			if (vectorDataset == null)
			{
				message =
					$"Dataset '{datasetName}' in model '{model.Name}' is not a vector dataset";
				return null;
			}

			message = string.Empty;
			return new ProcessDatasetName(vectorDataset, whereClause, repClassName);
		}

		/// <summary>
		/// Look up <paramref name="datasetName"/> in <paramref name="model"/>
		/// using <see cref="DdxModel.GetDatasetByModelName(string, bool)"/>, and if that fails,
		/// compare the name against all the model's datasets, ignoring case
		/// and qualifiers (if any).
		/// </summary>
		/// <returns>
		/// The <see cref="Dataset"/> if found, or null if not found.
		/// </returns>
		[CanBeNull]
		private static Dataset GetDataset([NotNull] DdxModel model, string datasetName)
		{
			Dataset dataset = model.GetDatasetByModelName(datasetName);
			if (dataset != null)
			{
				return dataset;
			}

			// Search all datasets, ignoring qualification

			foreach (Dataset candidate in model.GetDatasets())
			{
				if (CompareUnqualifiedName(datasetName, candidate.Name) == 0)
				{
					return candidate;
				}
			}

			return null; // not found
		}

		private static int CompareUnqualifiedName(string s, string t)
		{
			// Notice: slightly optimized for performance
			// Note: this might go to ModelElementUtils?

			const char separator = '.';

			int si = s.IndexOf(separator);
			si = si < 0 ? 0 : si + 1;
			int slen = s.Length - si;

			int ti = t.IndexOf(separator);
			ti = ti < 0 ? 0 : ti + 1;
			int tlen = t.Length - ti;

			CompareInfo compareInfo = CultureInfo.InvariantCulture.CompareInfo;
			return compareInfo.Compare(s, si, slen, t, ti, tlen,
			                           CompareOptions.OrdinalIgnoreCase);
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.Append(VectorDataset.Name);

			if (! string.IsNullOrEmpty(RepresentationClassName))
			{
				sb.Append(":").Append(RepresentationClassName);
			}

			if (! string.IsNullOrEmpty(WhereClause))
			{
				sb.Append("; ").Append(WhereClause);
			}

			return sb.ToString();
		}
	}
}
