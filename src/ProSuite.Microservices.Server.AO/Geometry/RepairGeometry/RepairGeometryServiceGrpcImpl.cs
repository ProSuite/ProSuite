using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ESRI.ArcGIS.esriSystem;
using Grpc.Core;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Definitions.Geometry;

namespace ProSuite.Microservices.Server.AO.Geometry.RepairGeometry
{
	public class RepairGeometryServiceGrpcImpl : RepairGeometryGrpc.RepairGeometryGrpcBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly StaTaskScheduler _staTaskScheduler;

		public RepairGeometryServiceGrpcImpl(StaTaskScheduler taskScheduler)
		{
			_staTaskScheduler = taskScheduler;
		}

		#region Overrides of RepairGeometryGrpcBase

		public override async Task<CalculateRepairInfoResponse> CalculateRepairInfo(
			CalculateRepairInfoRequest request, ServerCallContext context)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			ProcessUtils.TrySetThreadIdAsName();

			Func<ITrackCancel, CalculateRepairInfoResponse> func =
				trackCancel => RepairGeometryServiceUtils.CalculateRepairInfo(request, trackCancel);

			CalculateRepairInfoResponse response =
				await GrpcServerUtils.ExecuteServiceCall(func, context, _staTaskScheduler, true) ??
				new CalculateRepairInfoResponse();

			_msg.DebugStopTiming(watch,
			                     "Calculated repair info for peer {0} ({1} source feature(s))",
			                     context.Peer, request.SourceFeatures.Count);

			return response;
		}

		public override async Task<ApplyRepairGeometryResponse> ApplyRepairGeometry(
			ApplyRepairGeometryRequest request, ServerCallContext context)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			ProcessUtils.TrySetThreadIdAsName();

			Func<ITrackCancel, ApplyRepairGeometryResponse> func =
				trackCancel => RepairGeometryServiceUtils.ApplyRepairGeometry(request, trackCancel);

			ApplyRepairGeometryResponse response =
				await GrpcServerUtils.ExecuteServiceCall(func, context, _staTaskScheduler, true) ??
				new ApplyRepairGeometryResponse();

			_msg.DebugStopTiming(watch,
			                     "Applied repair geometry for peer {0} ({1} source feature(s))",
			                     context.Peer, request.SourceFeatures.Count);

			return response;
		}

		#endregion
	}
}
