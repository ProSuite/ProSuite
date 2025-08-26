using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.CreateFeatures
{
	public abstract class CreateMultiplePointsToolBase : ConstructionToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected CreateMultiplePointsToolBase()
		{
			RequiresSelection = false;

			// This does not work unless loadOnClick="false" in the daml.xml:
			// And the tags are not recognized either...
			Tooltip =
				"Create several point or multipoint features at once for the current feature template." +
				Environment.NewLine +
				Environment.NewLine +
				"Shortcuts:" + Environment.NewLine +
				"ESC: Delete sketch points" + Environment.NewLine +
				"F2:  Finish sketch";

			// how to set up shortcuts?
			if (Shortcuts != null)
			{
				Tooltip += string.Concat(Shortcuts.Select(shortCut => shortCut.DisplayString));
			}

			DisabledTooltip =
				"Select a point or multipoint feature template in the Create Features pane";
		}

		protected override SelectionCursors FirstPhaseCursors => SelectionCursors;

		protected override void OnCurrentTemplateUpdated()
		{
			UpdateEnabled();
		}

		#region Overrides of PlugIn

		protected override void OnUpdateCore()
		{
			UpdateEnabled();
		}

		#endregion

		protected override ISymbolizedSketchType GetSymbolizedSketch()
		{
			return MapUtils.IsStereoMapView(ActiveMapView)
				       ? null
				       : new SymbolizedSketchTypeBasedOnSelection(this, GetEditSketchGeometryType);
		}

		protected override SketchGeometryType GetEditSketchGeometryType()
		{
			return SketchGeometryType.Multipoint;
		}

		protected override EditingTemplate GetSketchTemplate()
		{
			return EditingTemplate.Current;
		}

		protected override void LogEnteringSketchMode()
		{
			string layerName = GetTargetObjectTypeName();

			_msg.InfoFormat(
				"Draw one or more points. Finish the sketch to create the individual point features in '{0}'.",
				layerName);
		}

		#region Overrides of OneClickToolBase

		protected override void LogPromptForSelection() { }

		protected override SketchGeometryType GetSelectionSketchGeometryType()
		{
			return SketchGeometryType.Rectangle;
		}

		protected override async Task<bool> OnEditSketchCompleteCoreAsync(
			Geometry sketchGeometry,
			EditingTemplate editTemplate,
			MapView activeView,
			CancelableProgressor cancelableProgressor = null)
		{
			bool success = await QueuedTaskUtils.Run(async () =>
			{
				try
				{
					SetToolCursor(Cursors.Wait);

					List<long> newFeatureIds;

					FeatureClass currentTargetClass = GetCurrentTargetClass(out Subtype subtype);

					if (currentTargetClass == null)
					{
						throw new Exception("No valid template selected");
					}

					IEnumerable<Dataset> datasets = new List<Dataset> { currentTargetClass };

					var multipoint = (Multipoint) sketchGeometry;

					if (multipoint?.IsEmpty != false)
					{
						_msg.Warn("Sketch is null or empty. No feature was created.");
						return false;
					}

					return await GdbPersistenceUtils.ExecuteInTransactionAsync(
						       editContext =>
						       {
							       newFeatureIds = CreatePointFeatures(
								       editContext, currentTargetClass, subtype, GetFieldValue,
								       multipoint,
								       cancelableProgressor);

							       _msg.DebugFormat("Created new feature IDs: {0}", newFeatureIds);

							       return newFeatureIds.Count > 0;
						       }, "Create multiple points", datasets);
				}
				finally
				{
					SetToolCursor(SelectionCursors.GetCursor(GetSketchType(), false));
				}
			});

			return success;
		}

		#endregion

		#region Virtual members

		protected virtual esriGeometryType? GetTargetLayerShapeType()
		{
			EditingTemplate editTemplate = EditingTemplate.Current;

			FeatureLayer currentTargetLayer = ToolUtils.CurrentTargetLayer(editTemplate);

			esriGeometryType? geometryType = currentTargetLayer?.ShapeType;

			return geometryType;
		}

		protected virtual FeatureClass GetCurrentTargetClass(out Subtype subtype)
		{
			FeatureClass result;
			try
			{
				result = ToolUtils.GetCurrentTargetFeatureClass(true, out subtype);
			}
			catch (Exception e)
			{
				throw new InvalidOperationException(
					$"{e.Message}. Please select a template that " +
					"determines the type of the new features.");
			}

			return result;
		}

		protected virtual object GetFieldValue([NotNull] Field field,
		                                       [NotNull] FeatureClassDefinition featureClassDef,
		                                       [CanBeNull] Subtype subtype)
		{
			// If there is an active template, use it:
			if (GdbPersistenceUtils.TryGetFieldValueFromTemplate(
				    field.Name, EditingTemplate.Current, out object result))
			{
				return result;
			}

			// Otherwise: Geodatabase default value:
			return field.GetDefaultValue(subtype);
		}

		protected virtual string GetTargetObjectTypeName()
		{
			EditingTemplate editTemplate = EditingTemplate.Current;

			return ToolUtils.CurrentTargetLayer(editTemplate)?.Name ?? string.Empty;
		}

		#endregion

		private static List<long> CreatePointFeatures(
			[NotNull] EditOperation.IEditContext editContext,
			[NotNull] FeatureClass targetFeatureClass,
			[CanBeNull] Subtype targetSubtype,
			[CanBeNull] Func<Field, FeatureClassDefinition, Subtype, object> getAttributeValueFunc,
			[NotNull] Multipoint multipoint,
			[CanBeNull] CancelableProgressor cancelableProgressor = null)
		{
			var result = new List<long>();

			FeatureClassDefinition featureClassDefinition = targetFeatureClass.GetDefinition();

			var pointGeometries = multipoint.Points.Select(
				p => CreateResultGeometry(p, featureClassDefinition));

			foreach (Feature newFeature in GdbPersistenceUtils.InsertTx(
				         editContext, targetFeatureClass, targetSubtype,
				         pointGeometries, getAttributeValueFunc,
				         cancelableProgressor))
			{
				result.Add(newFeature.GetObjectID());
			}

			return result;
		}

		private static Geometry CreateResultGeometry(MapPoint point,
		                                             FeatureClassDefinition classDefinition)
		{
			GeometryType geometryType = classDefinition.GetShapeType();
			bool classHasZ = classDefinition.HasZ();
			bool classHasM = classDefinition.HasM();

			Assert.True(geometryType == GeometryType.Point ||
			            geometryType == GeometryType.Multipoint,
			            "Invalid target feature class.");

			Geometry resultGeometry;
			if (geometryType == GeometryType.Point)
			{
				resultGeometry = CreatePoint(point, classHasZ, classHasM);
			}
			else
			{
				resultGeometry = CreateSingleMultipoint(point, classHasZ, classHasM);
			}

			return resultGeometry;
		}

		private static Geometry CreateSingleMultipoint(MapPoint point, bool zAware, bool mAware)
		{
			var mapPointBuilder = new MultipointBuilderEx(point);

			mapPointBuilder.HasZ = zAware;
			mapPointBuilder.HasM = mAware;

			Geometry resultGeometry = mapPointBuilder.ToGeometry();

			return resultGeometry;
		}

		private static Geometry CreatePoint(MapPoint point, bool zAware, bool mAware)
		{
			var mapPointBuilder = new MapPointBuilderEx(point);

			mapPointBuilder.HasZ = zAware;
			mapPointBuilder.HasM = mAware;

			Geometry resultGeometry = mapPointBuilder.ToGeometry();
			return resultGeometry;
		}

		private void UpdateEnabled()
		{
			esriGeometryType? geometryType = GetTargetLayerShapeType();

			Enabled = geometryType == esriGeometryType.esriGeometryPoint ||
			          geometryType == esriGeometryType.esriGeometryMultipoint;
		}
	}
}
