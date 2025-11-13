using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[TerrainTest]
	public class QaSurfaceSpikes : ContainerTest
	{
		[CanBeNull] private static TestIssueCodes _codes;

		private class Code : LocalTestIssueCodes
		{
			public const string MaximumSlopeAndDeltaZExceeded = "MaximumSlopeAndDeltaZExceeded";

			public Code() : base("TerrainSpikes") { }
		}

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private WKSEnvelope? _tileWksEnvelope;
		private readonly double _maxSlopeDegrees;
		private readonly double _maxDeltaZ;

		private readonly double _maxSlopeRadians;

		private readonly ITinTriangle _tinTriangle = new TinTriangleClass();
		private readonly ILongArray _triangleIndices = new LongArrayClass();

		[Doc(nameof(DocStrings.QaTerrainSpikes_0))]
		public QaSurfaceSpikes(
			[Doc(nameof(DocStrings.QaTerrainSpikes_terrain))] [NotNull]
			TerrainReference terrain,
			[Doc(nameof(DocStrings.QaTerrainSpikes_terrainTolerance))]
			double terrainTolerance,
			[Doc(nameof(DocStrings.QaTerrainSpikes_maxSlopeDegrees))]
			double maxSlopeDegrees,
			[Doc(nameof(DocStrings.QaTerrainSpikes_maxDeltaZ))]
			double maxDeltaZ)
			: base(new IReadOnlyTable[] { })
		{
			_maxSlopeDegrees = maxSlopeDegrees;
			_maxDeltaZ = maxDeltaZ;
			InvolvedTerrains = new List<TerrainReference>
			                   {terrain}; // {new GdbTerrainReference(terrain)}};
			TerrainTolerance = terrainTolerance;

			_maxSlopeRadians = MathUtils.ToRadians(maxSlopeDegrees);
		}

		[InternallyUsedTest]
		public QaSurfaceSpikes([NotNull] QaSurfaceSpikesDefinition definition)
			: this((TerrainReference)definition.Terrain, definition.TerrainTolerance, definition.MaxSlopeDegrees,
			       definition.MaxDeltaZ)
		{ }

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			return NoError;
		}

		protected override int ExecuteCore(ISurfaceRow surfaceRow, int surfaceIndex)
		{
			WKSEnvelope checkEnvelope = GetCheckEnvelope(surfaceRow);

			if (checkEnvelope.XMin > checkEnvelope.XMax ||
			    checkEnvelope.YMin > checkEnvelope.YMax)
			{
				return NoError;
			}

			ISimpleSurface surface = surfaceRow.Surface;
			var nodes = surface.AsTin() as ITinNodeCollection;
			Assert.NotNull(nodes, "tin node collection not available for surface");

			ITinNode node = new TinNodeClass();

			var errorCount = 0;

			int nodeCount = nodes.NodeCount;
			for (var index = 0; index < nodeCount; index++)
			{
				int nodeIndex = index + 1;

				nodes.QueryNode(nodeIndex, node);

				errorCount += CheckTinNode((ITin) nodes, node, nodeIndex, checkEnvelope);
			}

			return errorCount;
		}

		protected override void BeginTileCore(BeginTileParameters parameters)
		{
			IEnvelope tileEnvelope = parameters.TileEnvelope;

			if (tileEnvelope == null)
			{
				_tileWksEnvelope = null;
			}
			else
			{
				WKSEnvelope wksEnvelope;
				tileEnvelope.QueryWKSCoords(out wksEnvelope);

				_tileWksEnvelope = wksEnvelope;
			}
		}

		private int ReportSpike([NotNull] ITinNode tinNode,
		                        double minDeltaZ, double maxDeltaZ,
		                        double minSlopeRadians, double maxSlopeRadians)
		{
			double minSlopeDegrees = MathUtils.ToDegrees(minSlopeRadians);
			double maxSlopeDegrees = MathUtils.ToDegrees(maxSlopeRadians);

			double deltaZ0;
			double deltaZ1;
			GetAbsoluteDeltaZRange(minDeltaZ, maxDeltaZ, out deltaZ0, out deltaZ1);

			string description =
				string.Format("Point is identified as spike: all surrounding triangles have a " +
				              "Z difference greater than {0} ({1:N2}-{2:N2}) and a " +
				              "slope greater than {3}° ({4:N2}°-{5:N2}°)",
				              _maxDeltaZ, deltaZ0, deltaZ1,
				              _maxSlopeDegrees, minSlopeDegrees, maxSlopeDegrees);

			IPoint point = new PointClass();
			tinNode.QueryAsPoint(point);

			return ReportError(
				description, new InvolvedRows(), point, Codes[Code.MaximumSlopeAndDeltaZExceeded],
				null);
		}

		private static void GetAbsoluteDeltaZRange(double minDeltaZ, double maxDeltaZ,
		                                           out double deltaZ0, out double deltaZ1)
		{
			double minAbsDeltaZ = Math.Abs(minDeltaZ);
			double maxAbsDeltaZ = Math.Abs(maxDeltaZ);

			if (maxAbsDeltaZ >= minAbsDeltaZ)
			{
				deltaZ0 = minAbsDeltaZ;
				deltaZ1 = maxAbsDeltaZ;
			}
			else
			{
				deltaZ0 = maxAbsDeltaZ;
				deltaZ1 = minAbsDeltaZ;
			}
		}

		private static bool Contains(WKSEnvelope wksBox, WKSPointZ wksNode)
		{
			return wksNode.X >= wksBox.XMin &&
			       wksNode.X <= wksBox.XMax &&
			       wksNode.Y >= wksBox.YMin &&
			       wksNode.Y <= wksBox.YMax;
		}

		private int ValidateNode([NotNull] ITin tin,
		                         [NotNull] ITinNode tinNode,
		                         WKSPointZ wksNode,
		                         int nodeIndex)
		{
			const int noError = 0;

			double minSlope;
			double maxSlope;
			double minDeltaZ;
			double maxDeltaZ;
			double minAbsDeltaZ;
			bool inDataArea = GetNodeProperties(tin, wksNode, nodeIndex,
			                                    out minSlope, out maxSlope,
			                                    out minDeltaZ, out maxDeltaZ, out minAbsDeltaZ);

			if (! inDataArea)
			{
				return noError;
			}

			if (minSlope <= _maxSlopeRadians ||
			    minAbsDeltaZ <= _maxDeltaZ)
			{
				// delta z and/or slope are within allowed range
				return noError;
			}

			if (maxDeltaZ > 0 && minDeltaZ < 0)
			{
				// Node within a steep slope
				return noError;
			}

			return ReportSpike(tinNode, minDeltaZ, maxDeltaZ, minSlope, maxSlope);
		}

		private bool GetNodeProperties([NotNull] ITin tin, WKSPointZ wksNode,
		                               int nodeIndex,
		                               out double minSlope, out double maxSlope,
		                               out double minDeltaZ, out double maxDeltaZ,
		                               out double minAbsDeltaZ)
		{
			((ITinAdvanced2) tin).QueryTriangleIndicesAroundNode(nodeIndex, _triangleIndices);
			int triangleCount = _triangleIndices.Count;

			maxSlope = double.MinValue;
			minSlope = double.MaxValue;

			maxDeltaZ = double.MinValue;
			minDeltaZ = double.MaxValue;
			minAbsDeltaZ = double.MaxValue;

			var allHorizontal = true;

			ITinTriangle triangle = _tinTriangle;
			var dataTriangleCount = 0;

			for (var index = 0; index < triangleCount; index++)
			{
				int globalIndex = _triangleIndices.get_Element(index);
				((ITinAdvanced2) tin).QueryTriangle(globalIndex, triangle);

				double slope = triangle.SlopeRadians;

				if (double.IsNaN(slope))
				{
					// triangle is outside data area
					// could be determined explicitly using triangle.IsInDataArea, but this is more expensive
					continue;
				}

				dataTriangleCount++;

				maxSlope = Math.Max(maxSlope, slope);
				minSlope = Math.Min(minSlope, slope);

				double nodeZ = Assert.NotNaN(wksNode.Z);

				foreach (WKSPointZ vertex in GetVertices(triangle))
				{
					double deltaZ = Assert.NotNaN(vertex.Z) - nodeZ;

					if (Math.Abs(deltaZ) < double.Epsilon)
					{
						// same node, or other node at same height
						continue;
					}

					maxDeltaZ = Math.Max(deltaZ, maxDeltaZ);
					minDeltaZ = Math.Min(deltaZ, minDeltaZ);
					minAbsDeltaZ = Math.Min(minAbsDeltaZ, Math.Abs(deltaZ));
					allHorizontal = false;
				}
			}

			if (dataTriangleCount == 0)
			{
				return false;
			}

			if (allHorizontal)
			{
				// min/max values not assigned --> set to 0
				minDeltaZ = 0;
				maxDeltaZ = 0;
				minAbsDeltaZ = 0;
			}

			return true;
		}

		[NotNull]
		private static IEnumerable<WKSPointZ> GetVertices([NotNull] ITinTriangle triangle)
		{
			WKSPointZ a;
			WKSPointZ b;
			WKSPointZ c;
			triangle.QueryVertices(out a, out b, out c);

			yield return a;
			yield return b;
			yield return c;
		}

		private WKSEnvelope GetCheckEnvelope([NotNull] ISurfaceRow surfaceRow)
		{
			IEnvelope box = surfaceRow.Extent;

			WKSEnvelope rowEnvelope;
			box.QueryWKSCoords(out rowEnvelope);

			if (! _tileWksEnvelope.HasValue)
			{
				return rowEnvelope;
			}

			WKSEnvelope tileEnvelope = _tileWksEnvelope.Value;

			return new WKSEnvelope
			       {
				       XMin = Math.Max(rowEnvelope.XMin, tileEnvelope.XMin),
				       YMin = Math.Max(rowEnvelope.YMin, tileEnvelope.YMin),
				       XMax = Math.Min(rowEnvelope.XMax, tileEnvelope.XMax),
				       YMax = Math.Min(rowEnvelope.YMax, tileEnvelope.YMax)
			       };
		}

		private int CheckTinNode([NotNull] ITin tin,
		                         [NotNull] ITinNode tinNode,
		                         int nodeIndex,
		                         WKSEnvelope checkWksEnvelope)
		{
			WKSPointZ wksNode;
			tinNode.QueryAsWKSPointZ(out wksNode);

			return Contains(checkWksEnvelope, wksNode)
				       ? ValidateNode(tin, tinNode, wksNode, nodeIndex)
				       : NoError;
		}
	}
}
