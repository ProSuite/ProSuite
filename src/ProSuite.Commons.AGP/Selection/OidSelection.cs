using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AGP.Selection
{
	public class OidSelection : FeatureSelectionBase
	{
		private readonly SpatialReference _outputSpatialReference;
		private readonly IList<long> _objectIds;

		public OidSelection([NotNull] BasicFeatureLayer featureLayer,
		                    [NotNull] IList<long> objectIds,
							[CanBeNull] SpatialReference outputSpatialReference)
			: base(featureLayer)
		{
			_objectIds = objectIds ?? throw new ArgumentNullException(nameof(objectIds));
			_outputSpatialReference = outputSpatialReference;
		}

		public override int GetCount()
		{
			return _objectIds.Count;
		}

		// todo daro move to base?
		public override IEnumerable<long> GetOids()
		{
			return new ReadOnlyList<long>(_objectIds);
		}

		public override string ToString()
		{
			return
				$"{BasicFeatureLayer.Name}, {StringUtils.Concatenate(_objectIds.OrderBy(id => id), "; ")}";
		}
	}
}
