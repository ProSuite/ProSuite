using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// An axis-parallel rectangle in map units.
	/// </summary>
	public class PlanarExtent
	{
		public PlanarExtent(double xMin, double yMin, double xMax, double yMax)
		{
			Assert.ArgumentCondition(xMin <= xMax && yMin <= yMax,
			                         "Minimum must not exceed maximum");
			XMin = xMin;
			YMin = yMin;
			XMax = xMax;
			YMax = yMax;
		}

		public double XMin { get; }
		public double YMin { get; }
		public double XMax { get; }
		public double YMax { get; }

		public double Width => XMax - XMin;
		public double Height => YMax - YMin;

		[NotNull]
		public PlanarExtent Union([NotNull] PlanarExtent other)
		{
			return new PlanarExtent(Math.Min(XMin, other.XMin), Math.Min(YMin, other.YMin),
			                        Math.Max(XMax, other.XMax), Math.Max(YMax, other.YMax));
		}

		public bool Contains([NotNull] PlanarExtent other)
		{
			return other.XMin >= XMin && other.YMin >= YMin &&
			       other.XMax <= XMax && other.YMax <= YMax;
		}

		public override string ToString()
		{
			return $"{XMin:N1}, {YMin:N1}, {XMax:N1}, {YMax:N1}";
		}
	}

	/// <summary>
	/// Features that are sent together in one request, and the image extent that the
	/// request needs.
	/// </summary>
	public class FeatureCheckGroup
	{
		public FeatureCheckGroup([NotNull] IList<long> objectIds,
		                         [NotNull] PlanarExtent imageExtent)
		{
			ObjectIds = objectIds;
			ImageExtent = imageExtent;
		}

		[NotNull]
		public IList<long> ObjectIds { get; }

		[NotNull]
		public PlanarExtent ImageExtent { get; }
	}

	/// <summary>
	/// Decides which features are sent together, and which part of the image each request
	/// needs.
	/// </summary>
	public static class SemanticImageMatchGrouping
	{
		/// <summary>
		/// Side length of the grid cells that group features. One cell of a 10 cm image
		/// is about 75 MB as uncompressed RGB, well below the upload limit of the service.
		/// </summary>
		public const double DefaultCellSize = 500;

		/// <summary>The default limit of the service per job (MAX_CHECK_FEATURES).</summary>
		public const int DefaultMaximumFeaturesPerGroup = 500;

		/// <summary>
		/// Larger features are not sent: the service would read a window of 2.5 times
		/// this size at full resolution.
		/// </summary>
		public const double MaximumFeatureSize = 400;

		/// <summary>
		/// The part of the image the service shows the language model for a feature with
		/// this envelope. Must match <c>crop_box</c> in auspex/checks/run.py: a square
		/// centred on the envelope, 2.5 times its larger side and at least 30 m wide.
		/// </summary>
		[NotNull]
		public static PlanarExtent GetCheckWindow([NotNull] PlanarExtent featureEnvelope)
		{
			double half = Math.Max(15.0, 1.25 * Math.Max(featureEnvelope.Width,
			                                             featureEnvelope.Height));
			double centreX = (featureEnvelope.XMin + featureEnvelope.XMax) / 2;
			double centreY = (featureEnvelope.YMin + featureEnvelope.YMax) / 2;

			return new PlanarExtent(centreX - half, centreY - half,
			                        centreX + half, centreY + half);
		}

		public static bool IsTooLarge([NotNull] PlanarExtent featureEnvelope)
		{
			return Math.Max(featureEnvelope.Width, featureEnvelope.Height) >
			       MaximumFeatureSize;
		}

		/// <summary>
		/// Groups the features by the grid cell that contains the centre of their
		/// envelope. A cell with more features than allowed per request is split. The
		/// image extent of a group is the union of the check windows of its features, so
		/// that the service never has to draw areas without image data.
		/// </summary>
		[NotNull]
		public static IList<FeatureCheckGroup> Group(
			[NotNull] IDictionary<long, PlanarExtent> featureEnvelopes,
			double cellSize = DefaultCellSize,
			int maximumFeaturesPerGroup = DefaultMaximumFeaturesPerGroup)
		{
			Assert.ArgumentNotNull(featureEnvelopes, nameof(featureEnvelopes));
			Assert.ArgumentCondition(cellSize > 0, "Cell size must be positive");
			Assert.ArgumentCondition(maximumFeaturesPerGroup > 0,
			                         "Maximum features per group must be positive");

			var result = new List<FeatureCheckGroup>();

			IEnumerable<IGrouping<Tuple<long, long>, long>> cells =
				featureEnvelopes
					.OrderBy(pair => pair.Key)
					.GroupBy(pair => GetCell(pair.Value, cellSize), pair => pair.Key)
					.OrderBy(cell => cell.Key.Item2)
					.ThenBy(cell => cell.Key.Item1);

			foreach (IGrouping<Tuple<long, long>, long> cell in cells)
			{
				List<long> objectIds = cell.ToList();

				for (var start = 0; start < objectIds.Count; start += maximumFeaturesPerGroup)
				{
					List<long> chunk = objectIds.Skip(start)
					                            .Take(maximumFeaturesPerGroup)
					                            .ToList();

					PlanarExtent imageExtent =
						chunk.Select(oid => GetCheckWindow(featureEnvelopes[oid]))
						     .Aggregate((union, window) => union.Union(window));

					result.Add(new FeatureCheckGroup(chunk, imageExtent));
				}
			}

			return result;
		}

		[NotNull]
		private static Tuple<long, long> GetCell([NotNull] PlanarExtent envelope,
		                                         double cellSize)
		{
			double centreX = (envelope.XMin + envelope.XMax) / 2;
			double centreY = (envelope.YMin + envelope.YMax) / 2;

			return Tuple.Create((long) Math.Floor(centreX / cellSize),
			                    (long) Math.Floor(centreY / cellSize));
		}
	}
}
