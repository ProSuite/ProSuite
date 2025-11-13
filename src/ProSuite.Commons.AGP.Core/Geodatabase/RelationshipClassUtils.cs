using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Notifications;

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

	public static IEnumerable<RelationshipClassDefinition> GetRelationshipClassDefinitionsForAnnotation([NotNull] Dataset originClass)
	{
		string originClassName = originClass.GetName();
		using Datastore datastore = originClass.GetDatastore();
		if (datastore is not ArcGIS.Core.Data.PluginDatastore.PluginDatastore)
		{
			using var geodatabase = datastore as ArcGIS.Core.Data.Geodatabase;
			if (geodatabase != null)
			{
				Predicate<RelationshipClassDefinition> predicate =
					relClass => string.Equals(relClass.GetOriginClass(),
					                          originClassName,
					                          StringComparison.OrdinalIgnoreCase);

				foreach (RelationshipClassDefinition definition in
				         RelationshipClassUtils.GetRelationshipClassDefinitions(geodatabase, predicate))
				{
					string destinationClassName = definition.GetDestinationClass();
					if (!string.IsNullOrEmpty(destinationClassName))
					{
						using Table destinationClass = DatasetUtils.OpenDataset<Table>(geodatabase, destinationClassName);

						if (destinationClass is ArcGIS.Core.Data.Mapping.AnnotationFeatureClass)
						{
							yield return definition;
						}
					}
				}
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

	/// <summary>
	/// Gets the list of rows that are related to the given row.
	/// </summary>
	/// <param name="gdbRow">Row for which related rows should be found.</param>
	/// <param name="relationshipClass">The relationship class</param>
	/// <param name="rowIsOrigin">Whether the <see cref="gdbRow"/> is part of the origin class.</param>
	/// <returns>List with rows related to the input row, could be empty.</returns>
	[NotNull]
	public static IReadOnlyList<Row> GetRelatedRows(
		[NotNull] Row gdbRow,
		[NotNull] RelationshipClass relationshipClass,
		bool rowIsOrigin)
	{
		var rowIdList = new List<long> { gdbRow.GetObjectID() };

		return rowIsOrigin
			       ? relationshipClass.GetRowsRelatedToOriginRows(rowIdList)
			       : relationshipClass.GetRowsRelatedToDestinationRows(rowIdList);
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

	[CanBeNull]
	public static Relationship TryCreateRelationship(
		[NotNull] Row originObj,
		[NotNull] Row destinationObj,
		[NotNull] RelationshipClass relationshipClass,
		bool tryAddMissingPrimaryKey,
		bool overwriteExistingForeignKeys,
		[CanBeNull] NotificationCollection notifications)
	{
		Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

		//Row originObj, destinationObj;
		//DetermineRoles(obj1, obj2, relationshipClass, out originObj, out destinationObj);

		// TODO: Is this still necessary?
		//if (!TryEnsurePrimaryKey(originObj,
		//                         relationshipClass.OriginPrimaryKey,
		//                         tryAddMissingPrimaryKey, notifications))
		//{
		//	return null;
		//}

		if (relationshipClass.GetDefinition().GetCardinality() ==
		    RelationshipCardinality.ManyToMany)
		{
			// TODO: Is this still necessary?
			//if (!TryEnsurePrimaryKey(destinationObj,
			//                         relationshipClass.DestinationPrimaryKey,
			//                         tryAddMissingPrimaryKey, notifications))
			//{
			//	return null;
			//}
		}

		return ! CanCreateRelationship(originObj, destinationObj, relationshipClass,
		                               overwriteExistingForeignKeys, notifications)
			       ? null
			       : relationshipClass.CreateRelationship(originObj, destinationObj);
	}

	private static bool CanCreateRelationship(
		[NotNull] Row originRow,
		[NotNull] Row destinationRow,
		[NotNull] RelationshipClass relationshipClass,
		bool allowOverwriteForeignKey,
		[CanBeNull] NotificationCollection notifications)
	{
		RelationshipClassDefinition relClassDefinition = relationshipClass.GetDefinition();

		// make sure no existing foreign key value gets overwritten
		if (relationshipClass.GetDefinition().GetCardinality() !=
		    RelationshipCardinality.ManyToMany)
		{
			if (allowOverwriteForeignKey)
			{
				return true;
			}

			object existingForeignKey = GetFieldValue(destinationRow,
			                                          relClassDefinition
				                                          .GetOriginForeignKeyField());

			if (! Convert.IsDBNull(existingForeignKey))
			{
				if (existingForeignKey ==
				    GetFieldValue(originRow, relClassDefinition.GetOriginKeyField()))
				{
					NotificationUtils.Add(notifications,
					                      "{0} and {1} already have a relationship",
					                      GdbObjectUtils.ToString(originRow),
					                      GdbObjectUtils.ToString(destinationRow));

					// but setting the foreign key again won't do any harm:
					return true;
				}

				// it is still possible that the old origin still exists but the destination was duplicated and we
				// are dealing with a duplicate here (that now points to the wrong origin and we can fix it)
				// Example: The roof (destination) is exploded and in the create-feature event a new grundriss
				//		    is created that should be linked to the roof-copies (overwriting the existing foreign key)
				// this cannot be detected other than with a parameter: allowOverwriteForeignKey

				var preRelatedObjects = GetRelatedRows(destinationRow, relationshipClass, false);

				if (preRelatedObjects.Count == 0)
				{
					// either relational integrity is violated or the other origin was deleted in this very edit operation
					// and the field was not yet nulled - it's ok for a new relationship
					return true;
				}

				NotificationUtils.Add(notifications,
				                      "Destination object {0} already has a relationship to another origin object",
				                      GdbObjectUtils.ToString(destinationRow));

				return false;
			}

			return true;
		}

		// make sure (if m:n) it does not already exist -> duplicate m:n relationships are not
		// prevented by ArcObjects

		bool hasRelationship = GetRelatedRows(originRow, relationshipClass, true)
			.Any(rr => rr.GetObjectID() == destinationRow.GetObjectID());

		//Relationship existingRelationship = relationshipClass.GetRelationship(originRow,
		//                                                                      destinationRow);

		if (hasRelationship && relationshipClass is not AttributedRelationshipClass)
		{
			NotificationUtils.Add(notifications, "{0} and {1} already have a relationship",
			                      GdbObjectUtils.ToString(originRow),
			                      GdbObjectUtils.ToString(destinationRow));

			// creating an additional relationship would duplicate the existing entry
			return false;
		}

		return true;
	}

	private static object GetFieldValue(Row row, string fieldName)
	{
		Assert.ArgumentNotNull(row, nameof(row));
		Assert.ArgumentNotNull(fieldName, nameof(fieldName));

		TableDefinition tableDefinition = row.GetTable().GetDefinition();

		int fieldIndex = tableDefinition.FindField(fieldName);

		Assert.True(fieldIndex >= 0, "Field {0} not found in {1}", fieldName,
		            tableDefinition.GetName());

		object value = row[fieldIndex];

		return value;
	}
}
