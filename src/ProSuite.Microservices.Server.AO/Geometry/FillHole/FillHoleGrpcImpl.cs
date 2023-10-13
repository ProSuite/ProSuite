using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ESRI.ArcGIS.esriSystem;
using Grpc.Core;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Definitions.Geometry;

namespace ProSuite.Microservices.Server.AO.Geometry.FillHole
{
	public class FillHoleGrpcImpl : FillHolesGrpc.FillHolesGrpcBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly StaTaskScheduler _staTaskScheduler;

		public FillHoleGrpcImpl(StaTaskScheduler taskScheduler)
		{
			_staTaskScheduler = taskScheduler;
		}

		#region Overrides of RemoveHolesGrpcBase

		public override async Task<CalculateHolesResponse> CalculateHoles(
			CalculateHolesRequest request, ServerCallContext context)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			ProcessUtils.TrySetThreadIdAsName();

			Func<ITrackCancel, CalculateHolesResponse> func =
				trackCancel => FillHoleServiceUtils.CalculateHoles(request, trackCancel);

			CalculateHolesResponse response =
				await GrpcServerUtils.ExecuteServiceCall(func, context, _staTaskScheduler, true) ??
				new CalculateHolesResponse();

			_msg.DebugStopTiming(watch, "Calculated holes for peer {0} ({1} source features)",
			                     context.Peer, request.SourceFeatures.Count);

			return Assert.NotNull(response);
		}

		#endregion
	}
}
