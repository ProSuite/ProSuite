using System.Threading.Tasks;
using ArcGIS.Core.Geometry;

namespace ProSuite.AGP.Extensions;

public interface ISketchExtension
{
	bool AutoPreview { get; set; }

	bool HasPreview { get; }

	Task<Geometry> TryExtend(Geometry sketch);

	Task UpdatePreviewAsync(Geometry sketch);

	void ClearPreview();
}
