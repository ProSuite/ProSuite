using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing;

public class ResultFeature
{
	[NotNull]
	public Feature OriginalFeature { get; }

	[NotNull] private readonly Func<SpatialReference, Geometry> _getNewGeometry;
	private Geometry _newGeometry;

	private long _newObjectId;

	public ResultFeature([NotNull] Feature originalFeature,
	                     [NotNull] Func<SpatialReference, Geometry> getNewGeometry,
	                     RowChangeType changeType,
	                     bool hasWarningMessage,
	                     IList<string> messages)
	{
		OriginalFeature = originalFeature;

		_getNewGeometry = getNewGeometry;

		ChangeType = changeType;
		HasWarningMessage = hasWarningMessage;

		Messages = messages;
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
				SpatialReference expectedSpatialRef =
					KnownResultSpatialReference ?? OriginalFeature.GetShape().SpatialReference;

				_newGeometry = _getNewGeometry(expectedSpatialRef);
			}

			return _newGeometry;
		}
	}

	public IList<string> Messages { get; }

	public bool HasWarningMessage { get; set; }

	public void SetNewOid(long newObjectId)
	{
		_newObjectId = newObjectId;
	}
}
