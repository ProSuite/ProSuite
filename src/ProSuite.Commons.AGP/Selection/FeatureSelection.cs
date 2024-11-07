using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Selection
{
	public class FeatureSelection : FeatureSelectionBase
	{
		private readonly IList<Feature> _features;

		public FeatureSelection([NotNull] BasicFeatureLayer featureLayer,
		                        [NotNull] IList<Feature> features)
			: base(featureLayer)
		{
			_features = features ?? throw new ArgumentNullException(nameof(features));
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

		/// <summary>
		/// Does not have to be called on MCT
		/// </summary>
		public override IEnumerable<long> GetOids()
		{
			return GetFeatures().Select(feature => feature.GetObjectID());
		}

		public override string ToString()
		{
			return BasicFeatureLayer.Name;
		}
	}
}
