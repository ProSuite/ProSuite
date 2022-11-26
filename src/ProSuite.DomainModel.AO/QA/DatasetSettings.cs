using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.QA
{
	/// <summary>
	/// Helper class for standalone QA managing a specific dataset.
	/// </summary>
	public class DatasetSettings
	{
		public DatasetSettings([NotNull] Func<string, IList<Dataset>> getDatasetsByName,
		                       bool ignoreUnknownDatasets)
		{
			GetDatasetsByName = getDatasetsByName;
			IgnoreUnknownDatasets = ignoreUnknownDatasets;
			UnknownDatasetParameters = new List<DatasetTestParameterRecord>();
		}

		[NotNull]
		public Func<string, IList<Dataset>> GetDatasetsByName { get; }

		public bool IgnoreUnknownDatasets { get; }
		public List<DatasetTestParameterRecord> UnknownDatasetParameters { get; }
	}

	// TODO (in C# 9):
	//public record DatasetTestParameterRecord(string datasetValue, string workspaceId);
	public class DatasetTestParameterRecord
	{
		public DatasetTestParameterRecord(string datasetName, string workspaceId)
		{
			DatasetName = datasetName;
			WorkspaceId = workspaceId;
		}

		public string DatasetName { get; set; }
		public string WorkspaceId { get; set; }
	}
}
