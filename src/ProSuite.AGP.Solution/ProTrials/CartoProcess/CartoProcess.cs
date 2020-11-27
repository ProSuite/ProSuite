using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Processing;
using ProSuite.Processing.Domain;
using ProSuite.Processing.Utils;

namespace ProSuite.AGP.Solution.ProTrials.CartoProcess
{
	public class MapContextAGP : IMapContext
	{
		private Map Map { get; }

		public MapContextAGP(Map map)
		{
			Map = map ?? throw new ArgumentNullException();
		}

		public double ReferenceScale => Map.ReferenceScale;
	}

	public class LayerProxyAGP : IProcessingSelection, IProcessingSymbology
	{
		private readonly BasicFeatureLayer _layer;
		private readonly MapView _mapView;

		public LayerProxyAGP(BasicFeatureLayer layer, MapView mapView)
		{
			_layer = layer ?? throw new ArgumentNullException();
			_mapView = mapView ?? throw new ArgumentNullException();
		}

		public int SelectionCount => _layer.SelectionCount;

		public int CountSelection(QueryFilter filter = null)
		{
			if (filter == null) return SelectionCount;
			var selection = _layer.GetSelection();
			var filtered = selection.Select(filter);
			return filtered.GetCount();
		}

		public IEnumerable<Feature> SearchSelection(QueryFilter filter = null,
		                                            bool recycling = false)
		{
			if (SelectionCount == 0) yield break;

			var selection = _layer.GetSelection();
			using (var cursor = selection.Search(filter, recycling))
			{
				while (cursor.MoveNext())
				{
					if (cursor.Current is Feature feature)
					{
						yield return feature;
					}
				}
			}
		}

		public Geometry QueryDrawingOutline(long oid, OutlineType outlineType)
		{
			DrawingOutlineType drawingOutlineType;
			switch (outlineType)
			{
				case OutlineType.Exact:
					drawingOutlineType = DrawingOutlineType.Exact;
					break;
				case OutlineType.BoundingBox:
					drawingOutlineType = DrawingOutlineType.BoundingEnvelope;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(outlineType), outlineType, null);
			}

			return _layer.QueryDrawingOutline(oid, _mapView, drawingOutlineType);
		}
	}

	public class ProProcessingContext : IProcessingContext
	{
		private readonly Map _map;
		private readonly MapView _mapView;
		private readonly Geodatabase _geodatabase;

		public ProProcessingContext([NotNull] Geodatabase geodatabase, [NotNull] MapView mapView)
		{
			_mapView = mapView ?? throw new ArgumentNullException(nameof(mapView));
			_map = mapView.Map;
			_geodatabase = geodatabase ?? throw new ArgumentNullException(nameof(geodatabase));
			MapContext = new MapContextAGP(mapView.Map);
		}

		public Polygon GetProcessingPerimeter()
		{
			switch (SelectionType)
			{
				case ProcessSelectionType.SelectedFeatures:
					return null;
				case ProcessSelectionType.SelectedFeaturesWithinPerimeter:
					return EditPerimeter;

				case ProcessSelectionType.VisibleExtent:
					return GeometryFactory.CreatePolygon(VisibleExtent);
				case ProcessSelectionType.VisibleExtentWithinPerimeter:
					return GeometryUtils.Intersection(VisibleExtent, EditPerimeter);

				case ProcessSelectionType.AllFeatures:
					return null;
				case ProcessSelectionType.AllFeaturesWithinPerimeter:
					return EditPerimeter;

				default:
					throw new ArgumentOutOfRangeException(nameof(SelectionType));
			}
		}

		public IMapContext MapContext { get; }

		public ProcessSelectionType SelectionType { get; set; }

		public ProcessExecutionType ExecutionType { get; set; }

		protected Polygon EditPerimeter => null;

		protected Envelope VisibleExtent => null;

		public ProcessingDataset OpenDataset(ProcessDatasetName name)
		{
			if (name == null) return null;

			var featureClass = _geodatabase.OpenDataset<FeatureClass>(name.DatasetName); // MCT
			var featureLayer = FindLayer(_map, featureClass);

			var layerProxy = featureLayer != null ? new LayerProxyAGP(featureLayer, _mapView) : null;

			return new ProcessingDataset(name, featureClass, layerProxy, layerProxy);
		}

		public RelationshipClass OpenAssociation(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) return null;
			return _geodatabase.OpenDataset<RelationshipClass>(name); // MCT
		}

		[CanBeNull]
		private static BasicFeatureLayer FindLayer(Map map, FeatureClass featureClass)
		{
			if (map == null || featureClass == null) return null;
			var layers = map.GetLayersAsFlattenedList()
			                .OfType<BasicFeatureLayer>()
			                .Where(lyr => ProcessingUtils.IsSameTable(
				                       GetBaseTable(lyr.GetTable()), featureClass));

			return layers.SingleOrDefault(); // bombs if duplicate - ok?
		}

		private static T GetBaseTable<T>(T layerTable) where T : Table
		{
			if (layerTable == null)
				return null;
			if (! layerTable.IsJoinedTable())
				return layerTable;

			var join = layerTable.GetJoin();
			var baseTable = join.GetDestinationTable();

			return (T) baseTable;
		}

		public bool AllowModification(Feature feature, out string reason)
		{
			reason = string.Empty;
			return true;
		}

		public void SetSystemFields(Row row, Table table)
		{
			// Subclass may override to set system fields
		}

		/// <remarks>Caller's duty to dispose features! (See Pro SDK documentation)</remarks>
		public IEnumerable<Feature> GetInputFeatures(ProcessingDataset dataset,
		                                             Geometry extent = null,
		                                             bool recycling = false)
		{
			if (SelectionType.IsWithinEditPerimeter())
			{
				var perimeter = GetProcessingPerimeter();
				extent = GeometryUtils.Intersection(extent, perimeter);
			}

			if (SelectionType.IsSelectedFeatures())
			{
				if (dataset.Selection.SelectionCount < 1)
				{
					return Enumerable.Empty<Feature>();
				}

				var filter = ProcessingUtils.CreateFilter(dataset.WhereClause, extent);
				return dataset.Selection.SearchSelection(filter, recycling);
			}

			return GetOtherFeatures(dataset, extent, recycling);
		}

		/// <remarks>Caller's duty to dispose features! (See Pro SDK documentation)</remarks>
		public IEnumerable<Feature> GetOtherFeatures(ProcessingDataset dataset,
		                                             Geometry extent = null,
		                                             bool recycling = false)
		{
			var filter = ProcessingUtils.CreateFilter(dataset.WhereClause, extent);
			using (var cursor = dataset.FeatureClass.Search(filter, recycling))
			{
				while (cursor.MoveNext())
				{
					if (cursor.Current is Feature feature)
					{
						yield return feature;
					}
				}
			}
		}

		public int CountInputFeatures(ProcessingDataset dataset, Geometry extent = null)
		{
			if (SelectionType.IsWithinEditPerimeter())
			{
				var perimeter = GetProcessingPerimeter();
				extent = GeometryUtils.Intersection(extent, perimeter);
			}

			if (SelectionType.IsSelectedFeatures())
			{
				if (dataset.Selection.SelectionCount < 1) return 0;
				var filter = ProcessingUtils.CreateFilter(dataset.WhereClause, extent);
				return dataset.Selection.CountSelection(filter);
			}

			return CountOtherFeatures(dataset, extent);
		}

		public int CountOtherFeatures(ProcessingDataset dataset, Geometry extent = null)
		{
			QueryFilter filter = ProcessingUtils.CreateFilter(dataset.WhereClause, extent);

			return dataset.FeatureClass.GetCount(filter);
		}
	}

	public class ProProcessingFeedback : IProcessingFeedback
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public string CurrentGroup { get; set; }
		public string CurrentProcess { get; set; }
		public Feature CurrentFeature { get; set; }

		public void ReportInfo(string text)
		{
			_msg.Info(text);
		}

		public void ReportInfo(string format, params object[] args)
		{
			_msg.InfoFormat(format, args);
		}

		public void ReportWarning(string text, Exception exception = null)
		{
			_msg.Warn(text, exception);
		}

		public void ReportError(string text, Exception exception = null)
		{
			_msg.Error(text, exception);
		}

		public void ReportProgress(int percentage, string text)
		{
			// nothing here, but may kick some progress bar
		}

		public void ReportStopped()
		{
			_msg.Warn("Processing stopped");
		}

		public void ReportCompleted()
		{
			_msg.Info("Processing completed");
		}

		public bool CancellationPending => false;
	}

	public abstract class CartoProcess : ICartoProcess
	{
		public abstract string Name { get; }

		public virtual bool Validate(CartoProcessConfig config)
		{
			return true;
		}

		public abstract void Initialize(CartoProcessConfig config);

		public virtual IEnumerable<ProcessDatasetName> GetOriginDatasets()
		{
			yield break;
		}

		public virtual IEnumerable<ProcessDatasetName> GetDerivedDatasets()
		{
			yield break;
		}

		public virtual bool CanExecute(IProcessingContext context)
		{
			return true;
		}

		public abstract void Execute(IProcessingContext context, IProcessingFeedback feedback);

		[StringFormatMethod("format")]
		protected static Exception ConfigError(string format, params object[] args)
		{
			return new Exception(string.Format(format, args));
		}
	}

	public abstract class CartoProcessEngineBase : IDisposable
	{
		protected CartoProcessEngineBase(string name, IProcessingContext context,
		                                 IProcessingFeedback feedback)
		{
			Name = name ?? nameof(CartoProcess);
			Context = context ?? throw new ArgumentNullException(nameof(context));
			Feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
		}

		protected string Name { get; }
		protected IProcessingContext Context { get; }
		protected IProcessingFeedback Feedback { get; }

		public int TotalFeatures { get; set; }
		private int FeaturesStarted { get; set; }
		public int FeaturesProcessed { get; set; }
		public int FeaturesSkipped { get; set; }
		public int FeaturesFailed { get; set; }

		public abstract void Execute();

		public virtual void Dispose() { }

		protected void CheckCancel()
		{
			if (Feedback.CancellationPending)
			{
				throw new OperationCanceledException();
			}
		}

		protected bool AllowModification(Feature feature, out string reason)
		{
			return Context.AllowModification(feature, out reason);
		}

		#region Opening Datasets

		/// <summary>
		/// Like <see cref="OpenDataset"/>, but throws an exception
		/// if <paramref name="dataset"/> is <c>null</c>. The exception
		/// message includes the <paramref name="parameterName"/>, if present.
		/// </summary>
		[NotNull]
		protected ProcessingDataset OpenRequiredDataset(
			ProcessDatasetName dataset, string parameterName = null)
		{
			if (dataset != null)
			{
				return Assert.NotNull(OpenDataset(dataset));
			}

			if (string.IsNullOrEmpty(parameterName))
			{
				throw new InvalidConfigurationException("Required dataset parameter is missing");
			}

			throw new InvalidConfigurationException(
				$"Required parameter '{parameterName}' is missing");
		}

		/// <summary>
		/// Open the given <paramref name="dataset"/>.
		/// </summary>
		/// <param name="dataset">The dataset to open; can be null</param>
		/// <returns>A <see cref="ProcessingDataset"/> instance, or <c>null</c>
		/// if the given <paramref name="dataset"/> is <c>null</c></returns>
		/// <remarks>Null is allowed to easily cope with optional dataset parameters.</remarks>
		[ContractAnnotation("dataset:null => null")]
		protected ProcessingDataset OpenDataset([CanBeNull] ProcessDatasetName dataset)
		{
			if (dataset == null) return null;
			return Context.OpenDataset(dataset);
		}

		[ContractAnnotation("associationName:null => null")]
		protected RelationshipClass OpenAssociation([CanBeNull] string associationName)
		{
			if (string.IsNullOrWhiteSpace(associationName)) return null;
			return Context.OpenAssociation(associationName);
		}

		/// <summary>
		/// Open the given <paramref name="datasets"/>. The resulting
		/// list contains a <see cref="ProcessingDataset"/> instance
		/// for each non-null dataset, and <c>null</c> for each null dataset.
		/// </summary>
		/// <param name="datasets">The datasets to open; required, but <c>null</c> entries are allowed</param>
		/// <returns>List of <see cref="ProcessingDataset"/> objects; the list is
		/// never <c>null</c>, but it may contain <c>null</c> entries!</returns>
		/// <remarks>Null entries are allowed to easily cope with optional dataset parameters.</remarks>
		[NotNull]
		protected IList<ProcessingDataset> OpenDatasets(
			ICollection<ProcessDatasetName> datasets)
		{
			var result = new List<ProcessingDataset>(datasets.Count);

			result.AddRange(datasets.Select(OpenDataset));

			return result;
		}

		#endregion

		#region Getting and counting features

		protected IEnumerable<Feature> GetInputFeatures(ProcessingDataset dataset,
		                                                Geometry extent = null,
		                                                bool recycling = false)
		{
			return Context.GetInputFeatures(dataset, extent, recycling);
		}

		protected IEnumerable<Feature> GetOtherFeatures(ProcessingDataset dataset,
		                                                Geometry extent = null,
		                                                bool recycling = false)
		{
			return Context.GetOtherFeatures(dataset, extent, recycling);
		}

		protected int CountInputFeatures(ProcessingDataset dataset, Geometry extent = null)
		{
			return Context.CountInputFeatures(dataset, extent);
		}

		protected int CountOtherFeatures(ProcessingDataset dataset, Geometry extent = null)
		{
			return Context.CountOtherFeatures(dataset, extent);
		}

		#endregion

		#region Reporting

		protected void ReportInfo(string message)
		{
			Feedback.ReportInfo(message);
		}

		[StringFormatMethod("format")]
		protected void ReportInfo(string format, params object[] args)
		{
			Feedback.ReportInfo(format, args);
		}

		protected void ReportWarning(string message)
		{
			Feedback.ReportWarning(message);
		}

		[StringFormatMethod("format")]
		protected void ReportWarning(string format, params object[] args)
		{
			Feedback.ReportWarning(string.Format(format, args));
		}

		protected void ReportError(Exception exception, string message)
		{
			Feedback.ReportError(message, exception);
		}

		[StringFormatMethod("format")]
		protected void ReportError(Exception exception, string format, params object[] args)
		{
			Feedback.ReportError(string.Format(format, args), exception);
		}

		protected void ReportProgress(int percentage, string message)
		{
			Feedback.ReportProgress(percentage, message);
		}

		[StringFormatMethod("format")]
		protected void ReportProgress(int percentage, string format, params object[] args)
		{
			ReportProgress(percentage, string.Format(format, args));
		}

		//protected void ReportProgress(int currentStep, int totalSteps, string message)
		//{
		//    float ratio = currentStep / (float) totalSteps;
		//    int percentage = (int) (100.0 * ratio);

		//    _feedback.ReportProgress(percentage, message);
		//}

		private void ReportFeatureProgress()
		{
			int tally = FeaturesProcessed + FeaturesSkipped + FeaturesFailed;

			tally = Math.Max(tally, FeaturesStarted); // use FeaturesStarted (if maintained)

			if (tally > 0 && TotalFeatures > 0)
			{
				float ratio = tally / (float) TotalFeatures;
				var percentage = (int) Math.Round(100.0 * ratio);

				string text = $"{Name}: feature {tally:N0} of {TotalFeatures:N0}";

				Feedback.ReportProgress(percentage, text);
			}
			else
			{
				Feedback.ReportProgress(0, Name);
			}
		}

		protected void ReportStartFeature(Feature feature)
		{
			FeaturesStarted += 1;

			Feedback.CurrentFeature = feature;

			ReportFeatureProgress();
		}

		protected void ReportFeatureProcessed(Feature feature)
		{
			FeaturesProcessed += 1;

			Feedback.CurrentFeature = null;

			ReportFeatureProgress();
		}

		protected void ReportFeatureSkipped(Feature feature, string reason)
		{
			FeaturesSkipped += 1;

			Feedback.ReportWarning(
				string.Format("Feature ({0}) skipped: {1}",
				              ProcessingUtils.Format(feature), reason));

			Feedback.CurrentFeature = null;

			ReportFeatureProgress();
		}

		protected void ReportFeatureFailed(Feature feature, Exception ex)
		{
			FeaturesFailed += 1;

			if (ex is COMException comEx)
			{
				Feedback.ReportError(
					string.Format("Feature ({0}) failed (COMException: ErrorCode = {1}): {2}",
					              ProcessingUtils.Format(feature), comEx.ErrorCode,
					              comEx.Message), comEx);
			}
			else
			{
				Feedback.ReportError(
					string.Format("Feature ({0}) failed: {1}",
					              ProcessingUtils.Format(feature), ex.Message), ex);
			}

			Feedback.CurrentFeature = null;

			ReportFeatureProgress();
		}

		public void ReportProcessComplete(string message = null)
		{
			// Leave CurrentGroup as is
			Feedback.CurrentProcess = null;
			Feedback.CurrentFeature = null;

			StringBuilder sb = GetProcessCompleteMessage();

			if (! string.IsNullOrEmpty(message))
			{
				sb.Append(", ");
				sb.Append(message);
			}

			Feedback.ReportInfo(sb.ToString());
		}

		[StringFormatMethod("format")]
		public void ReportProcessComplete(string format, params object[] args)
		{
			ReportProcessComplete(string.Format(format, args));
		}

		private StringBuilder GetProcessCompleteMessage()
		{
			var sb = new StringBuilder();

			sb.AppendFormat("GdbProcess {0} completed", Name);

			if (TotalFeatures >= 0)
			{
				// Processing was feature-by-feature, give some feature stats:
				sb.Append(", ");
				sb.AppendFormat("{0:N0}/{1:N0}/{2:N0} features processed/skipped/failed",
				                FeaturesProcessed, FeaturesSkipped, FeaturesFailed);
			}

			return sb;
		}

		#endregion
	}
}
