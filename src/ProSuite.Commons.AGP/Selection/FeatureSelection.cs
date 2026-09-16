using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Selection;

public class FeatureSelection : FeatureSelectionBase
{
	private readonly List<Feature> _features;

	public FeatureSelection([NotNull] BasicFeatureLayer featureLayer,
	                        [NotNull] IEnumerable<Feature> features)
		: base(featureLayer)
	{
		Assert.ArgumentNotNull(features, nameof(features));

		_features = features.ToList(); // take ownership

		// Taking ownership is only valid for a non-recycling cursor. With a recycling
		// cursor every entry references the same row, which silently corrupts consumers
		// instead of failing (see FeatureFinder.FindFeaturesByLayer).
		Assert.False(_features.Count > 1 && ReferenceEquals(_features[0], _features[1]),
		             "The features originate from a recycling cursor and cannot be owned " +
		             "by this selection.");
	}

	[NotNull]
	public override IEnumerable<Feature> GetFeatures()
	{
		return _features;
	}

	public override int GetCount()
	{
		return _features.Count;
	}

	/// <remarks>Must run on MCT</remarks>
	public override IEnumerable<long> GetOids()
	{
		return _features.Select(feature => feature.GetObjectID());
	}

	public override string ToString()
	{
		int count = GetCount();
		return $"{BasicFeatureLayer.Name} ({count} feature{(count == 1 ? "" : "s")})";
	}
}
