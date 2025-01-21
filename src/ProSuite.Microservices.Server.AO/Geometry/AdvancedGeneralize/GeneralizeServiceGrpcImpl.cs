using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ESRI.ArcGIS.esriSystem;
using Grpc.Core;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Definitions.Geometry;

namespace ProSuite.Microservices.Server.AO.Geometry.AdvancedGeneralize
{
	public class GeneralizeServiceGrpcImpl : GeneralizeGrpc.GeneralizeGrpcBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly StaTaskScheduler _staTaskScheduler;

		public GeneralizeServiceGrpcImpl(StaTaskScheduler taskScheduler)
		{
			_staTaskScheduler = taskScheduler;
		}

		#region Overrides of GeneralizeGrpcBase

		public override async Task<CalculateRemovableSegmentsResponse> CalculateRemovableSegments(
			CalculateRemovableSegmentsRequest request, ServerCallContext context)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			ProcessUtils.TrySetThreadIdAsName();

			Func<ITrackCancel, CalculateRemovableSegmentsResponse> func =
				trackCancel => GeneralizeServiceUtils.CalculateRemovableSegments(
					request, trackCancel);

			CalculateRemovableSegmentsResponse response =
				await GrpcServerUtils.ExecuteServiceCall(func, context, _staTaskScheduler, true) ??
				new CalculateRemovableSegmentsResponse();

			_msg.DebugStopTiming(
				watch, "Calculated removable segments for peer {0} ({1} source feature(s))",
				context.Peer, request.SourceFeatures.Count);

			return response;
		}

		public override async Task<ApplySegmentRemovalResponse> ApplySegmentRemoval(
			ApplySegmentRemovalRequest request, ServerCallContext context)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			ProcessUtils.TrySetThreadIdAsName();

			Func<ITrackCancel, ApplySegmentRemovalResponse> func =
				trackCancel => GeneralizeServiceUtils.ApplySegmentRemoval(request, trackCancel);

			ApplySegmentRemovalResponse response =
				await GrpcServerUtils.ExecuteServiceCall(func, context, _staTaskScheduler, true) ??
				new ApplySegmentRemovalResponse();

			_msg.DebugStopTiming(watch, "Applied crack points for peer {0} ({1} source feature(s))",
			                     context.Peer, request.SourceFeatures.Count);

			return response;
		}

		#endregion
	}
}
