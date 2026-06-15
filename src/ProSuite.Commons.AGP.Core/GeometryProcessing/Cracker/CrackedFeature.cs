using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.Cracker;

public class CrackedFeature
{
	public CrackedFeature(Feature feature, IList<CrackPoint> crackPoints = null)
	{
		Feature = feature;

		if (crackPoints == null)
		{
			crackPoints = new List<CrackPoint>();
		}

		CrackPoints = crackPoints;
	}

	public Feature Feature { get; }

	public IList<CrackPoint> CrackPoints { get; }
	public GdbObjectReference GdbFeatureReference => new GdbObjectReference(Feature);
}
