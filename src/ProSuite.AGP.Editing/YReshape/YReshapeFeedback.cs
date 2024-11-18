using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.AdvancedReshape;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.YReshape
{
	public class YReshapeFeedback : AdvancedReshapeFeedback
	{
		private IDisposable _openJawReplacedEndPointOverlay;

		private readonly CIMPointSymbol _openJawReplaceEndSymbol;
		private readonly CIMPointSymbol _openJawReplaceEndSymbolMove;

		public YReshapeFeedback()
		{
			_openJawReplaceEndSymbol = CreateHollowCircle(0, 0, 200); // Azure Blue
			_openJawReplaceEndSymbolMove = CreateHollowCircle(0, 255, 200); // Celest Blue
		}

		public override Task<bool> UpdatePreview([CanBeNull] IList<ResultFeature> resultFeatures)
		{
			_openJawReplacedEndPointOverlay?.Dispose();

			if (resultFeatures == null || resultFeatures.Count == 0)
			{
				return Task.FromResult(false);
			}

			var jawEndPoints = new List<Geometry>(resultFeatures.Count);
			foreach (var resultFeature in resultFeatures)
			{
				jawEndPoints.Add(resultFeature.NewGeometry);
			}

			if (resultFeatures.Count == 1)
			{
				_openJawReplacedEndPointOverlay =
					AddOverlay(jawEndPoints[0], _openJawReplaceEndSymbol); //TODO: Switch between celest and azure
			}
			else
			{
				return null;
			}

			return Task.FromResult(true);
		}

		public override void Clear()
		{
			_openJawReplacedEndPointOverlay?.Dispose();
			_openJawReplacedEndPointOverlay = null;
		}

		private static CIMPointSymbol CreateHollowCircle(int red, int green, int blue)
		{
			CIMColor transparent = ColorFactory.Instance.CreateRGBColor(0d, 0d, 0d, 0d);
			CIMColor color = ColorFactory.Instance.CreateRGBColor(red, green, blue);

			CIMPointSymbol hollowCircle =
				SymbolFactory.Instance.ConstructPointSymbol(color, 19,
				                                            SimpleMarkerStyle.Circle);

			var marker = hollowCircle.SymbolLayers[0] as CIMVectorMarker;
			var polySymbol = Assert.NotNull(marker).MarkerGraphics[0].Symbol as CIMPolygonSymbol;

			//Outline:
			Assert.NotNull(polySymbol).SymbolLayers[0] =
				SymbolFactory.Instance.ConstructStroke(color, 2, SimpleLineStyle.Solid);

			// Fill:
			polySymbol.SymbolLayers[1] = SymbolFactory.Instance.ConstructSolidFill(transparent);

			return hollowCircle;
		}
	}
}
