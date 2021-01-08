using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.DataModel.Harvesting
{
	[CLSCompliant(false)]
	public abstract class DatasetMatchCriterionBase : IDatasetMatchCriterion
	{
		[NotNull] private readonly List<GdbElementNamePattern> _namePatterns;

		protected DatasetMatchCriterionBase([NotNull] IEnumerable<string> namePatterns)
		{
			Assert.ArgumentNotNull(namePatterns, nameof(namePatterns));

			_namePatterns = new List<GdbElementNamePattern>();

			foreach (string namePattern in namePatterns)
			{
				_namePatterns.Add(new GdbElementNamePattern(namePattern));
			}
		}

		public abstract bool IsSatisfied(IDatasetName datasetName, out string reason);

		[CanBeNull]
		protected string GetFirstMatchedPattern([CanBeNull] string tableName,
		                                        [CanBeNull] string ownerName,
		                                        [CanBeNull] string databaseName)
		{
			foreach (GdbElementNamePattern pattern in _namePatterns)
			{
				if (pattern.Matches(tableName, ownerName, databaseName))
				{
					return pattern.Pattern;
				}
			}

			return null;
		}
	}
}
