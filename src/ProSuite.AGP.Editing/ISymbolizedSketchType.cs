using System;
using System.Threading.Tasks;
using ArcGIS.Desktop.Mapping.Events;

namespace ProSuite.AGP.Editing;

public interface ISymbolizedSketchType : IDisposable
{
	public Task ClearSketchSymbol();

	/// <summary>
	/// Must be called on the MCT.
	/// </summary>
	public Task SetSketchAppearanceAsync();

	/// <summary>
	/// Called when the selection is modified during the sketch phase and the symbol might need to
	/// be updated.
	/// </summary>
	/// <param name="args"></param>
	/// <returns></returns>
	Task SelectionChangedAsync(MapSelectionChangedEventArgs args);
}
