using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.LinearNetwork
{
	public class LinearNetworkGdbFeatureFinder : LinearNetworkFeatureFinderBase
	{
		private IWorkspace _searchWorkspace;

		/// <summary>
		/// Instantiate a network finder that searches the specified network classes for
		/// geometrically connected features.
		/// The specified list of <see cref="LinearNetworkClassDef"/> defines a network of
		/// junctions (points) and edges (polylines).
		/// </summary>
		/// <param name="linearNetworkClassDef">The list of <see cref="LinearNetworkClassDef"/>.</param>
		public LinearNetworkGdbFeatureFinder(
			[NotNull] IList<LinearNetworkClassDef> linearNetworkClassDef)
		{
			Assert.ArgumentNotNull(linearNetworkClassDef, nameof(linearNetworkClassDef));

			LinearNetworkClassDefs = linearNetworkClassDef;

			SearchTolerance =
				DatasetUtils.GetMaximumXyTolerance(
					LinearNetworkClassDefs.Select(cd => cd.FeatureClass));
		}

		/// <summary>
		/// Instantiate a network finder that searches the provided cache of potential target features
		/// for geometrically connected features.
		/// </summary>
		/// <param name="targetFeatureCandidates">The potential target features.</param>
		public LinearNetworkGdbFeatureFinder(
			[NotNull] IEnumerable<IFeature> targetFeatureCandidates)
		{
			Assert.ArgumentNotNull(targetFeatureCandidates, nameof(targetFeatureCandidates));

			TargetFeatureCandidates = new List<IFeature>(targetFeatureCandidates);
		}

		[CanBeNull]
		private IList<LinearNetworkClassDef> LinearNetworkClassDefs { get; }

		protected override IList<IFeature> ReadFeaturesCore(IGeometry searchGeometry,
		                                                    ICollection<esriGeometryType>
			                                                    geometryTypes)
		{
			return SearchFeatureClasses(Assert.NotNull(LinearNetworkClassDefs), searchGeometry,
			                            geometryTypes, _searchWorkspace);
		}

		public void SetVersion([CanBeNull] IWorkspace workspace)
		{
			_searchWorkspace = workspace;
		}
	}
}
