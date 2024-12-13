using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.Geodatabase;

public static class RelationshipClassUtils
{
	public static IEnumerable<RelationshipClassDefinition> GetRelationshipClassDefinitions(
		[NotNull] ArcGIS.Core.Data.Geodatabase geodatabase,
		[CanBeNull] Predicate<RelationshipClassDefinition> predicate = null)
	{
		foreach (RelationshipClassDefinition definition in geodatabase
			         .GetDefinitions<RelationshipClassDefinition>())
		{
			if (predicate is null || predicate(definition))
			{
				yield return definition;
			}
		}

		foreach (AttributedRelationshipClassDefinition definition in
		         geodatabase.GetDefinitions<AttributedRelationshipClassDefinition>())
		{
			if (predicate is null || predicate(definition))
			{
				yield return definition;
			}
		}
	}

	public static IEnumerable<RelationshipClass> GetRelationshipClasses(
		[NotNull] ArcGIS.Core.Data.Geodatabase geodatabase,
		[CanBeNull] Predicate<RelationshipClassDefinition> predicate = null)
	{
		foreach (RelationshipClassDefinition definition in
		         GetRelationshipClassDefinitions(geodatabase, predicate))
		{
			try
			{
				yield return geodatabase.OpenDataset<RelationshipClass>(definition.GetName());
			}
			finally
			{
				definition.Dispose();
			}
		}
	}

	public static RelationshipClass OpenRelationshipClass(
		[NotNull] ArcGIS.Core.Data.Geodatabase geodatabase,
		[NotNull] string relClassName)
	{
		return DatasetUtils.OpenDataset<RelationshipClass>(geodatabase, relClassName);
	}

	public static IEnumerable<Table> GetOriginClasses([NotNull] Dataset destinationClass)
	{
		string destinationClassName = destinationClass.GetName();
		using (var geodatabase = (ArcGIS.Core.Data.Geodatabase) destinationClass.GetDatastore())
		{

			Predicate<RelationshipClassDefinition> predicate =
				relClass => string.Equals(relClass.GetDestinationClass(),
				                          destinationClassName,
				                          StringComparison.OrdinalIgnoreCase);

			foreach (RelationshipClassDefinition definition in
			         GetRelationshipClassDefinitions(geodatabase, predicate))
			{
				try
				{
					yield return DatasetUtils.OpenDataset<Table>(geodatabase,
					                                             definition.GetOriginClass());
				}
				finally
				{
					definition.Dispose();
				}
			}
		}
	}

	public static IEnumerable<string> GetOriginClassNames([NotNull] Dataset destinationClass)
	{
		string destinationClassName = destinationClass.GetName();
		using (var geodatabase = (ArcGIS.Core.Data.Geodatabase) destinationClass.GetDatastore())
		{
			// NOTE: Don't return IEnumerable<string>. It leads to exception because of the disposed geodatabase:
			// System.InvalidOperationException: Could not get the definitions of type RelationshipClassDefinition. Please make sure a valid geodatabase or data store has been opened first.
			foreach (string name in GetOriginClassNames(geodatabase, destinationClassName))
			{
				yield return name;
			}
		}
	}

	public static IEnumerable<string> GetOriginClassNames(
		[NotNull] ArcGIS.Core.Data.Geodatabase geodatabase,
		[NotNull] string destinationClassName)
	{
		Predicate<RelationshipClassDefinition> predicate =
			relClass => string.Equals(relClass.GetDestinationClass(),
			                          destinationClassName,
			                          StringComparison.OrdinalIgnoreCase);

		foreach (RelationshipClassDefinition definition in
		         GetRelationshipClassDefinitions(geodatabase, predicate))
		{
			// try finally in case an exception happens after yield return
			try
			{
				yield return definition.GetOriginClass();
			}
			finally
			{
				definition.Dispose();
			}
		}
	}

	public static IEnumerable<string> GetDestinationClassNames([NotNull] Dataset originClass)
	{
		string originClassName = originClass.GetName();
		using (var geodatabase = (ArcGIS.Core.Data.Geodatabase) originClass.GetDatastore())

		{
			// NOTE: Don't return IEnumerable<string>. It leads to exception because of the disposed geodatabase:
			// System.InvalidOperationException: Could not get the definitions of type RelationshipClassDefinition. Please make sure a valid geodatabase or data store has been opened first.
			foreach (string name in GetDestinationClassNames(geodatabase, originClassName))
			{
				yield return name;
			}
		}
	}

	public static IEnumerable<string> GetDestinationClassNames(
		[NotNull] ArcGIS.Core.Data.Geodatabase geodatabase,
		[NotNull] string originClassName)
	{
		Predicate<RelationshipClassDefinition> predicate =
			relClass => string.Equals(relClass.GetOriginClass(),
			                          originClassName,
			                          StringComparison.OrdinalIgnoreCase);

		foreach (RelationshipClassDefinition definition in
		         GetRelationshipClassDefinitions(geodatabase, predicate))
		{
			// try finally in case an exception happens after yield return
			try
			{
				yield return definition.GetDestinationClass();
			}
			finally
			{
				definition.Dispose();
			}
		}
	}

	public static IEnumerable<Row> GetRelatedOriginRows([NotNull] Dataset dataset,
	                                                    [NotNull] ICollection<long> oids)
	{
		string destinationDatasetName = dataset.GetName();
		using (var geodatabase = (ArcGIS.Core.Data.Geodatabase) dataset.GetDatastore())
		{
			Predicate<RelationshipClassDefinition> predicate =
				relClass => string.Equals(relClass.GetDestinationClass(),
				                          destinationDatasetName,
				                          StringComparison.OrdinalIgnoreCase);

			// NOTE: Don't return IEnumerable<Row> like this.
			// It leads to ArcGIS.Core.ObjectDisconnectedException : This object has been previously disposed and cannot be manipulated
			// of the geodatabase.
			//return relationshipClasses.SelectMany(relClass =>
			//										  relClass.GetRowsRelatedToDestinationRows(oids));

			foreach (RelationshipClass relClass in GetRelationshipClasses(geodatabase, predicate))
			{
				// try finally in case an exception happens after yield return
				try
				{
					foreach (Row row in relClass.GetRowsRelatedToDestinationRows(oids))
					{
						yield return row;
					}
				}
				finally
				{
					relClass.Dispose();
				}
			}
		}
	}
}
