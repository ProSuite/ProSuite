using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing.AdvancedReshape
{
	public class ReshapeResultFeature
	{
		private readonly ResultObjectMsg _resultFeatureMsg;

		public Feature Feature { get; }

		private Geometry _updatedGeometry;

		public ReshapeResultFeature(Feature feature,
		                            ResultObjectMsg resultFeatureMsg)
		{
			_resultFeatureMsg = resultFeatureMsg;
			Feature = feature;
		}

		/// <summary>
		/// The updated geometry. Must be called in a queued task (at least the first time)
		/// </summary>
		public Geometry UpdatedGeometry
		{
			get
			{
				if (_updatedGeometry == null)
				{
					SpatialReference knownSr = Feature.GetShape().SpatialReference;

					_updatedGeometry =
						ProtobufConversionUtils.FromShapeMsg(_resultFeatureMsg.Update.Shape,
						                                     knownSr);
				}

				return _updatedGeometry;
			}
		}

		public IList<string> Messages => _resultFeatureMsg.Notifications;

		public bool HasWarningMessage => _resultFeatureMsg.HasWarning;
	}
}
