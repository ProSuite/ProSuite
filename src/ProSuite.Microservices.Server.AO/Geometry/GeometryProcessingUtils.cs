using System.Collections.Generic;
using System.Diagnostics;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Server.AO.Geometry
{
	internal static class GeometryProcessingUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		internal static void GetFeatures([NotNull] ICollection<GdbObjectMsg> requestSourceFeatures,
		                                 [NotNull] ICollection<GdbObjectMsg> requestTargetFeatures,
		                                 [NotNull] ICollection<ObjectClassMsg> classDefinitions,
		                                 [NotNull] out IList<IFeature> sourceFeatures,
		                                 [NotNull] out IList<IFeature> targetFeatures)
		{
			Stopwatch watch = Stopwatch.StartNew();

			sourceFeatures = ProtobufConversionUtils.FromGdbObjectMsgList(requestSourceFeatures,
				classDefinitions);

			targetFeatures = ProtobufConversionUtils.FromGdbObjectMsgList(requestTargetFeatures,
				classDefinitions);

			_msg.DebugStopTiming(
				watch,
				"GetFeatures: Unpacked {0} source and {1} target features from request params",
				sourceFeatures.Count, targetFeatures.Count);
		}
	}
}