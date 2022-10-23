using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing
{
	public class ResultFeature
	{
		[NotNull] private readonly ResultObjectMsg _resultFeatureMsg;

		[NotNull]
		public Feature Feature { get; }

		private Geometry _updatedGeometry;
		private long _newObjectId;

		public ResultFeature([NotNull] Feature feature,
		                     [NotNull] ResultObjectMsg resultFeatureMsg)
		{
			_resultFeatureMsg = resultFeatureMsg;
			Feature = feature;
			ChangeType = ToChangeType(_resultFeatureMsg.FeatureCase);
			HasWarningMessage = _resultFeatureMsg.HasWarning;
		}

		public RowChangeType ChangeType { get; }

		/// <summary>
		/// The new geometry of an update or insert. Must be called in a queued task (at least the first time)
		/// </summary>
		public Geometry NewGeometry
		{
			get
			{
				if (_updatedGeometry == null)
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

					SpatialReference knownSr = Feature.GetShape().SpatialReference;

					_updatedGeometry =
						ProtobufConversionUtils.FromShapeMsg(gdbObjectMsg.Shape,
						                                     knownSr);
				}

				return _updatedGeometry;
			}
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
