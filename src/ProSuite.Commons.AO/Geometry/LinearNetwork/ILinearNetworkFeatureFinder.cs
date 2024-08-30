using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.LinearNetwork
{
	public interface ILinearNetworkFeatureFinder
	{
		[NotNull]
		IList<IFeature> FindEdgeFeaturesAt([NotNull] IPoint point,
		                                   [CanBeNull] Predicate<IFeature> predicate = null);

		[NotNull]
		IList<IFeature> FindJunctionFeaturesAt([NotNull] IPoint point);

		/// <summary>
		/// Gets the edge features that are connected to the specified edge feature at the
		/// end which can be specified.
		/// </summary>
		/// <param name="toEdgeFeature">The connected edge.</param>
		/// <param name="edgeGeometry">Optionally the connected edge's shape (typically the version
		/// before it was updated) to search with. If null is provided, the feature's shape is used.
		/// </param>
		/// <param name="atEnd">The end at which the search should be performed.</param>
		/// <returns></returns>
		[NotNull]
		IList<IFeature> GetConnectedEdgeFeatures(
			[NotNull] IFeature toEdgeFeature,
			[CanBeNull] IPolyline edgeGeometry = null,
			LineEnd atEnd = LineEnd.Both);

		/// <summary>
		/// Allows caching of all potential network features to avoid excessive data access.
		/// </summary>
		/// <param name="searchGeometry">The search geometry for features to be cached. It can
		///  be empty which should result in no caching.</param>
		void CacheTargetFeatureCandidates([NotNull] IGeometry searchGeometry);

		/// <summary>
		/// All potential network features that can be used instead of a separate round-trip.
		/// This list will be populated by <see cref="CacheTargetFeatureCandidates"/>
		/// </summary>
		[CanBeNull]
		IList<IFeature> TargetFeatureCandidates { get; }

		double SearchTolerance { get; }

		void InvalidateTargetFeatureCache();

		/// <summary>
		/// Creates the union of the searched classes of this and the classes of the other specified
		/// instance and creates a new network feature finder.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		ILinearNetworkFeatureFinder Union([NotNull] ILinearNetworkFeatureFinder other);
	}
}
