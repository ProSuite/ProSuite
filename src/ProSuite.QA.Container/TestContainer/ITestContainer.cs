using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestContainer
{
	internal interface ITestContainer : ITestProgress
	{
		// TODO pass whatever possible as parameters 

		[NotNull]
		IList<ContainerTest> ContainerTests { get; }

		bool CalculateRowCounts { get; }

		bool FilterExpressionsUseDbSyntax { get; }

		[NotNull]
		IGeometryEngine GeometryEngine { get; }

		[CanBeNull]
		ISpatialReference SpatialReference { get; }

		/// <summary>
		/// Get the maximum number of points that are cached between tiles.
		/// A value &lt; 0 indicates unlimited cache
		/// </summary>
		int MaxCachedPointCount { get; }

		void BeginTile([NotNull] IEnvelope tileEnvelope,
		               [NotNull] IEnvelope testRunEnvelope);

		void CompleteTile(TileState tileState,
		                  [NotNull] IEnvelope tileEnvelope,
		                  [NotNull] IEnvelope testRunEnvelope,
		                  [NotNull] OverlappingFeatures overlappingFeatures);

		void ClearErrors(double xMax, double yMax);

		void SubscribeTestEvents([NotNull] ContainerTest containerTest);

		void UnsubscribeTestEvents([NotNull] ContainerTest containerTest);

		/// <summary>
		/// Indicates if the test run was cancelled.
		/// </summary>
		bool Cancelled { get; }
	}
}
