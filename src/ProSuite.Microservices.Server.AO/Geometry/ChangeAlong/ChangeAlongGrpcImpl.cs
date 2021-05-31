using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ESRI.ArcGIS.esriSystem;
using Grpc.Core;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Definitions.Geometry;

namespace ProSuite.Microservices.Server.AO.Geometry.ChangeAlong
{
	public class ChangeAlongGrpcImpl : ChangeAlongGrpc.ChangeAlongGrpcBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly StaTaskScheduler _staTaskScheduler;

		public ChangeAlongGrpcImpl(StaTaskScheduler taskScheduler)
		{
			_staTaskScheduler = taskScheduler;
		}

		public override async Task<CalculateReshapeLinesResponse> CalculateReshapeLines(
			[NotNull] CalculateReshapeLinesRequest request,
			[NotNull] ServerCallContext context)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			Func<ITrackCancel, CalculateReshapeLinesResponse> func =
				trackCancel =>
					ChangeAlongServiceUtils.CalculateReshapeLines(request, trackCancel);

			CalculateReshapeLinesResponse response =
				await GrpcServerUtils.ExecuteServiceCall(func, context, _staTaskScheduler, true) ??
				new CalculateReshapeLinesResponse();

			_msg.DebugStopTiming(
				watch, "Calculated reshape lines for peer {0} ({1} source features, {2})",
				context.Peer, request.SourceFeatures.Count, request.TargetFeatures.Count);

			return response;
		}

		public override async Task<CalculateCutLinesResponse> CalculateCutLines(
			[NotNull] CalculateCutLinesRequest request,
			[NotNull] ServerCallContext context)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			Func<ITrackCancel, CalculateCutLinesResponse> func =
				trackCancel =>
					ChangeAlongServiceUtils.CalculateCutLines(request, trackCancel);

			CalculateCutLinesResponse response =
				await GrpcServerUtils.ExecuteServiceCall(func, context, _staTaskScheduler, true) ??
				new CalculateCutLinesResponse();

			_msg.DebugStopTiming(
				watch, "Calculated {0} cut lines for peer {1} ({2} source features, {3})",
				response.CutLines.Count, context.Peer, request.SourceFeatures.Count,
				request.TargetFeatures.Count);

			return response;
		}

		public override async Task<ApplyReshapeLinesResponse> ApplyReshapeLines(
			[NotNull] ApplyReshapeLinesRequest request,
			[NotNull] ServerCallContext context)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			Func<ITrackCancel, ApplyReshapeLinesResponse> func =
				trackCancel =>
					ChangeAlongServiceUtils.ApplyReshapeLines(request, trackCancel);

			ApplyReshapeLinesResponse response =
				await GrpcServerUtils.ExecuteServiceCall(func, context, _staTaskScheduler, true) ??
				new ApplyReshapeLinesResponse();

			_msg.DebugStopTiming(
				watch,
				"Applied reshape lines for peer {0} ({1} source features, {2} reshape lines)",
				context.Peer, request.CalculationRequest.SourceFeatures.Count,
				request.ReshapeLines.Count);

			return response;
		}

		public override async Task<ApplyCutLinesResponse> ApplyCutLines(
			[NotNull] ApplyCutLinesRequest request,
			[NotNull] ServerCallContext context)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			Func<ITrackCancel, ApplyCutLinesResponse> func =
				trackCancel =>
					ChangeAlongServiceUtils.ApplyCutLines(request, trackCancel);

			ApplyCutLinesResponse response =
				await GrpcServerUtils.ExecuteServiceCall(func, context, _staTaskScheduler, true) ??
				new ApplyCutLinesResponse();

			_msg.DebugStopTiming(
				watch,
				"Applied reshape lines for peer {0} ({1} source features, {2} reshape lines)",
				context.Peer, request.CalculationRequest.SourceFeatures.Count,
				request.CutLines.Count);

			return response;
		}
	}
}
