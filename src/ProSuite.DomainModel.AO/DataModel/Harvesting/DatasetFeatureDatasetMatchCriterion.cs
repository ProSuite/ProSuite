using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.DataModel.Harvesting
{
	public class DatasetFeatureDatasetMatchCriterion : DatasetMatchCriterionBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DatasetFeatureDatasetMatchCriterion"/> class.
		/// </summary>
		/// <param name="featureDatasetNamePatterns">The feature dataset name patterns.</param>
		public DatasetFeatureDatasetMatchCriterion(
			[NotNull] IEnumerable<string> featureDatasetNamePatterns)
			: base(featureDatasetNamePatterns) { }

		public override bool IsSatisfied(IDatasetName datasetName, out string reason)
		{
			IDatasetName featureDatasetName = DatasetUtils.GetFeatureDatasetName(datasetName);

			string tableName;
			string ownerName;
			string databaseName;

			if (featureDatasetName == null)
			{
				tableName = null;
				ownerName = null;
				databaseName = null;
			}
			else
			{
				DatasetUtils.ParseTableName(featureDatasetName,
				                            out databaseName,
				                            out ownerName,
				                            out tableName);
			}

			string pattern = GetFirstMatchedPattern(tableName, ownerName, databaseName);

			if (pattern == null)
			{
				reason = "Feature dataset name does not match any pattern";
				return false;
			}

			reason = $"Feature dataset name matches pattern '{pattern}'";
			return true;
		}
	}
}
