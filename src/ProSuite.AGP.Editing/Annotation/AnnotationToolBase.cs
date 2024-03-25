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
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Selection;
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

			// Very important! If not called, the EditingTemplate.TextString is not instantly updated.
			// Without the call it seems like the template is not completely activated or missing commit
			// or race condition.
			layer.AutoGenerateTemplates();

			var labelById = GetLabelClassCollection(layer).ToDictionary(cls => cls.ID, cls => cls);

			IEnumerable<Feature> features = pair.Value;

			foreach (AnnotationFeature annotationFeature in features.Cast<AnnotationFeature>())
			{
				int annotationClassID = annotationFeature.GetAnnotationClassID();

				if (!labelById.TryGetValue(annotationClassID, out CIMLabelClass label))
				{
					_msg.Debug(
						$"Cannot find label class ID {annotationClassID} in label class collection of layer {layer.Name}");
					continue;
				}

				EditingTemplate template = layer.GetTemplate(label.Name);

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
				await inspector.LoadAsync(layer, annotationFeature.GetObjectID());

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
			Dictionary<MapMember, List<long>> selectionByLayer = SelectionUtils.GetSelection(ActiveMapView.Map);

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

					//CIMTextGraphic newGraphic = textGraphic.Clone();
					//newGraphic.Shape = sketchGeometry;
					//annotationFeature.SetGraphic(newGraphic);
					
					textGraphic.Shape = sketchGeometry;
					annotationFeature.SetGraphic(textGraphic);
					annotationFeature.Store();

					//Inspector inspector = new Inspector();
					//await inspector.LoadAsync(layer, annotationFeature.GetObjectID()); // LoadAsync()?
					//AnnotationProperties annotationProperties = inspector.GetAnnotationProperties();
					//annotationProperties.Shape = sketchGeometry;
					//inspector.SetAnnotationProperties(annotationProperties);
				}
			}

			return true;
		});
	}

	private async Task<EditingTemplate> CreateTemplate(MapMember layer, Row feature)
	{
		var inspector = new Inspector();
		await inspector.LoadAsync(layer, feature.GetObjectID());

		return layer.CreateTemplate("Annotation Tool", "temporarily used for Annotation Tool", inspector, ID);
	}

	private static IReadOnlyList<CIMLabelClass> GetLabelClassCollection(AnnotationLayer layer)
	{
		return layer.GetFeatureClass() is AnnotationFeatureClass annotationFeatureClass
			       ? annotationFeatureClass.GetDefinition().GetLabelClassCollection()
			       : new List<CIMLabelClass>();
	}
}
