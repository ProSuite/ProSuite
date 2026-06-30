using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Selection;

public class FeatureSelection : FeatureSelectionBase
{
	private readonly List<Feature> _features;

	public FeatureSelection([NotNull] BasicFeatureLayer featureLayer,
	                        [NotNull] IEnumerable<Feature> features)
		: base(featureLayer)
	{
		if (features is null)
			throw new ArgumentNullException(nameof(features));

		_features = features.ToList(); // take ownership
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
