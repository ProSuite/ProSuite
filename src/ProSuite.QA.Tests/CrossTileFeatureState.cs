using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
	public abstract class CrossTileFeatureState<T> where T : PendingFeature
	{
		private readonly IDictionary<int, HashSet<int>> _featuresKnownOK =
			new Dictionary<int, HashSet<int>>();

		private readonly IDictionary<int, IDictionary<int, T>>
			_suspiciousFeaturesByTableIndex =
				new Dictionary<int, IDictionary<int, T>>();

		private readonly IEnvelope _envelopeTemplate = new EnvelopeClass();

		public void FlagFeatureAsOK(int tableIndex, [NotNull] IFeature feature)
		{
			IDictionary<int, T> pendingFeatures;
			if (_suspiciousFeaturesByTableIndex.TryGetValue(tableIndex, out pendingFeatures))
			{
				pendingFeatures.Remove(feature.OID);
			}

			HashSet<int> oids;
			if (! _featuresKnownOK.TryGetValue(tableIndex, out oids))
			{
				oids = new HashSet<int>();
				_featuresKnownOK.Add(tableIndex, oids);
			}

			oids.Add(feature.OID);
		}

		public bool IsFeatureKnownOK(int tableIndex, int oid)
		{
			HashSet<int> oids;
			return _featuresKnownOK.TryGetValue(tableIndex, out oids) &&
			       oids.Contains(oid);
		}

		public void FlagFeatureAsSuspicious(int tableIndex,
		                                    [NotNull] IFeature feature,
		                                    [NotNull] out T pendingFeature)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			IDictionary<int, T> pendingFeatures;
			if (! _suspiciousFeaturesByTableIndex.TryGetValue(tableIndex, out pendingFeatures))
			{
				pendingFeatures = new Dictionary<int, T>();
				_suspiciousFeaturesByTableIndex.Add(tableIndex, pendingFeatures);
			}

			int oid = feature.OID;
			if (! pendingFeatures.TryGetValue(oid, out pendingFeature))
			{
				pendingFeature = GetPendingFeature(feature);
				pendingFeatures.Add(oid, pendingFeature);
			}
		}

		public int ReportErrors(
			[NotNull] Func<int, ICollection<T>, int> reportErrors,
			[NotNull] IEnvelope tileEnvelope,
			[CanBeNull] IEnvelope testRunEnvelope)
		{
			int errorCount = 0;
			foreach (
				KeyValuePair<int, IDictionary<int, T>> pair in
				_suspiciousFeaturesByTableIndex)
			{
				int tableIndex = pair.Key;
				IDictionary<int, T> suspiciousFeatures = pair.Value;

				var oidsToRemove = new List<int>();
				var errorFeatures = new List<T>();

				foreach (T suspiciousFeature in suspiciousFeatures.Values)
				{
					if (! suspiciousFeature.IsFullyChecked(tileEnvelope, testRunEnvelope))
					{
						continue;
					}

					// the suspect feature was fully checked -> guilty
					errorFeatures.Add(suspiciousFeature);
					oidsToRemove.Add(suspiciousFeature.OID);
				}

				errorCount += reportErrors(tableIndex, errorFeatures);

				foreach (int oid in oidsToRemove)
				{
					suspiciousFeatures.Remove(oid);
				}

				PurgeFeaturesKnownOK(tableIndex, oidsToRemove);
			}

			return errorCount;
		}

		private void PurgeFeaturesKnownOK(int tableIndex,
		                                  [NotNull] IEnumerable<int> oidsToRemove)
		{
			HashSet<int> oids;
			if (! _featuresKnownOK.TryGetValue(tableIndex, out oids))
			{
				return;
			}

			foreach (int oid in oidsToRemove)
			{
				oids.Remove(oid);
			}
		}

		[NotNull]
		private T GetPendingFeature([NotNull] IFeature feature)
		{
			feature.Shape.QueryEnvelope(_envelopeTemplate);

			double xMin;
			double yMin;
			double xMax;
			double yMax;
			_envelopeTemplate.QueryCoords(out xMin, out yMin, out xMax, out yMax);

			return CreatePendingFeature(feature, xMin, yMin, xMax, yMax);
		}

		[NotNull]
		protected abstract T CreatePendingFeature([NotNull] IFeature feature,
		                                          double xMin, double yMin,
		                                          double xMax, double yMax);
	}
}
