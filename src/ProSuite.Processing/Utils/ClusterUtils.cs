using System;
using System.Collections.Generic;
using System.Linq;

namespace ProSuite.Processing.Utils
{
	public static class ClusterUtils
	{
		// TODO Somewhere I have code for Lloyd's k-means clustering; it could go here.
		// K-means is an example of an "assignment" clustering algorithm, as opposed
		// to the "hierarchical" algorithm below ("assignment" creates a given number
		// of clusters, "hierarchical" clusters until some condition is met).

		/// <summary>
		/// Perform a hierarchical clustering of the given <paramref name="data"/>
		/// points until no more clusters are closer than <paramref name="maxdist"/>.
		/// </summary>
		/// <remarks>
		/// This method runs in O(N) space and O(N**3) time where N is the number
		/// of data points. The <paramref name="distanceFunc"/> is called O(N**3)
		/// times. The <paramref name="mergeFunc"/> is called at most N times.
		/// <para/>
		/// If <paramref name="maxdist"/> is larger than the maximum distance
		/// between any two data points, the result is one cluster comprising
		/// all data points.
		/// <para/>
		/// If points (or clusters) A and B have exactly the same distance as B and C,
		/// only one pair is merged into a cluster, and it is undefined which pair.
		/// Depending on <paramref name="maxdist"/>, the remaining point/cluster
		/// may or may not be merged with the other two.
		/// For example, if dist(A,B)=dist(B,C) but less than <paramref name="maxdist"/>,
		/// they either be clustered (AB)(C) or (A)(BC); if dist(AB,C) is less than
		/// <paramref name="maxdist"/>, the final result is (ABC), as is the case
		/// if dist(A,BC) is less than <paramref name="maxdist"/>.
		/// </remarks>
		/// <typeparam name="T"></typeparam>
		/// <param name="data">The list of data points</param>
		/// <param name="maxdist">Stop clustering when nearest pair is further apart</param>
		/// <param name="distanceFunc">Function to compute distance between two points</param>
		/// <param name="mergeFunc">Function to merge (cluster) two points</param>
		/// <returns>The list of clustered data points</returns>
		public static IEnumerable<T> Cluster<T>(IEnumerable<T> data, double maxdist,
		                                        Func<T, T, double> distanceFunc,
		                                        Func<T, T, T> mergeFunc) where T : class
		{
			var points = data.ToList();

			int n = points.Count;
			if (n < 2)
			{
				return points;
			}

			int M = n - 1; // max number of merge ops

			for (int m = 0; m < M; m++)
			{
				int i = 0, j = 0;
				double mindist = double.MaxValue;

				for (int k = 0; k < n; k++)
				{
					for (int l = k + 1; l < n; l++)
					{
						double dist = distanceFunc(points[k], points[l]);

						if (dist < mindist)
						{
							i = k;
							j = l;
							mindist = dist;
						}
					}
				}

				if (mindist > maxdist)
				{
					break;
				}

				int u = Math.Min(i, j);
				int v = Math.Max(i, j);

				// Create the cluster in place of the point at the least index:
				points[u] = mergeFunc(points[i], points[j]);
				// Clear the point at the larger index and swap it with the last:
				points[v] = points[n - 1];
				points[n - 1] = null;
				// This decreases the length of the list of points by one:
				n -= 1;
			}

			return points.Where(p => p != null);
		}
	}
}
