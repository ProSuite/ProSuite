using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using ESRI.ArcGIS.esriSystem;
using Grpc.Core;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Definitions.Geometry;

namespace ProSuite.Microservices.Server.AO.Geometry.RemoveOverlaps
{
	public class RemoveOverlapsGrpcImpl : RemoveOverlapsGrpc.RemoveOverlapsGrpcBase
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly StaTaskScheduler _staTaskScheduler;

		public RemoveOverlapsGrpcImpl(StaTaskScheduler taskScheduler)
		{
			_staTaskScheduler = taskScheduler;
		}

		[NotNull]
		public override async Task<CalculateOverlapsResponse> CalculateOverlaps(
			CalculateOverlapsRequest request, ServerCallContext context)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			Func<ITrackCancel, CalculateOverlapsResponse> func =
				trackCancel => RemoveOverlapsServiceUtils.CalculateOverlaps(request, trackCancel);

			CalculateOverlapsResponse response =
				await GrpcServerUtils.ExecuteServiceCall(func, context, _staTaskScheduler, true) ??
				new CalculateOverlapsResponse();

			_msg.DebugStopTiming(watch, "Calculated overlaps for peer {0} ({1} source features)",
			                     context.Peer, request.SourceFeatures.Count);

			return Assert.NotNull(response);
		}

		public override async Task<RemoveOverlapsResponse> RemoveOverlaps(
			RemoveOverlapsRequest request, ServerCallContext context)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			Func<ITrackCancel, RemoveOverlapsResponse> func =
				trackCancel => RemoveOverlapsServiceUtils.RemoveOverlaps(request, trackCancel);

			RemoveOverlapsResponse response =
				await GrpcServerUtils.ExecuteServiceCall(func, context, _staTaskScheduler, true) ??
				new RemoveOverlapsResponse();

			_msg.DebugStopTiming(watch, "Removed overlaps for peer {0} ({1} source features)",
			                     context.Peer, request.SourceFeatures.Count);

			return response;
		}
	}
}
