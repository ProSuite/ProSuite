using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ProSuite.Processing.Domain
{
	[TypeConverter(typeof(ProcessDatasetNameConverter))]
	public class ProcessDatasetName
	{
		public ProcessDatasetName(string datasetName, string whereClause = null)
		{
			if (datasetName == null)
				throw new ArgumentNullException(nameof(datasetName));

			DatasetName = datasetName.Trim();
			WhereClause = whereClause;
		}

		public string DatasetName { get; }

		public string WhereClause { get; }

		public static ProcessDatasetName Parse(string text)
		{
			if (text == null)
				throw new ArgumentNullException();

			string datasetName = text;
			string whereClause = null;

			// TODO allow "FC where WC" which reads better than "FC; WC"

			int index = text.IndexOf(';');
			if (index >= 0)
			{
				datasetName = text.Substring(0, index);
				whereClause = text.Substring(index + 1);
			}

			return new ProcessDatasetName(datasetName, whereClause);
		}

		public override string ToString()
		{
			if (string.IsNullOrEmpty(WhereClause))
				return DatasetName;
			return string.Format("{0}; {1}", DatasetName, WhereClause);
		}

		public static IEnumerable<ProcessDatasetName> Merge(
			IEnumerable<ProcessDatasetName> datasets)
		{
			return datasets.GroupBy(d => d.DatasetName, d => d, StringComparer.OrdinalIgnoreCase)
			               .Select(g => new ProcessDatasetName(g.Key, Or(g.Select(d => d.WhereClause))))
			               .ToList();
		}

		private static string Or(IEnumerable<string> whereClauses)
		{
			if (whereClauses.Any(string.IsNullOrWhiteSpace)) return null;
			return string.Join(" OR ", whereClauses.Select(wc => $"({wc})"));
		}
	}
}
