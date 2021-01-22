using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	public class OverlappingMeasures
	{
		private readonly HashSet<TestRowReference> _features =
			new HashSet<TestRowReference>();

		/// <summary>
		/// Initializes a new instance of the <see cref="OverlappingMeasures"/> class.
		/// </summary>
		/// <param name="routeId">The route id.</param>
		/// <param name="mMin">The m min.</param>
		/// <param name="mMax">The m max.</param>
		public OverlappingMeasures([NotNull] object routeId, double mMin, double mMax)
		{
			RouteId = routeId;

			MMin = mMin;
			MMax = mMax;
		}

		public void Add([NotNull] TestRowReference testRowReference)
		{
			_features.Add(testRowReference);
		}

		[NotNull]
		public object RouteId { get; }

		public double MMin { get; }

		public double MMax { get; }

		[NotNull]
		public IEnumerable<TestRowReference> Features => _features;

		public override string ToString()
		{
			return string.Format("RouteId: {0}, MMin: {1}, MMax: {2}, Features: {3}",
			                     RouteId, MMin, MMax, _features.Count);
		}
	}
}
