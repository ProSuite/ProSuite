using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Selection
{
	public class FeatureSelection : FeatureSelectionBase
	{
		private readonly IList<Feature> _features;

		public FeatureSelection([NotNull] IList<Feature> features,
		                        [NotNull] BasicFeatureLayer featureLayer)
			: base(featureLayer)
		{
			_features = features;
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

		// daro todo daro to Ienumerable?
		/// <summary>
		/// Does not have to be called on MCT
		/// </summary>
		public override IEnumerable<long> GetOids()
		{
			return new ReadOnlyList<long>(GetFeatures().Select(feature => feature.GetObjectID()).ToList());
		}

		public override string ToString()
		{
			return $"{BasicFeatureLayer.Name}";
		}
	}
}
