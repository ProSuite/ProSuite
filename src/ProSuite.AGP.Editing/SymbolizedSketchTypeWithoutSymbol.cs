using System;
using System.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing;

[Obsolete("Use a null symbolized sketch type")]
public class SymbolizedSketchTypeWithoutSymbol : ISymbolizedSketchType
{
	[NotNull] private readonly ISymbolizedSketchTool _tool;
	private readonly Func<SketchGeometryType> _sketchGeometryTypeFunc;
	private readonly SketchGeometryType _sketchGeometryType;

	/// <summary>
	/// Sets sketch geometry type based on current selection.
	/// Also set sketch symbol bases on the given SketchGeometryType
	/// or function.
	/// </summary>
	/// <param name="tool"></param>
	/// <param name="sketchType"></param>
	public SymbolizedSketchTypeWithoutSymbol([NotNull] ISymbolizedSketchTool tool,
	                                         Func<SketchGeometryType> sketchType)
	{
		_tool = tool;
		_sketchGeometryTypeFunc = sketchType;
	}

	public SymbolizedSketchTypeWithoutSymbol([NotNull] ISymbolizedSketchTool tool,
	                                         SketchGeometryType sketchType)
	{
		_tool = tool;
		_sketchGeometryTypeFunc = null;
		_sketchGeometryType = sketchType;
	}

	public void Dispose()
	{
		ClearSketchSymbol();
	}

	public Task ClearSketchSymbol()
	{
		_tool.SetSketchSymbol(null);
		return Task.CompletedTask;
	}

	/// <summary>
	/// Must be called on the MCT.
	/// </summary>
	public Task SetSketchAppearanceAsync()
	{
		return Task.CompletedTask;
	}

	public void SetSketchType(BasicFeatureLayer featureLayer)
	{
		var type = _sketchGeometryTypeFunc?.Invoke() ?? _sketchGeometryType;
		_tool.SetSketchType(type);
	}

	public Task SelectionChangedAsync(MapSelectionChangedEventArgs args)
	{
		return Task.CompletedTask;
	}
}
