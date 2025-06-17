using System.Collections.Generic;
using System.Diagnostics;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using Google.Protobuf.Collections;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Server.AO.Geometry.FillHole
{
	public static class FillHoleServiceUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static CalculateHolesResponse CalculateHoles(
			[NotNull] CalculateHolesRequest request,
			[CanBeNull] ITrackCancel trackCancel)
		{
			var watch = Stopwatch.StartNew();

			GetFeatures(request.SourceFeatures, request.ClassDefinitions,
						out IList<IFeature> sourceFeatures);

			_msg.DebugStopTiming(watch, "Unpacked feature lists from request params");

			var result = new CalculateHolesResponse();

			if (request.UnionFeatures)
			{
				HolesMsg holeMsg = CalculateHoleMsg(sourceFeatures, request.VisibleExtents, trackCancel);
				result.Holes.Add(holeMsg);
			}
			else
			{
				// Feature-by-feature:
				foreach (IFeature feature in sourceFeatures)
				{
					HolesMsg holeMsg =
						CalculateHoleMsg(new List<IFeature> { feature }, request.VisibleExtents,
										 trackCancel);
					holeMsg.OriginalFeatureRef = ProtobufGdbUtils.ToGdbObjRefMsg(feature);
					result.Holes.Add(holeMsg);
				}
			}

			return result;
		}

		private static HolesMsg CalculateHoleMsg(IList<IFeature> sourceFeatures,
												 ICollection<EnvelopeMsg> envelopeMsgs,
												 ITrackCancel trackCancel)
		{
			List<IEnvelope> envelopes = null;

			if (envelopeMsgs != null)
			{
				envelopes = new List<IEnvelope>(envelopeMsgs.Count);

				foreach (EnvelopeMsg envelopeMsg in envelopeMsgs)
				{
					IEnvelope envelope = ProtobufGeometryUtils.FromEnvelopeMsg(envelopeMsg);
					envelopes.Add(envelope);
				}
			}

			IList<IPolygon> holes = FillHoleUtils.CalculateHoles(
				sourceFeatures, envelopes, trackCancel);

			var watch = Stopwatch.StartNew();

			var holeMsg = new HolesMsg();

			var shapeFormat = ShapeMsg.FormatOneofCase.EsriShape;
			var srFormat = SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml;

			foreach (IPolygon holePolygon in holes)
			{
				holeMsg.HoleGeometries.Add(ProtobufGeometryUtils.ToShapeMsg(
											   holePolygon, shapeFormat, srFormat));
			}

			_msg.DebugStopTiming(watch, "Packed holes into response");
			return holeMsg;
		}

		private static void GetFeatures([NotNull] RepeatedField<GdbObjectMsg> requestSourceFeatures,
										[NotNull] RepeatedField<ObjectClassMsg> classDefinitions,
										[NotNull] out IList<IFeature> sourceFeatures)
		{
			Stopwatch watch = Stopwatch.StartNew();

			sourceFeatures = ProtobufConversionUtils.FromGdbObjectMsgList(requestSourceFeatures,
				classDefinitions);

			_msg.DebugStopTiming(
				watch,
				"GetFeatures: Unpacked {0} source features from request params",
				sourceFeatures.Count);
		}
	}
}
