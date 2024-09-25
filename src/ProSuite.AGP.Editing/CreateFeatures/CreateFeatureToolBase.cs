using System;
using System.Linq;
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
			UseSnapping = true;

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

		#region Overrides of PlugIn

		protected override void OnUpdateCore()
		{
			Enabled = IsTargetObjectTypeSet();
		}

		#endregion

		protected override void LogPromptForSelection() { }

		protected override SketchGeometryType GetSketchGeometryType()
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

		protected override void OnToolActivatingCore()
		{
			_targetFeatureClass = GetCurrentTargetClass(out _);

			ActiveTemplateChangedEvent.Subscribe(OnActiveTemplateChanged);

			base.OnToolActivatingCore();
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
			return ToolUtils.GetCurrentTargetFeatureClass(true, out subtype);
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

			StartSketchPhase();
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

						SetNullValuesToGdbDefault(rowBuffer, featureClassDef, subtype);

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
						         MapView.Active.Map, featureClass))
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

		/// <summary>
		/// Sets the values of the <see cref="RowBuffer"/> which are not yet initialized to the
		/// default values defined in the Geodatabase.
		/// </summary>
		/// <param name="rowBuffer"></param>
		/// <param name="featureClassDef"></param>
		/// <param name="subtype"></param>
		private static void SetNullValuesToGdbDefault(
			[NotNull] RowBuffer rowBuffer,
			[NotNull] FeatureClassDefinition featureClassDef,
			[CanBeNull] Subtype subtype)
		{
			foreach (Field field in featureClassDef.GetFields())
			{
				if (! field.IsEditable)
				{
					continue;
				}

				if (field.FieldType == FieldType.Geometry)
				{
					continue;
				}

				// If the value has not been set (e.g. by the subclass), use the GDB default:
				if (rowBuffer[field.Name] != null)
				{
					rowBuffer[field.Name] = field.GetDefaultValue(subtype);
				}
			}
		}
	}
}
