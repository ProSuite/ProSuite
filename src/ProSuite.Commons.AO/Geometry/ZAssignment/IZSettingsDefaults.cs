using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.ZAssignment
{
	public interface IZSettingsDefaults
	{
		double DefaultConstantZ { get; }

		double DefaultOffset { get; }

		ISurface DefaultSurface { get; }

		double DefaultDtmOffset { get; }

		double DefaultDtmDrapeTolerance { get; }

		ZMode DefaultZMode { get; }

		DtmSubMode DefaultDtmSubMode { get; }

		MultiTargetSubMode DefaultMultiTargetSubMode { get; }

		[CanBeNull]
		ISurface PrepareVirtualSurface([NotNull] IEnvelope envelope,
		                               [CanBeNull] ISurface surface,
		                               double minimalResolution);
	}
}
