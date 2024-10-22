using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.Geodatabase;

public static class RelationshipClassUtils
{
	public static IEnumerable<Table> GetOriginClasses([NotNull] Dataset destinationClass)
	{
		string name = destinationClass.GetName();
		using var geodatabase = (ArcGIS.Core.Data.Geodatabase) destinationClass.GetDatastore();

		IEnumerable<RelationshipClassDefinition> definitions =
			DatasetUtils.GetRelationshipClassDefinitions(geodatabase, relClass =>
															 string.Equals(
																 relClass.GetDestinationClass(), name,
																 StringComparison.OrdinalIgnoreCase));


		foreach (RelationshipClassDefinition definition in definitions)
		{
			try
			{
				yield return DatasetUtils.OpenDataset<Table>(geodatabase, definition.GetOriginClass());
			}
			finally
			{
				definition.Dispose();
			}
		}
	}

	public static IEnumerable<string> GetOriginClassNames([NotNull] Dataset destinationClass)
	{
		string name = destinationClass.GetName();
		using var geodatabase = (ArcGIS.Core.Data.Geodatabase) destinationClass.GetDatastore();

		IEnumerable<RelationshipClassDefinition> definitions =
			DatasetUtils.GetRelationshipClassDefinitions(geodatabase, relClass =>
				                                             string.Equals(
					                                             relClass.GetDestinationClass(), name,
					                                             StringComparison.OrdinalIgnoreCase));
		

		foreach (RelationshipClassDefinition definition in definitions)
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
		using var geodatabase = (ArcGIS.Core.Data.Geodatabase) originClass.GetDatastore();

		// NOTE: Don't return IEnumerable<string>. It leads to exception because of the disposed geodatabase:
		// System.InvalidOperationException: Could not get the definitions of type RelationshipClassDefinition. Please make sure a valid geodatabase or data store has been opened first.
		foreach (string name in GetDestinationClassNames(geodatabase, originClassName))
		{
			yield return name;
		}
	}

	public static IEnumerable<string> GetDestinationClassNames([NotNull] ArcGIS.Core.Data.Geodatabase geodatabase, string originClassName)
	{
		IEnumerable<RelationshipClassDefinition> definitions =
			DatasetUtils.GetRelationshipClassDefinitions(geodatabase, relClass =>
				                                             string.Equals(
					                                             relClass.GetOriginClass(), originClassName,
					                                             StringComparison.OrdinalIgnoreCase));

		foreach (RelationshipClassDefinition definition in definitions)
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
	                                                    ICollection<long> oids)
	{
		string name = dataset.GetName();
		using var geodatabase = (ArcGIS.Core.Data.Geodatabase) dataset.GetDatastore();

		IEnumerable<RelationshipClass> relationshipClasses =
			DatasetUtils.GetRelationshipClasses(geodatabase, relClass =>
				                                    string.Equals(
					                                    relClass.GetDestinationClass(), name,
					                                    StringComparison.OrdinalIgnoreCase));

		// Don't return IEnumerable<Row> like this. It leads to
		// ArcGIS.Core.ObjectDisconnectedException : This object has been previously disposed and cannot be manipulated
		// of the geodatabase.
		//return relationshipClasses.SelectMany(relClass =>
		//										  relClass.GetRowsRelatedToDestinationRows(oids));

		foreach (RelationshipClass relClass in relationshipClasses)
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
