using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	[CLSCompliant(false)]
	public interface IObjectCache
	{
		/// <summary>
		/// Indicates that the cache should be completely invalidated (i.e. objects should 
		/// be refetched), based on a given feature workspace.
		/// </summary>
		/// <param name="featureWorkspace">The feature workspace.</param>
		void Invalidate([NotNull] IFeatureWorkspace featureWorkspace);

		/// <summary>
		/// Passes all changes that happened in an edit operation to the cache for processing.
		/// </summary>
		/// <param name="insertedObjects">The inserted objects.</param>
		/// <param name="updatedObjects">The updated objects.</param>
		/// <param name="deletedObjects">The deleted objects.</param>
		void ProcessChanges([NotNull] IList<IObject> insertedObjects,
		                    [NotNull] IList<IObject> updatedObjects,
		                    [NotNull] IList<IObject> deletedObjects);

		/// <summary>
		/// Determines whether the object cache can contain the specified object class. 
		/// This is used by the object cache synchronizer to ignore irrelevant change events.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <returns>
		/// 	<c>true</c> if the object cache can contain the specified object class; otherwise, <c>false</c>.
		///     Only changes for object classes for which this returns <c>true</c> will be passed to
		///     <see cref="ProcessChanges"/>.
		/// </returns>
		/// <remarks>If this check is potentially expensive for a given object cache implementation, 
		/// <c>true</c> can be returned. In this case the implementation of <see cref="ProcessChanges"/> 
		/// should ignore any irrelevant instances.</remarks>
		bool CanContain([NotNull] IObjectClass objectClass);
	}
}
