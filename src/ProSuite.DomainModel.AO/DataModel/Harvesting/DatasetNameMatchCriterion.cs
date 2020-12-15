using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.DataModel.Harvesting
{
	[CLSCompliant(false)]
	public class DatasetNameMatchCriterion : DatasetMatchCriterionBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DatasetNameMatchCriterion"/> class.
		/// </summary>
		/// <param name="namePatterns">The name patterns.</param>
		public DatasetNameMatchCriterion([NotNull] IEnumerable<string> namePatterns)
			: base(namePatterns) { }

		[CLSCompliant(false)]
		public override bool IsSatisfied(IDatasetName datasetName, out string reason)
		{
			string databaseName;
			string ownerName;
			string tableName;
			DatasetUtils.ParseTableName(datasetName,
			                            out databaseName,
			                            out ownerName,
			                            out tableName);

			string pattern = GetFirstMatchedPattern(tableName, ownerName, databaseName);

			if (pattern == null)
			{
				reason = "Name does not match any pattern";
				return false;
			}

			reason = $"Name matches pattern '{pattern}'";
			return true;
		}
	}
}
