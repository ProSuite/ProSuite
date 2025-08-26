using System;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing.Events;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI;

namespace ProSuite.AGP.Editing.CreateFeatures
{
	public abstract class CreateFeatureToolBase : ConstructionToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private FeatureClass _targetFeatureClass;

		protected CreateFeatureToolBase()
		{
			FireSketchEvents = true;

			RequiresSelection = false;

			// This does not work unless loadOnClick="false" in the daml.xml:
			// And the tags are not recognized either...

			//Tooltip =
			// "Create a new feature for the current feature template." +
			// Environment.NewLine +
			// Environment.NewLine +
			// "Shortcuts:" + Environment.NewLine +
			// "ESC: Delete sketch points" + Environment.NewLine +
			// "F2:  Finish sketch";

			//// how to set up shortcuts?
			//if (Shortcuts != null)
			//{
			// Tooltip += string.Concat(Shortcuts.Select(shortCut => shortCut.DisplayString));
			//}

			//DisabledTooltip =
			// "Select a point or multipoint feature template in the Create Features pane";
		}

		protected override SelectionCursors FirstPhaseCursors => SelectionCursors;

		#region Overrides of PlugIn

		protected override void OnUpdateCore()
		{
			Enabled = IsTargetObjectTypeSet();
		}

		#endregion

		protected override void LogPromptForSelection() { }

		protected override ISymbolizedSketchType GetSymbolizedSketch()
		{
			return null;
		}

		protected override SketchGeometryType GetEditSketchGeometryType()
		{
			esriGeometryType? targetShapeType = GetTargetLayerShapeType();

			switch (targetShapeType)
			{
				case esriGeometryType.esriGeometryPoint:
					return SketchGeometryType.Point;
				case esriGeometryType.esriGeometryMultipoint:
					return SketchGeometryType.Multipoint;
				case esriGeometryType.esriGeometryPolyline:
					return SketchGeometryType.Line;
				case esriGeometryType.esriGeometryPolygon:
					return SketchGeometryType.Polygon;

				default:
					throw new ArgumentOutOfRangeException(
						$"Unsupported sketch type: {targetShapeType}");
			}
		}

		protected override SketchGeometryType GetSelectionSketchGeometryType()
		{
			return SketchGeometryType.Rectangle;
		}

		protected override Task OnToolActivatingCoreAsync()
		{
			_targetFeatureClass = GetCurrentTargetClass(out _);

			ActiveTemplateChangedEvent.Subscribe(OnActiveTemplateChanged);

			return base.OnToolActivatingCoreAsync();
		}

		protected override void OnToolDeactivateCore(bool hasMapViewChanged)
		{
			ActiveTemplateChangedEvent.Unsubscribe(OnActiveTemplateChanged);
			base.OnToolDeactivateCore(hasMapViewChanged);
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

		protected override async Task HandleEscapeAsync()
		{
			try
			{
				Geometry sketch = await GetCurrentSketchAsync();

				if (sketch is { IsEmpty: true } && MapUtils.HasSelection(ActiveMapView))
				{
					await QueuedTask.Run(ClearSelection);
				}
				else
				{
					await ClearSketchAsync();
				}
			}
			catch (Exception ex)
			{
				Gateway.ShowError(ex, _msg);
			}
		}

		#region Overrides of ConstructionToolBase

		protected override async Task<bool> OnEditSketchCompleteCoreAsync(
			Geometry sketchGeometry, EditingTemplate editTemplate,
			MapView activeView,
			CancelableProgressor cancelableProgressor = null)
		{
			bool success = await QueuedTaskUtils.Run(async () =>
			{
				try
				{
					FeatureClass featureClass = GetCurrentTargetClass(out Subtype subtype);

					await StoreNewFeature(sketchGeometry, featureClass, subtype);

					return true;
				}
				catch (Exception ex)
				{
					_msg.Error(ex.Message, ex);
					return false;
				}
			});

			return false;
		}

		#endregion

		#region Virtual members

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
					"determines the type of the new feature.");
			}

			return result;
		}

		protected virtual string GetTargetObjectTypeName()
		{
			EditingTemplate editTemplate = EditingTemplate.Current;

			return ToolUtils.CurrentTargetLayer(editTemplate)?.Name ?? string.Empty;
		}

		protected virtual bool IsTargetObjectTypeSet()
		{
			return EditingTemplate.Current != null;
		}

		protected virtual esriGeometryType? GetTargetLayerShapeType()
		{
			EditingTemplate editTemplate = EditingTemplate.Current;

			FeatureLayer currentTargetLayer = ToolUtils.CurrentTargetLayer(editTemplate);

			esriGeometryType? geometryType = currentTargetLayer?.ShapeType;

			return geometryType;
		}

		protected virtual void SetPredefinedFields(RowBuffer rowBuffer)
		{
			EditingTemplate template = EditingTemplate.Current;

			if (template != null && template.Inspector.HasAttributes)
			{
				GdbPersistenceUtils.CopyAttributeValues(template.Inspector, rowBuffer);
			}
		}

		#endregion

		private void OnActiveTemplateChanged(ActiveTemplateChangedEventArgs e)
		{
			ViewUtils.Try(() =>
			{
				FeatureClass newTargetClass = GetCurrentTargetClass(out _);

				TargetClassChanged(newTargetClass);
			}, _msg, true);
		}

		protected void TargetClassChanged(FeatureClass newTargetClass)
		{
			if (DatasetUtils.IsSameTable(_targetFeatureClass, newTargetClass))
			{
				return;
			}

			_targetFeatureClass = newTargetClass;

			RememberSketch();

			StartSketchPhaseAsync();
		}

		private async Task StoreNewFeature([NotNull] Geometry sketchGeometry,
		                                   [NotNull] FeatureClass featureClass,
		                                   [CanBeNull] Subtype subtype)
		{
			// Prevent invalid Z values and other non-simple geometries:
			Geometry simplifiedSketch =
				Assert.NotNull(GeometryUtils.Simplify(sketchGeometry), "Geometry is null");

			FeatureClassDefinition featureClassDef = featureClass.GetDefinition();

			string tableName = featureClassDef.GetAliasName() ?? featureClassDef.GetName();

			string subtypeName = subtype != null
				                     ? subtype.GetName()
				                     : tableName;

			Feature newFeature = null;
			bool transactionSucceeded =
				await GdbPersistenceUtils.ExecuteInTransactionAsync(
					editContext =>
					{
						RowBuffer rowBuffer = featureClass.CreateRowBuffer(subtype);

						SetPredefinedFields(rowBuffer);

						GdbObjectUtils.SetNullValuesToGdbDefault(
							rowBuffer, featureClassDef, subtype);

						Geometry projected =
							MakeGeometryStorable(simplifiedSketch, featureClassDef);

						GdbPersistenceUtils.SetShape(rowBuffer, projected, featureClass);

						newFeature = featureClass.CreateRow(rowBuffer);

						GdbPersistenceUtils.StoreShape(newFeature, projected, editContext);

						return true;
					}, $"Create {subtypeName}",
					new[] { featureClass });

			if (transactionSucceeded)
			{
				SelectionUtils.ClearSelection(MapView.Active.Map);

				foreach (IDisplayTable displayTable in MapUtils
					         .GetFeatureLayersForSelection<FeatureLayer>(
						         MapView.Active, featureClass))
				{
					if (displayTable is FeatureLayer featureLayer)
					{
						SelectionUtils.SelectFeature(featureLayer, SelectionCombinationMethod.New,
						                             newFeature.GetObjectID());
					}
				}

				_msg.Info(
					$"Created new feature {tableName} ({subtypeName}) ID: {newFeature.GetObjectID()}");
			}
			else
			{
				_msg.Warn($"{Caption}: edit operation failed");
			}
		}

		private static Geometry MakeGeometryStorable(Geometry simplifiedSketch,
		                                             FeatureClassDefinition featureClassDef)
		{
			bool classHasZ = featureClassDef.HasZ();
			bool classHasM = featureClassDef.HasM();

			Geometry geometryToStore =
				GeometryUtils.EnsureGeometrySchema(
					simplifiedSketch, classHasZ, classHasM);

			Geometry projected = GeometryUtils.EnsureSpatialReference(
				geometryToStore, featureClassDef.GetSpatialReference());
			return projected;
		}
	}
}
