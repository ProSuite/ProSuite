using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Mapping;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.Annotation;

public abstract class AnnotationToolBase : ToolBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();
	
	protected AnnotationToolBase(SketchGeometryType sketchGeometryType) :
		base(sketchGeometryType) { }

	protected override bool CanSelectFromLayerCore(BasicFeatureLayer basicFeatureLayer)
	{
		return basicFeatureLayer is AnnotationLayer;
	}

	protected override bool CanSelectGeometryType(GeometryType geometryType)
	{
		return geometryType == GeometryType.Polygon;
	}

	protected override async Task AfterSelection(
		IDictionary<BasicFeatureLayer, IEnumerable<Feature>> featuresByLayer,
		CancelableProgressor progressor = null)
	{
		foreach (KeyValuePair<BasicFeatureLayer, IEnumerable<Feature>> pair in featuresByLayer)
		{
			if (pair.Key is not AnnotationLayer layer)
			{
				continue;
			}

			IEnumerable<Feature> features = pair.Value;

			foreach (AnnotationFeature annotationFeature in features.Cast<AnnotationFeature>())
			{
				EditingTemplate template = await CreateTemplate(layer, annotationFeature);

				if (template == null)
				{
					continue;
				}

				await template.ActivateToolAsync(ID);

				if (annotationFeature.GetGraphic() is not CIMTextGraphic textGraphic)
				{
					continue;
				}

				Inspector inspector = template.Inspector;
				// useless
				//await inspector.LoadAsync(layer, annotationFeature.GetObjectID());

				if (textGraphic.Shape.GeometryType == GeometryType.GeometryBag)
				{
					return;
				}

				AnnotationProperties annotationProperties = inspector.GetAnnotationProperties();
				annotationProperties.LoadFromTextGraphic(textGraphic);
				inspector.SetAnnotationProperties(annotationProperties);

				SketchSymbol = textGraphic.Symbol;
				await SetCurrentSketchAsync(textGraphic.Shape);
				SketchMode = SketchMode.VertexMove;
			}
		}
	}

	protected override async Task<bool> OnSketchModifiedCoreAsync()
	{
		if (! MapUtils.HasSelection(ActiveMapView.Map))
		{
			return false;
		}

		return await QueuedTask.Run(async () =>
		{
			SelectionSet selectionSet = ActiveMapView.Map.GetSelection();

			Dictionary<MapMember, List<long>> selectionByLayer = selectionSet.ToDictionary();

			if (selectionByLayer.Count < 1)
			{
				return false;
			}

			SpatialReference mapSpatialReference = MapView.Active.Map.SpatialReference;

			foreach (KeyValuePair<MapMember, List<long>> pair in selectionByLayer)
			{
				if (pair.Key is not AnnotationLayer layer)
				{
					continue;
				}
				
				foreach (var annotationFeature in MapUtils.GetFeatures(layer, pair.Value, false, mapSpatialReference).Cast<AnnotationFeature>())
				{
					if (annotationFeature.GetGraphic() is not CIMTextGraphic textGraphic)
					{
						continue;
					}

					Geometry sketchGeometry = await GetCurrentSketchAsync();
					
					textGraphic.Shape = sketchGeometry;
					annotationFeature.SetGraphic(textGraphic);
					annotationFeature.Store();

					ActiveMapView.Invalidate(selectionSet);
				}
			}

			return true;
		});
	}
	
	private async Task<EditingTemplate> CreateTemplate(MapMember layer, AnnotationFeature feature)
	{
		var textGraphic = feature.GetGraphic() as CIMTextGraphic;
		Assert.NotNull(textGraphic);

		long oid = feature.GetObjectID();
		
		return await CreateTemplate(layer, oid, textGraphic);
	}

	private async Task<EditingTemplate> CreateTemplate(MapMember layer, long oid,
	                                                   CIMTextGraphic textGraphic)
	{
		var inspector = new Inspector();
		await inspector.LoadAsync(layer, oid);

		AnnotationProperties annotationProperties = inspector.GetAnnotationProperties();
		annotationProperties.LoadFromTextGraphic(textGraphic);
		inspector.SetAnnotationProperties(annotationProperties);

		// todo daro inline
		EditingTemplate template = layer.CreateTemplate("Annotation Tool", "temporarily used for Annotation Tool",
		                                                       inspector, ID);
		return template;
	}
}
