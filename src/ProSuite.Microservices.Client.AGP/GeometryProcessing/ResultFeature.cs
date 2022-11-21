using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing
{
	public class ResultFeature
	{
		[NotNull] private readonly ResultObjectMsg _resultFeatureMsg;

		[NotNull]
		public Feature OriginalFeature { get; }

		private Geometry _newGeometry;
		private long _newObjectId;

		public ResultFeature([NotNull] Feature originalFeature,
		                     [NotNull] ResultObjectMsg resultFeatureMsg)
		{
			_resultFeatureMsg = resultFeatureMsg;
			OriginalFeature = originalFeature;
			ChangeType = ToChangeType(_resultFeatureMsg.FeatureCase);
			HasWarningMessage = _resultFeatureMsg.HasWarning;
		}

		/// <summary>
		/// The known spatial reference of the result feature msg provided in the constructor.
		/// It will be used when creating the new geometry. It must be set if the calculation
		/// (map) SR is different from the feature class' spatial reference.
		/// </summary>
		public SpatialReference KnownResultSpatialReference { get; set; }

		public RowChangeType ChangeType { get; }

		/// <summary>
		/// The new geometry of an update or insert. Must be called in a queued task (at least the first time)
		/// </summary>
		public Geometry NewGeometry
		{
			get
			{
				if (_newGeometry == null)
				{
					GdbObjectMsg gdbObjectMsg;

					if (ChangeType == RowChangeType.Update)
					{
						gdbObjectMsg = _resultFeatureMsg.Update;
					}
					else if (ChangeType == RowChangeType.Insert)
					{
						gdbObjectMsg = _resultFeatureMsg.Insert.InsertedObject;
					}
					else
					{
						throw new InvalidOperationException("Cannot get new geometry of delete");
					}

					SpatialReference expectedSr =
						KnownResultSpatialReference ?? OriginalFeature.GetShape().SpatialReference;

					Assert.True(
						IsExpectedSpatialRef(expectedSr, gdbObjectMsg.Shape.SpatialReference),
						"Unexpected spatial reference in result feature: {0}. Expected: {1}",
						gdbObjectMsg.Shape.SpatialReference, expectedSr.Name);

					_newGeometry =
						ProtobufConversionUtils.FromShapeMsg(gdbObjectMsg.Shape, expectedSr);
				}

				return _newGeometry;
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
					SpatialReferenceBuilder.FromXML(Assert.NotNullOrEmpty(xml));

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

		public IList<string> Messages => _resultFeatureMsg.Notifications;

		public bool HasWarningMessage { get; set; }

		private RowChangeType ToChangeType(ResultObjectMsg.FeatureOneofCase featureCase)
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

		public void SetNewOid(long newObjectId)
		{
			_newObjectId = newObjectId;
		}
	}
}
