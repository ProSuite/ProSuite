using System;
using System.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ProSuite.AGP.Editing;

public interface ISymbolizedSketchType : IDisposable
{
	public Task ClearSketchSymbol();

	/// <summary>
	/// Must be called on the MCT.
	/// </summary>
	public Task SetSketchAppearanceAsync();

	public void SetSketchType(BasicFeatureLayer featureLayer);
}
