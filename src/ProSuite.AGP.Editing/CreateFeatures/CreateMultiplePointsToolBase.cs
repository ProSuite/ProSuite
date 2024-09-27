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
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using Attribute = ArcGIS.Desktop.Editing.Attributes.Attribute;

namespace ProSuite.AGP.Editing.CreateFeatures
{
	public abstract class CreateMultiplePointsToolBase : ConstructionToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected CreateMultiplePointsToolBase()
		{
			UseSnapping = true;

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

		protected override SketchGeometryType GetSketchGeometryType()
		{
			return SketchGeometryType.Multipoint;
		}

		protected override SketchGeometryType GetSelectionSketchGeometryType()
		{
			return SketchGeometryType.Rectangle;
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

		private static List<long> CreatePointsFeatures(
			[NotNull] EditOperation.IEditContext editContext,
			[NotNull] FeatureClass featureClass,
			[CanBeNull] IEnumerable<Attribute> attributes,
			[NotNull] Multipoint multipoint,
			[CanBeNull] CancelableProgressor cancelableProgressor = null)
		{
			var result = new List<long>();

			RowBuffer rowBuffer = null;
			Feature feature = null;

			try
			{
				// Set the attributes
				rowBuffer = featureClass.CreateRowBuffer();

				FeatureClassDefinition classDefinition = featureClass.GetDefinition();
				GeometryType geometryType = classDefinition.GetShapeType();
				bool classHasZ = classDefinition.HasZ();
				bool classHasM = classDefinition.HasM();

				Assert.True(geometryType == GeometryType.Point ||
				            geometryType == GeometryType.Multipoint,
				            "Invalid target feature class.");

				// NOTE: The attributes from the template inspector can be null!
				if (attributes != null)
				{
					GdbPersistenceUtils.CopyAttributeValues(attributes, rowBuffer);
				}

				foreach (MapPoint point in multipoint.Points)
				{
					Geometry resultGeometry;
					if (geometryType == GeometryType.Point)
					{
						resultGeometry = CreatePoint(point, classHasZ, classHasM);
					}
					else
					{
						resultGeometry = CreateSingleMultipoint(point, classHasZ, classHasM);
					}

					if (cancelableProgressor != null &&
					    cancelableProgressor.CancellationToken.IsCancellationRequested)
					{
						return result;
					}

					// NOTE: Sometimes on CreateRow the following exception is thrown:
					// The feature does not have any associated geometry
					// (which is no problem most of the time)
					GdbPersistenceUtils.SetShape(rowBuffer, resultGeometry, featureClass);

					// Set Z/M awareness
					feature = featureClass.CreateRow(rowBuffer);

					feature.Store();

					//To Indicate that the attribute table has to be updated
					editContext.Invalidate(feature);

					result.Add(feature.GetObjectID());
				}

				// Do some other processing with the row.
			}
			finally
			{
				rowBuffer?.Dispose();
				feature?.Dispose();
			}

			return result;
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

		#region Overrides of OneClickToolBase

		protected override void LogPromptForSelection() { }

		protected override async Task<bool> OnEditSketchCompleteCoreAsync(
			Geometry sketchGeometry, EditingTemplate editTemplate,
			MapView activeView,
			CancelableProgressor cancelableProgressor = null)
		{
			bool success = await QueuedTaskUtils.Run(async () =>
			{
				try
				{
					SetCursor(Cursors.Wait);

					List<long> newFeatureIds;

					FeatureClass currentTargetClass = GetCurrentTargetClass();

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
							       newFeatureIds = CreatePointsFeatures(
								       editContext, currentTargetClass,
								       editTemplate.Inspector, multipoint,
								       cancelableProgressor);

							       _msg.DebugFormat("Created new feature IDs: {0}", newFeatureIds);

							       return newFeatureIds.Count > 0;
						       }, "Create multiple points", datasets);
				}
				finally
				{
					SetCursor(SketchCursor);
				}
			});

			return success;
		}

		protected override CancelableProgressor GetSketchCompleteProgressor()
		{
			var sketchCompleteProgressorSource =
				new CancelableProgressorSource("Creating multiple points from the sketch...",
				                               "cancelled");

			CancelableProgressor sketchCompleteProgressor =
				sketchCompleteProgressorSource.Progressor;

			return sketchCompleteProgressor;
		}

		#endregion

		protected virtual esriGeometryType? GetTargetLayerShapeType()
		{
			EditingTemplate editTemplate = EditingTemplate.Current;

			FeatureLayer currentTargetLayer = ToolUtils.CurrentTargetLayer(editTemplate);

			esriGeometryType? geometryType = currentTargetLayer?.ShapeType;

			return geometryType;
		}

		protected virtual FeatureClass GetCurrentTargetClass()
		{
			EditingTemplate editTemplate =
				Assert.NotNull(EditingTemplate.Current, "No edit template");

			FeatureClass currentTargetClass =
				ToolUtils.GetCurrentTargetFeatureClass(editTemplate);

			return currentTargetClass;
		}

		protected virtual string GetTargetObjectTypeName()
		{
			EditingTemplate editTemplate = EditingTemplate.Current;

			return ToolUtils.CurrentTargetLayer(editTemplate)?.Name ?? string.Empty;
		}
	}
}
