using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Processing.AGP.Core.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProSuite.Processing.AGP.Core.Utils
{
	public static class ProProcessingUtils
	{
		[NotNull]
		public static ICartoProcess CreateCartoProcess(Type type, CartoProcessRepo repo = null)
		{
			if (type == null || ! typeof(ICartoProcess).IsAssignableFrom(type))
			{
				throw new CartoConfigException(
					$"Type {type?.Name ?? "(null)"} is not a carto process type");
			}

			object instance;
			if (repo != null && type.GetConstructor(new[] { typeof(CartoProcessRepo) }) != null)
			{
				instance = Activator.CreateInstance(type, repo);
			}
			else
			{
				// by default, use the parameter-less constructor:
				instance = Activator.CreateInstance(type);
			}

			if (instance is null)
			{
				throw new AssertionException($"Activator returned null for type {type.Name}");
			}

			if (instance is not ICartoProcess process)
			{
				throw new AssertionException(
					$"Activator returned a {instance.GetType().Name} instance for type {type.Name}");
			}

			return process;
		}

		/// <summary>
		/// Create a string from the given object. Format:
		/// &quot;OID=123 Class=AliasNameOrDatasetName&quot;
		/// </summary>
		public static string Format(Feature feature)
		{
			if (feature is null) return string.Empty;

			var oid = feature.GetObjectID();
			string className;

			using (var table = feature.GetTable())
			{
				className = table?.GetName() ?? "UnknownTable";
			}

			return FormattableString.Invariant($"OID={oid} Class={className}");
		}

		public static T GetBaseTable<T>([CanBeNull] T table) where T : Table
		{
			if (table is null)
			{
				return null;
			}

			while (table.IsJoinedTable())
			{
				var join = table.GetJoin();
				var destination = join.GetDestinationTable();
				// TODO dispose table? (will underlying destination table survive?)
				table = (T) destination;
			}

			return table;

			//if (!layerTable.IsJoinedTable())
			//	return layerTable;

			//var join = layerTable.GetJoin();
			//var baseTable = join.GetDestinationTable();

			//return (T) baseTable;
		}

		[NotNull]
		public static QueryFilter CreateFilter(string whereClause, Geometry extent)
		{
			QueryFilter filter;

			if (extent != null)
			{
				filter = new SpatialQueryFilter
				         {
					         FilterGeometry = extent,
					         SpatialRelationship = SpatialRelationship.Intersects
				         };
			}
			else
			{
				filter = new QueryFilter();
			}

			if (! string.IsNullOrEmpty(whereClause))
			{
				filter.WhereClause = whereClause;
			}

			return filter;
		}

		/// <summary>
		/// Find the subtype, if any, of the given row values and for the
		/// given table (or feature class) definition; return null if no subtype.
		/// </summary>
		[CanBeNull]
		public static Subtype FindSubtype([NotNull] TableDefinition definition,
		                                  [NotNull] IRowValues values)
		{
			if (definition is null)
				throw new ArgumentNullException(nameof(definition));
			if (values is null)
				throw new ArgumentNullException(nameof(values));

			string subtypeFieldName = definition.GetSubtypeField();
			if (string.IsNullOrEmpty(subtypeFieldName))
			{
				return null;
			}

			object value = values.GetValue(subtypeFieldName);
			var subtypes = definition.GetSubtypes();

			if (value is int code)
			{
				return subtypes?.FirstOrDefault(s => s.GetCode() == code);
			}

			if (value is string name)
			{
				return subtypes?.FirstOrDefault(s => string.Equals(s.GetName(),
					                                name)); // TODO ignore case?
			}

			// First subtype is default subtype (empirical)
			return subtypes?.FirstOrDefault();
		}

		/// <summary>
		/// Ensure that the given <paramref name="builder"/> has a vertex at
		/// <paramref name="distanceAlong"/> from the start of the curve.
		/// Set <paramref name="partIndex"/> and <paramref name="vertexIndex"/>
		/// to identify this vertex.
		/// </summary>
		/// <returns>
		/// <c>true</c> if a segment was split, otherwise <c>false</c>
		/// </returns>
		/// <remarks>
		/// If there is an existing vertex within <paramref name="snapTolerance"/>
		/// from <paramref name="distanceAlong"/> (measured along the curve), use
		/// it. Otherwise, split the segment at <paramref name="distanceAlong"/>
		/// the curve to create a vertex.
		/// </remarks>
		public static bool EnsureVertex([NotNull] PolylineBuilderEx builder, double distanceAlong,
		                                double snapTolerance, out int partIndex,
		                                out int vertexIndex)
		{
			double cumulative = 0.0;

			for (int part = 0; part < builder.PartCount; ++part)
			{
				int segmentCount = builder.GetSegmentCount(part);

				for (int segmentIndex = 0; segmentIndex < segmentCount; ++segmentIndex)
				{
					var segment = builder.GetSegment(part, segmentIndex);
					double segmentEnd = cumulative + segment.Length;

					bool isLast = part == builder.PartCount - 1 && segmentIndex == segmentCount - 1;

					if (distanceAlong <= segmentEnd || isLast)
					{
						// distanceAlong falls within (or after) this segment
						double distToStart = distanceAlong - cumulative;
						double distToEnd = segmentEnd - distanceAlong;

						if (distToStart <= snapTolerance)
						{
							partIndex = part;
							vertexIndex = segmentIndex;
							return false; // snap to start vertex
						}

						if (distToEnd <= snapTolerance)
						{
							partIndex = part;
							vertexIndex = segmentIndex + 1;
							return false; // snap to end vertex
						}

						// No nearby vertex — split the segment
						builder.SplitAtDistance(distanceAlong, false);
						partIndex = part;
						vertexIndex = segmentIndex + 1;
						return true;
					}

					cumulative = segmentEnd;
				}
			}

			// Should never reach here for a valid distanceAlong
			partIndex = vertexIndex = -1;
			return false;
		}

		/// <summary>
		/// Ensure that <i>polycurve</i> has a vertex near <i>point</i>.
		/// Return that vertex's <i>partIndex</i> and <i>vertexIndex</i>.
		/// </summary>
		/// <remarks>
		/// If there's an existing vertex within <i>tolerance</i> from
		/// the given <i>point</i>, use it; otherwise, split a segment.
		/// This method should work fine with non-linear segments.
		/// </remarks>
		/// <returns>True if a segment was split; otherwise, false.</returns>
		// TODO Consider separate vertexSnapTolerance and searchRadius parameters
		public static bool EnsureVertex([NotNull] PolylineBuilderEx builder,
		                                [NotNull] MapPoint point, double tolerance,
		                                out int partIndex, out int vertexIndex)
		{
			Assert.ArgumentNotNull(builder, nameof(builder));
			Assert.ArgumentNotNull(point, nameof(point));

			double minVertexDist = double.MaxValue;
			int bestVertexPart = -1;
			int bestVertexIndex = -1;

			double minSegDist = double.MaxValue;
			int bestSegmentPart = -1;
			int bestSegmentIndex = -1;
			double bestDistAlong = 0.0;

			double cumulative = 0.0;

			for (int part = 0; part < builder.PartCount; ++part)
			{
				int segmentCount = builder.GetSegmentCount(part);
				for (int segmentIndex = 0; segmentIndex < segmentCount; ++segmentIndex)
				{
					var segment = builder.GetSegment(part, segmentIndex);

					// Check start vertex (= vertex index seg).
					// The end vertex of the last segment (= vertex index segCount)
					// is not a start vertex of any segment, so handle it explicitly.
					double distToStart =
						GeometryEngine.Instance.Distance(segment.StartPoint, point);
					if (distToStart < minVertexDist)
					{
						minVertexDist = distToStart;
						bestVertexPart = part;
						bestVertexIndex = segmentIndex;
					}

					if (segmentIndex == segmentCount - 1)
					{
						double distToEnd =
							GeometryEngine.Instance.Distance(segment.EndPoint, point);
						if (distToEnd < minVertexDist)
						{
							minVertexDist = distToEnd;
							bestVertexPart = part;
							bestVertexIndex = segmentIndex + 1;
						}
					}

					// Check foot-point distance for potential split.
					GeometryEngine.Instance.QueryPointAndDistance(
						segment, SegmentExtensionType.NoExtension, point,
						AsRatioOrLength.AsLength,
						out double distAlongSeg, out double distFromSeg, out _);

					if (distFromSeg < minSegDist)
					{
						minSegDist = distFromSeg;
						bestSegmentPart = part;
						bestSegmentIndex = segmentIndex;
						bestDistAlong = cumulative + distAlongSeg;
					}

					cumulative += segment.Length;
				}
			}

			if (minVertexDist <= tolerance)
			{
				partIndex = bestVertexPart;
				vertexIndex = bestVertexIndex;
				return false; // snap to existing vertex, no split
			}

			builder.SplitAtDistance(bestDistAlong, false);
			partIndex = bestSegmentPart;
			vertexIndex = bestSegmentIndex + 1;
			return true; // segment split
		}

		public static double GetMaxXYTolerance(
			[CanBeNull] ProcessingDataset dataset,
			[CanBeNull] IEnumerable<ProcessingDataset> more)
		{
			double tolerance = 0;

			if (dataset != null && dataset.XYTolerance > tolerance)
			{
				tolerance = dataset.XYTolerance;
			}

			if (more != null)
			{
				foreach (var extra in more)
				{
					if (extra != null && extra.XYTolerance > tolerance)
					{
						tolerance = extra.XYTolerance;
					}
				}
			}

			return tolerance;
		}
	}
}
