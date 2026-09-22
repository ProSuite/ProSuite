using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Mapping;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AGP.Core.Geodatabase;

public static class RelationshipClassUtils
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	/// <summary>
	/// Gets the definitions of the simple and the attributed relationship classes of the
	/// geodatabase.
	/// </summary>
	public static IEnumerable<RelationshipClassDefinition> GetRelationshipClassDefinitions(
		[NotNull] ArcGIS.Core.Data.Geodatabase geodatabase,
		[CanBeNull] Predicate<RelationshipClassDefinition> predicate = null)
	{
		var result = new List<RelationshipClassDefinition>();

		if (! TryAddRelationshipClassDefinitions(geodatabase, result, out string message,
		                                         predicate))
		{
			_msg.Debug(message);
		}

		return result;
	}

	/// <summary>
	/// Adds the definitions of the simple and the attributed relationship classes of the
	/// geodatabase to the result.
	/// </summary>
	/// <param name="geodatabase">The geodatabase to read the definitions from.</param>
	/// <param name="toResult">The list to which the definitions are added.</param>
	/// <param name="message">The reason why a relationship class type could not be read,
	/// or null if all types could be read.</param>
	/// <param name="predicate">An optional condition that a definition must fulfill to be
	/// added.</param>
	/// <returns>True if the definitions of all relationship class types could be read.</returns>
	public static bool TryAddRelationshipClassDefinitions(
		[NotNull] ArcGIS.Core.Data.Geodatabase geodatabase,
		[NotNull] ICollection<RelationshipClassDefinition> toResult,
		[CanBeNull] out string message,
		[CanBeNull] Predicate<RelationshipClassDefinition> predicate = null)
	{
		Assert.ArgumentNotNull(geodatabase, nameof(geodatabase));
		Assert.ArgumentNotNull(toResult, nameof(toResult));

		if (geodatabase.GetGeodatabaseType() == GeodatabaseType.Memory)
		{
			// NOTE: Currently (3.8) pro throws InvalidOperationException for a memory geodatabase
			//       when getting RelationshipClassDefinition or AttributedRelationshipClassDefinition
			message = $"{WorkspaceUtils.GetDatastoreDisplayText(geodatabase)} does not " +
			          "support relationship classes";
			return false;
		}

		var failures = new List<string>();

		TryAddDefinitions<RelationshipClassDefinition>(
			geodatabase, toResult, predicate, failures);
		TryAddDefinitions<AttributedRelationshipClassDefinition>(
			geodatabase, toResult, predicate, failures);

		message = failures.Count == 0
			          ? null
			          : $"Not all relationship classes of " +
			            $"{WorkspaceUtils.GetDatastoreDisplayText(geodatabase)} can be read: " +
			            $"{string.Join(" ", failures)}";

		return failures.Count == 0;
	}

	private static void TryAddDefinitions<T>(
		[NotNull] ArcGIS.Core.Data.Geodatabase geodatabase,
		[NotNull] ICollection<RelationshipClassDefinition> toResult,
		[CanBeNull] Predicate<RelationshipClassDefinition> predicate,
		[NotNull] ICollection<string> failures) where T : RelationshipClassDefinition
	{
		IReadOnlyList<T> definitions;
		try
		{
			// NOTE: Currently (3.8) pro throws InvalidOperationException for a memory geodatabase,
			//       therefore it is filtered by the caller.
			definitions = geodatabase.GetDefinitions<T>();
		}
		catch (NotSupportedException e)
		{
			// For future Pro versions which might throw the proper exception type for unsupported geodatabases...
			failures.Add(e.Message);
			return;
		}
		catch (NotImplementedException e)
		{
			failures.Add(e.Message);
			return;
		}

		foreach (T definition in definitions)
		{
			if (predicate is null || predicate(definition))
			{
				toResult.Add(definition);
			}
			else
			{
				definition.Dispose();
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

	public static IEnumerable<RelationshipClassDefinition>
		GetRelationshipClassDefinitionsForAnnotation([NotNull] Dataset originClass)
	{
		string originClassName = originClass.GetName();
		using Datastore datastore = originClass.GetDatastore();
		if (datastore is not PluginDatastore)
		{
			using var geodatabase = datastore as ArcGIS.Core.Data.Geodatabase;
			if (geodatabase != null)
			{
				Predicate<RelationshipClassDefinition> predicate =
					relClass => string.Equals(relClass.GetOriginClass(),
					                          originClassName,
					                          StringComparison.OrdinalIgnoreCase);

				foreach (RelationshipClassDefinition definition in
				         GetRelationshipClassDefinitions(geodatabase, predicate))
				{
					string destinationClassName = definition.GetDestinationClass();
					if (! string.IsNullOrEmpty(destinationClassName))
					{
						using Table destinationClass =
							DatasetUtils.OpenDataset<Table>(geodatabase, destinationClassName);

						if (destinationClass is AnnotationFeatureClass)
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
		using Datastore datastore = destinationClass.GetDatastore();
		if (datastore is not ArcGIS.Core.Data.Geodatabase geodatabase)
		{
			// Shapefiles and plug-in data sources have no relationship classes.
			yield break;
		}

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

	public static IEnumerable<string> GetOriginClassNames([NotNull] Dataset destinationClass)
	{
		string destinationClassName = destinationClass.GetName();
		using Datastore datastore = destinationClass.GetDatastore();
		if (datastore is not ArcGIS.Core.Data.Geodatabase geodatabase)
		{
			// Shapefiles and plug-in data sources have no relationship classes.
			yield break;
		}

		// NOTE: Don't return IEnumerable<string>. It leads to exception because of the disposed geodatabase:
		// System.InvalidOperationException: Could not get the definitions of type RelationshipClassDefinition. Please make sure a valid geodatabase or data store has been opened first.
		foreach (string name in GetOriginClassNames(geodatabase, destinationClassName))
		{
			yield return name;
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
		using Datastore datastore = originClass.GetDatastore();
		if (datastore is not ArcGIS.Core.Data.Geodatabase geodatabase)
		{
			// Shapefiles and plug-in data sources have no relationship classes.
			yield break;
		}

		// NOTE: Don't return IEnumerable<string>. It leads to exception because of the disposed geodatabase:
		// System.InvalidOperationException: Could not get the definitions of type RelationshipClassDefinition. Please make sure a valid geodatabase or data store has been opened first.
		foreach (string name in GetDestinationClassNames(geodatabase, originClassName))
		{
			yield return name;
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
		using Datastore datastore = dataset.GetDatastore();
		if (datastore is not ArcGIS.Core.Data.Geodatabase geodatabase)
		{
			// Shapefiles and plug-in data sources have no relationship classes.
			yield break;
		}

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
