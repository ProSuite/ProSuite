using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing
{
	public static class FeatureDtoConversionUtils
	{
		public static IEnumerable<ResultFeature> FromUpdateMsgs(
			[NotNull] IEnumerable<ResultObjectMsg> resultFeatureMsgs,
			[NotNull] IReadOnlyDictionary<GdbObjectReference, Feature> allInputFeatures,
			[CanBeNull] SpatialReference resultSpatialReference)
		{
			foreach (ResultObjectMsg resultFeatureMsg in resultFeatureMsgs)
			{
				yield return FromResultObjectMsg(resultFeatureMsg, allInputFeatures,
				                                 resultSpatialReference);
			}
		}

		public static ResultFeature FromResultObjectMsg(
			[NotNull] ResultObjectMsg resultFeatureMsg,
			[NotNull] IReadOnlyDictionary<GdbObjectReference, Feature> allInputFeatures,
			[CanBeNull] SpatialReference resultSpatialReference)
		{
			GdbObjectReference objRef = GetOriginalGdbObjectReference(resultFeatureMsg);

			Feature inputFeature = allInputFeatures[objRef];

			var reshapeResultFeature = new ResultFeature(inputFeature, resultFeatureMsg)
			                           {
				                           KnownResultSpatialReference = resultSpatialReference
			                           };

			return reshapeResultFeature;
		}

		private static GdbObjectReference GetOriginalGdbObjectReference(
			[NotNull] ResultObjectMsg resultObjectMsg)
		{
			Assert.ArgumentNotNull(nameof(resultObjectMsg));

			long classHandle, objectId;

			if (resultObjectMsg.FeatureCase == ResultObjectMsg.FeatureOneofCase.Insert)
			{
				InsertedObjectMsg insert = Assert.NotNull(resultObjectMsg.Insert);

				GdbObjRefMsg originalObjRefMsg = insert.OriginalReference;

				classHandle = originalObjRefMsg.ClassHandle;
				objectId = originalObjRefMsg.ObjectId;
			}
			else
			{
				GdbObjectMsg updateMsg = Assert.NotNull(resultObjectMsg.Update);

				classHandle = updateMsg.ClassHandle;
				objectId = updateMsg.ObjectId;
			}

			return new GdbObjectReference(classHandle, objectId);
		}
	}
}
