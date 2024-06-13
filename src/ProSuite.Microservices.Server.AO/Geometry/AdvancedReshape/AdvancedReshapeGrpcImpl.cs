using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ESRI.ArcGIS.esriSystem;
using Grpc.Core;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Server.AO.Geometry.AdvancedReshape
{
	public class AdvancedReshapeGrpcImpl : ReshapeGrpc.ReshapeGrpcBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly StaTaskScheduler _staTaskScheduler;

		public AdvancedReshapeGrpcImpl(StaTaskScheduler taskScheduler)
		{
			_staTaskScheduler = taskScheduler;
		}

		public override async Task<AdvancedReshapeResponse> AdvancedReshape(
			AdvancedReshapeRequest request, ServerCallContext context)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			ProcessUtils.TrySetThreadIdAsName();

			Func<ITrackCancel, AdvancedReshapeResponse> func =
				trackCancel => AdvancedReshapeServiceUtils.Reshape(request);

			AdvancedReshapeResponse response =
				await GrpcServerUtils.ExecuteServiceCall(func, context, _staTaskScheduler, true) ??
				new AdvancedReshapeResponse();

			_msg.DebugStopTiming(watch, "Reshaped for peer {0} ({1} source features)",
			                     context.Peer, request.Features.Count);

			return response;
		}

		public override async Task<ShapeMsg> GetOpenJawReshapeLineReplaceEndPoint(
			OpenJawReshapeLineReplacementRequest request,
			ServerCallContext context)
		{
			ProcessUtils.TrySetThreadIdAsName();

			Func<ITrackCancel, ShapeMsg> func =
				trackCancel => AdvancedReshapeServiceUtils.GetOpenJawReshapeReplaceEndPoint(
					request, trackCancel);

			ShapeMsg response =
				await GrpcServerUtils.ExecuteServiceCall(func, context, _staTaskScheduler, true) ??
				new ShapeMsg();

			return response;
		}
	}
}
