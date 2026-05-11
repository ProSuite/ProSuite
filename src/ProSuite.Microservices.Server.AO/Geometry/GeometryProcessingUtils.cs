using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Server.AO.Geometry
{
	public static class GeometryProcessingUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Returns a display label for the feature suitable for user-facing messages.
		/// Unlike <see cref="GdbObjectUtils.ToString(ESRI.ArcGIS.Geodatabase.IObject)"/>,
		/// this reads the OID directly, which works for protobuf-reconstructed features whose
		/// class declares <c>HasOID = false</c> but still carry a valid object ID.
		/// </summary>
		public static string GetGdbObjectLabel([NotNull] IFeature feature)
		{
			string className;
			try
			{
				className = DatasetUtils.GetAliasName(feature.Class);
			}
			catch (Exception)
			{
				className = "[unknown class]";
			}

			string oid;
			try
			{
				oid = feature.OID.ToString(CultureInfo.InvariantCulture);
			}
			catch (Exception)
			{
				oid = "[n/a]";
			}

			return $"{className} oid {oid}";
		}

		public static void GetFeatures([NotNull] ICollection<GdbObjectMsg> requestSourceFeatures,
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

			if (_msg.IsVerboseDebugEnabled)
			{
				string sourceList =
					StringUtils.Concatenate(sourceFeatures, f => $"{f.Class.AliasName} {f.OID}",
					                        ",");
				string targetList =
					StringUtils.Concatenate(targetFeatures, f => $"{f.Class.AliasName} {f.OID}",
					                        ",");

				_msg.Debug($"Source features: {sourceList}");
				_msg.Debug($"Target features: {targetList}");
			}
		}
	}
}
