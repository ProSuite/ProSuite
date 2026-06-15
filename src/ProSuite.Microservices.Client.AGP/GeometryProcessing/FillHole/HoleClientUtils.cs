using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing.Holes;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Definitions.Geometry;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing.FillHole
{
	public static class HoleClientUtils
	{
		[CanBeNull]
		public static IList<Holes> CalculateHoles(FillHolesGrpc.FillHolesGrpcClient rpcClient,
		                                          [NotNull] IList<Feature> selectedFeatures,
		                                          [NotNull] IList<Envelope> clipEnvelopes,
		                                          bool unionFeatures,
		                                          CancellationToken cancellationToken)
		{
			CalculateHolesResponse response =
				CalculateHolesRpc(rpcClient, selectedFeatures, clipEnvelopes, unionFeatures,
				                  cancellationToken);

			if (response == null || cancellationToken.IsCancellationRequested)
			{
				return null;
			}

			var result = new List<Holes>();

			// Get the spatial reference from a shape (== map spatial reference) rather than a feature class.
			SpatialReference spatialReference = selectedFeatures
			                                    .Select(f => f.GetShape().SpatialReference)
			                                    .FirstOrDefault();

			foreach (HolesMsg holesMsg in response.Holes)
			{
				GdbObjectReference? gdbObjRef = null;

				if (holesMsg.OriginalFeatureRef != null)
				{
					gdbObjRef = new GdbObjectReference(
						holesMsg.OriginalFeatureRef.ClassHandle,
						holesMsg.OriginalFeatureRef.ObjectId);
				}

				List<Polygon> holeGeometries =
					ProtobufConversionUtils.FromShapeMsgList(
						holesMsg.HoleGeometries, spatialReference).Cast<Polygon>().ToList();

				result.Add(new Holes(holeGeometries, gdbObjRef));
			}

			return result;
		}

		[CanBeNull]
		private static CalculateHolesResponse CalculateHolesRpc(
			FillHolesGrpc.FillHolesGrpcClient rpcClient,
			ICollection<Feature> selectedFeatures,
			IList<Envelope> clipEnvelopes,
			bool unionFeatures,
			CancellationToken cancellationToken)
		{
			Assert.ArgumentCondition(selectedFeatures.Count > 0, "No selection");

			IEnumerable<Feature> intersectingFeatures =
				clipEnvelopes.Count == 0
					? selectedFeatures
					: selectedFeatures.Where(f => IntersectsAny(f.GetShape(), clipEnvelopes));

			CalculateHolesRequest request =
				CreateCalculateHolesRequest(intersectingFeatures, clipEnvelopes);

			request.UnionFeatures = unionFeatures;

			int deadline = FeatureProcessingUtils.GetProcessingTimeout(selectedFeatures.Count);

			CalculateHolesResponse response =
				GrpcClientUtils.Try(
					o => rpcClient.CalculateHoles(request, o),
					cancellationToken, deadline);

			return response;
		}

		private static bool IntersectsAny([NotNull] Geometry geometry,
		                                  [NotNull] IEnumerable<Envelope> envelopes)
		{
			return envelopes.Any(envelope => GeometryUtils.Intersects(geometry, envelope));
		}

		private static CalculateHolesRequest CreateCalculateHolesRequest(
			[NotNull] IEnumerable<Feature> selectedFeatures,
			[NotNull] IList<Envelope> clipEnvelopes)
		{
			var request = new CalculateHolesRequest();

			ProtobufConversionUtils.ToGdbObjectMsgList(selectedFeatures,
			                                           request.SourceFeatures,
			                                           request.ClassDefinitions);

			foreach (Envelope clipEnvelope in clipEnvelopes)
			{
				var envelopeMsg =
					ProtobufGeomUtils.ToEnvelopeMsg(
						GeomConversionUtils.CreateEnvelopeXY(clipEnvelope));
				request.VisibleExtents.Add(envelopeMsg);
			}

			return request;
		}
	}
}
