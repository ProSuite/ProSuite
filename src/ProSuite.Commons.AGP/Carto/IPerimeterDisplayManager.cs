using ArcGIS.Core.Geometry;

namespace ProSuite.Commons.AGP.Carto;

public interface IPerimeterDisplayManager
{
	bool WantPerimeter { get; set; } // to be exposed as a user setting

	Polygon Perimeter { get; set; } // to be maintained by business logic

	void Refresh();
}
