using System.Windows.Input;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Mapping;

namespace ProSuite.AGP.Editing;

public interface ISketchTool
{
	string Caption { get; }

	void SetSketchType(SketchGeometryType? sketchType);

	SketchGeometryType? GetSketchType();

	void SetCursor(Cursor cursor);

	void SetTransparentVertexSymbol(VertexSymbolType vertexSymbolType);
}
