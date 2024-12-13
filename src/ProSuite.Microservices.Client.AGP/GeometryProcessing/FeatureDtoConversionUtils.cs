using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Definitions.Shared.Gdb;

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

			RowChangeType changeType = ToChangeType(resultFeatureMsg.FeatureCase);

			Func<SpatialReference, Geometry> getGeometryFunc =
				sr => GetGeometry(sr, changeType, resultFeatureMsg);

			var reshapeResultFeature =
				new ResultFeature(inputFeature,
				                  getGeometryFunc,
				                  changeType,
				                  resultFeatureMsg.HasWarning,
				                  resultFeatureMsg.Notifications)
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

		private static Geometry GetGeometry([NotNull] SpatialReference expectedSpatialRef,
		                                    RowChangeType changeType,
		                                    ResultObjectMsg resultObjMsg)
		{
			GdbObjectMsg gdbObjectMsg;

			if (changeType == RowChangeType.Update)
			{
				gdbObjectMsg = resultObjMsg.Update;
			}
			else if (changeType == RowChangeType.Insert)
			{
				gdbObjectMsg = resultObjMsg.Insert.InsertedObject;
			}
			else
			{
				throw new InvalidOperationException("Cannot get new geometry of delete");
			}

			Assert.True(
				IsExpectedSpatialRef(expectedSpatialRef, gdbObjectMsg.Shape.SpatialReference),
				"Unexpected spatial reference in result feature: {0}. Expected: {1}",
				gdbObjectMsg.Shape.SpatialReference, expectedSpatialRef.Name);

			return
				ProtobufConversionUtils.FromShapeMsg(gdbObjectMsg.Shape, expectedSpatialRef);
		}

		private static RowChangeType ToChangeType(ResultObjectMsg.FeatureOneofCase featureCase)
		{
			switch (featureCase)
			{
				case ResultObjectMsg.FeatureOneofCase.Insert: return RowChangeType.Insert;
				case ResultObjectMsg.FeatureOneofCase.Update: return RowChangeType.Update;
				case ResultObjectMsg.FeatureOneofCase.Delete: return RowChangeType.Delete;
				default:
					throw new ArgumentOutOfRangeException(nameof(featureCase),
					                                      $"Unsupported change type: {featureCase}");
			}
		}

		private static bool IsExpectedSpatialRef([CanBeNull] SpatialReference expected,
		                                         SpatialReferenceMsg spatialReferenceMsg)
		{
			if (expected == null)
			{
				// No expectations:
				return true;
			}

			if (spatialReferenceMsg.FormatCase ==
			    SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkid)
			{
				return expected.Wkid == spatialReferenceMsg.SpatialReferenceWkid;
			}

			if (spatialReferenceMsg.FormatCase ==
			    SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml)
			{
				string xml = spatialReferenceMsg.SpatialReferenceEsriXml;

				SpatialReference actual =
					SpatialReferenceBuilder.FromXml(Assert.NotNullOrEmpty(xml));

				return expected.Wkid == actual.Wkid;
			}

			if (spatialReferenceMsg.FormatCase ==
			    SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkt)
			{
				SpatialReference actual = SpatialReferenceBuilder.CreateSpatialReference(
					Assert.NotNullOrEmpty(spatialReferenceMsg.SpatialReferenceWkt));

				return expected.Wkid == actual.Wkid;
			}

			return true;
		}
	}
}
