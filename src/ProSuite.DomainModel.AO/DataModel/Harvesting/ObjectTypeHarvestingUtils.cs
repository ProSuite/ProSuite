using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel.Harvesting
{
	public static class ObjectTypeHarvestingUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Reads the object types from the geodatabase.
		/// </summary>
		public static void HarvestObjectTypes([NotNull] ObjectDataset objectDataset)
		{
			IWorkspaceContext workspaceContext =
				ModelElementUtils.GetAccessibleMasterDatabaseWorkspaceContext(objectDataset);

			IObjectClass objectClass =
				Assert.NotNull(workspaceContext).OpenObjectClass(objectDataset);

			Assert.NotNull(objectClass, "Unable to open object class {0}", objectDataset.Name);

			HarvestObjectTypes(objectDataset, objectClass);
		}

		public static void HarvestObjectTypes([NotNull] ObjectDataset objectDataset,
		                                      [NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			IList<Subtype> subtypes = DatasetUtils.GetSubtypes(objectClass);

			if (subtypes.Count > 0)
			{
				for (var subtypeIndex = 0;
				     subtypeIndex < subtypes.Count;
				     subtypeIndex++)
				{
					Subtype subtype = subtypes[subtypeIndex];

					AddOrUpdateObjectType(objectDataset, subtype, subtypeIndex, subtypes);
				}
			}
			else
			{
				var subtype = new Subtype(0, "<default>");

				AddOrUpdateObjectType(objectDataset, subtype, 0, subtypes);

				subtypes.Add(subtype);
			}

			DeleteObjectTypesNotInList(objectDataset, subtypes);
		}

		private static void AddOrUpdateObjectType([NotNull] ObjectDataset objectDataset,
		                                          [NotNull] Subtype subtype,
		                                          int subtypeIndex,
		                                          [NotNull] IEnumerable<Subtype> allSubtypes)
		{
			Assert.ArgumentNotNull(subtype, nameof(subtype));
			Assert.ArgumentNotNull(allSubtypes, nameof(allSubtypes));

			ObjectType objectType = GetObjectType(objectDataset, subtype, allSubtypes);

			if (objectType == null)
			{
				objectType = new ObjectType(objectDataset, subtype.Code, subtype.Name);

				_msg.InfoFormat("Adding object type {0}", objectType.Name);

				objectDataset.AddObjectType(subtype.Code, subtype.Name);
			}
			else
			{
				// object type already registered

				if (objectType.Deleted)
				{
					_msg.WarnFormat(
						"Object type {0} ({1}) was registered as deleted, but exists now. " +
						"Resurrecting it...",
						objectType.Name, objectType.SubtypeCode);

					objectType.RegisterExisting();
				}

				// update properties
				if (! Equals(objectType.Name, subtype.Name))
				{
					_msg.InfoFormat("Updating name of object type {0} to {1}",
					                objectType.Name, subtype.Name);

					objectType.UpdateName(subtype.Name);
				}

				if (! Equals(objectType.SubtypeCode, subtype.Code))
				{
					_msg.InfoFormat("Updating subtype code of object type {0} from {1} to {2}",
					                objectType.Name, objectType.SubtypeCode, subtype.Code);

					objectType.UpdateSubtypeCode(subtype.Code);
				}
			}

			// configure the object type
			objectType.SortOrder = subtypeIndex;
		}

		[CanBeNull]
		private static ObjectType GetObjectType([NotNull] ObjectDataset objectDataset,
		                                        [NotNull] Subtype subtype,
		                                        [NotNull] IEnumerable<Subtype> allSubtypes)
		{
			ObjectType objectType = objectDataset.GetObjectType(subtype.Name);
			if (objectType != null)
			{
				// found a name match, use it
				return objectType;
			}

			// not found by name - search by code. 
			// If an object type is found, use it only if it has a name that is unique in the list of subtypes
			objectType = objectDataset.GetObjectType(subtype.Code);
			if (objectType == null)
			{
				return null;
			}

			// an object type with the same code (but a different name) was found. 
			// use it only if the name of object type is not equal to a subtype with *another* code
			// (as this would cause duplicate names)
			foreach (Subtype otherSubtype in allSubtypes)
			{
				if (subtype.Code != otherSubtype.Code &&
				    string.Equals(objectType.Name, otherSubtype.Name))
				{
					// the object type with the same code has a name that is equal to 
					// the name of a subtype with a different code. 

					// Renaming the found object type therefore would cause duplicate names
					// -> return null; this will trigger the addition/deletion instead of update
					return null;
				}
			}

			// NOTE 
			// this would be easier to understand if the mapping between existing object types 
			// and subtypes was established first, and THEN the properties of the object types 
			// would be updated. 

			return objectType;
		}

		/// <summary>
		/// Removes the object types that are not present in a list of subtypes.
		/// </summary>
		/// <remarks>Assumes that the object type list is in override mode.</remarks>
		/// <param name="objectDataset"></param>
		/// <param name="subtypes">The subtypes.</param>
		private static void DeleteObjectTypesNotInList([NotNull] ObjectDataset objectDataset,
		                                               [NotNull] ICollection<Subtype> subtypes)
		{
			Assert.ArgumentNotNull(subtypes, nameof(subtypes));

			foreach (ObjectType objectType in objectDataset.ObjectTypes)
			{
				if (ExistsObjectType(subtypes, objectType) || objectType.Deleted)
				{
					continue;
				}

				_msg.WarnFormat("Registering object type {0} as deleted",
				                objectType.Name);

				objectType.RegisterDeleted();
			}
		}

		private static bool ExistsObjectType([NotNull] IEnumerable<Subtype> subtypes,
		                                     [NotNull] ObjectType objectType)
		{
			Assert.ArgumentNotNull(subtypes, nameof(subtypes));
			Assert.ArgumentNotNull(objectType, nameof(objectType));

			return subtypes.Any(subtype => subtype.Code == objectType.SubtypeCode &&
			                               string.Equals(subtype.Name, objectType.Name));
		}
	}
}
