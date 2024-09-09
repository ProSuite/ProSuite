using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ESRI.ArcGIS.esriSystem;
using Grpc.Core;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Definitions.Geometry;

namespace ProSuite.Microservices.Server.AO.Geometry.Cracker
{
	public class CrackServiceGrpcImpl : CrackGrpc.CrackGrpcBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly StaTaskScheduler _staTaskScheduler;

		public CrackServiceGrpcImpl(StaTaskScheduler taskScheduler)
		{
			_staTaskScheduler = taskScheduler;
		}

		#region Overrides of CrackGrpcBase

		[NotNull]
		public override async Task<CalculateCrackPointsResponse> CalculateCrackPoints(
			CalculateCrackPointsRequest request, ServerCallContext context)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			ProcessUtils.TrySetThreadIdAsName();

			Func<ITrackCancel, CalculateCrackPointsResponse> func =
				trackCancel => CrackServiceUtils.CalculateCrackPoints(request, trackCancel);

			CalculateCrackPointsResponse response =
				await GrpcServerUtils.ExecuteServiceCall(func, context, _staTaskScheduler, true) ??
				new CalculateCrackPointsResponse();

			_msg.DebugStopTiming(
				watch, "Calculated crack points for peer {0} ({1} source feature(s))",
				context.Peer, request.SourceFeatures.Count);

			return response;
		}

		public override async Task<ApplyCrackPointsResponse> ApplyCrackPoints(
			ApplyCrackPointsRequest request, ServerCallContext context)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			ProcessUtils.TrySetThreadIdAsName();

			Func<ITrackCancel, ApplyCrackPointsResponse> func =
				trackCancel => CrackServiceUtils.ApplyCrackPoints(request, trackCancel);

			ApplyCrackPointsResponse response =
				await GrpcServerUtils.ExecuteServiceCall(func, context, _staTaskScheduler, true) ??
				new ApplyCrackPointsResponse();

			_msg.DebugStopTiming(watch, "Applied crack points for peer {0} ({1} source feature(s))",
			                     context.Peer, request.SourceFeatures.Count);

			return response;
		}

		#endregion
	}
}
