using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Logging;
using Attribute = ArcGIS.Desktop.Editing.Attributes.Attribute;

namespace ProSuite.AGP.Editing.CreateFeatures
{
	public class CreateMultiplePointsToolBase : ConstructionToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public CreateMultiplePointsToolBase()
		{
			UseSnapping = true;

			RequiresSelection = false;

			// This does not work unless loadOnClick="false" in the daml.xml:
			// And the tags are not recognized either...
			Tooltip =
				"Create a new multipoint or several new point features at once in the current feature template." +
				Environment.NewLine +
				"ESC: Delete sketch points" + Environment.NewLine +
				"F2:  Finish sketch";

			// how to set up shortcuts?
			if (Shortcuts != null)
			{
				Tooltip += string.Concat(Shortcuts.Select(shortCut => shortCut.DisplayString));
			}

			DisabledTooltip = "Select a point or multipoint feature class";
		}

		protected override void OnCurrentTemplateUpdated()
		{
			Enabled = CurrentTemplate != null &&
			          CurrentTargetLayer(CurrentTemplate).ShapeType ==
			          esriGeometryType.esriGeometryPoint;
		}

		protected override SketchGeometryType GetSketchGeometryType()
		{
			return SketchGeometryType.Multipoint;
		}

		protected override void LogEnteringSketchMode()
		{
			_msg.InfoFormat(
				"Draw one or more points. Finish the sketch to create the individual point features in '{0}'.",
				CurrentTemplate?.Layer?.Name);
		}

		private static FeatureClass GetCurrentTargetFeatureClass(EditingTemplate editTemplate)
		{
			// TODO: Notifications
			FeatureLayer featureLayer = CurrentTargetLayer(editTemplate);

			if (featureLayer == null)
			{
				return null;
			}

			return featureLayer.GetFeatureClass();
		}

		private static FeatureLayer CurrentTargetLayer(EditingTemplate editTemplate)
		{
			if (editTemplate == null)
			{
				return null;
			}

			var featureLayer = (FeatureLayer) editTemplate.Layer;

			return featureLayer;
		}

		private static List<long> CreatePointsFeatures(EditOperation.IEditContext editContext,
		                                               FeatureClass featureClass,
		                                               IEnumerable<Attribute> attributes,
		                                               Multipoint multipoint,
		                                               CancelableProgressor cancelableProgressor =
			                                               null)
		{
			var result = new List<long>();

			RowBuffer rowBuffer = null;
			Feature feature = null;

			try
			{
				// Set the attributes
				rowBuffer = featureClass.CreateRowBuffer();

				foreach (Attribute attribute in attributes)
				{
					if (! attribute.IsSystemField && ! attribute.IsGeometryField)
					{
						rowBuffer[attribute.Index] = attribute.CurrentValue;
					}
				}

				foreach (MapPoint point in multipoint.Points)
				{
					if (cancelableProgressor != null &&
					    cancelableProgressor.CancellationToken.IsCancellationRequested)
					{
						return result;
					}

					// Set Z/M awareness
					feature = featureClass.CreateRow(rowBuffer);

					var mapPointBuilder = new MapPointBuilderEx(point);

					mapPointBuilder.HasZ = featureClass.GetDefinition().HasZ();
					mapPointBuilder.HasM = featureClass.GetDefinition().HasM();

					feature.SetShape(mapPointBuilder.ToGeometry());

					feature.Store();

					//To Indicate that the attribute table has to be updated
					editContext.Invalidate(feature);

					result.Add(feature.GetObjectID());
				}

				// Do some other processing with the row.
			}
			catch (GeodatabaseException exObj)
			{
				Console.WriteLine(exObj);
			}
			finally
			{
				if (rowBuffer != null)
					rowBuffer.Dispose();

				if (feature != null)
					feature.Dispose();
			}

			return result;
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

					FeatureClass currentTargetClass = GetCurrentTargetFeatureClass(editTemplate);

					if (currentTargetClass == null)
					{
						throw new Exception("No valid template selected");
					}

					IEnumerable<Dataset> datasets = new List<Dataset> {currentTargetClass};

					var multipoint = (Multipoint) sketchGeometry;

					return await GdbPersistenceUtils.ExecuteInTransactionAsync(
						       editContext =>
						       {
							       newFeatureIds = CreatePointsFeatures(
								       editContext, currentTargetClass,
								       CurrentTemplate.Inspector, multipoint,
								       cancelableProgressor);

							       _msg.DebugFormat("Created new featrue IDs: {0}", newFeatureIds);

							       return newFeatureIds.Count > 0;
						       }, "Create multiple points", datasets);
				}
				finally
				{
					// Anything but the Wait cursor
					SetCursor(Cursors.Arrow);
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
	}
}
