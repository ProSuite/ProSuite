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
		/// <param name="linearNetworkClassDefinitions">The list of <see cref="LinearNetworkClassDef"/>.</param>
		public LinearNetworkGdbFeatureFinder(
			[NotNull] IList<LinearNetworkClassDef> linearNetworkClassDefinitions)
		{
			Assert.ArgumentNotNull(linearNetworkClassDefinitions,
			                       nameof(linearNetworkClassDefinitions));

			LinearNetworkClassDefinitions = linearNetworkClassDefinitions;

			SearchTolerance =
				DatasetUtils.GetMaximumXyTolerance(
					LinearNetworkClassDefinitions.Select(cd => cd.FeatureClass));
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
		private IList<LinearNetworkClassDef> LinearNetworkClassDefinitions { get; }

		protected override IList<IFeature> ReadFeaturesCore(IGeometry searchGeometry,
		                                                    ICollection<esriGeometryType>
			                                                    geometryTypes)
		{
			return SearchFeatureClasses(Assert.NotNull(LinearNetworkClassDefinitions),
			                            searchGeometry,
			                            geometryTypes, _searchWorkspace);
		}

		public void SetVersion([CanBeNull] IWorkspace workspace)
		{
			_searchWorkspace = workspace;
		}

		public override ILinearNetworkFeatureFinder Union(ILinearNetworkFeatureFinder other)
		{
			LinearNetworkGdbFeatureFinder otherFeatureFinder =
				other as LinearNetworkGdbFeatureFinder;

			Assert.NotNull(otherFeatureFinder,
			               "Both network feature finders must be of the same type");

			if (LinearNetworkClassDefinitions != null)
			{
				var unionedNetworkClasses =
					new List<LinearNetworkClassDef>(LinearNetworkClassDefinitions);

				IList<LinearNetworkClassDef> otherClassDefs =
					Assert.NotNull(otherFeatureFinder.LinearNetworkClassDefinitions);

				foreach (LinearNetworkClassDef networkClassDef in otherClassDefs)
				{
					if (unionedNetworkClasses.Any(c => c.Equals(networkClassDef)))
					{
						continue;
					}

					unionedNetworkClasses.Add(networkClassDef);
				}

				return new LinearNetworkGdbFeatureFinder(unionedNetworkClasses);
			}

			if (TargetFeatureCandidates != null)
			{
				return new LinearNetworkGdbFeatureFinder(UnionTargetFeatureSet(otherFeatureFinder));
			}

			throw new AssertionException(
				"Either the LinearNetworkClasses or the TargetFeatureCandidates must be non-null");
		}
	}
}
