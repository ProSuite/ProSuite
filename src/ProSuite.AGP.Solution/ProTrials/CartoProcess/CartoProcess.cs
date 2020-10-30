using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Processing.Utils;
using Geometry = ArcGIS.Core.Geometry.Geometry;
using Polygon = ArcGIS.Core.Geometry.Polygon;
using SpatialReference = ArcGIS.Core.Geometry.SpatialReference;

namespace ProSuite.AGP.Solution.ProTrials.CartoProcess
{
	public interface IProcessingContext
	{
		// ProcessExecutionType
		// ProcessSelectionType

		Geodatabase Geodatabase { get; }

		[CanBeNull]
		Polygon GetProcessingPerimeter();

		bool CanExecute(CartoProcess process);

		bool AllowModification(Feature feature, out string reason);

		void SetSystemFields(RowBuffer row, Table table);

		IEnumerable<Feature> GetFeatures(FeatureClass featureClass, string whereClause,
		                                 Geometry extent = null, bool recycling = false);

		int CountFeatures(FeatureClass featureClass, string whereClause, Geometry extent = null);
	}

	public interface IProcessingFeedback
	{
		/// <summary>
		/// Assume subsequent reports pertain to this process group
		/// </summary>
		string CurrentGroup { get; set; }

		/// <summary>
		/// Assume subsequent reports pertain to this process
		/// </summary>
		CartoProcess CurrentProcess { get; set; }

		/// <summary>
		/// Assume subsequent reports pertain to this feature
		/// </summary>
		Feature CurrentFeature { get; set; }

		void ReportInfo([NotNull] string text);

		[StringFormatMethod("format")]
		void ReportInfo([NotNull] string format, params object[] args);

		void ReportWarning([NotNull] string text, Exception exception = null);

		void ReportError([NotNull] string text, Exception exception = null);

		/// <summary>
		/// Report progress to whom it may concern.
		/// </summary>
		/// <remarks>
		/// Implementors shall interpret a percentage below 1 or above 100 as indefinite.
		/// The <paramref name="text"/> is typically something like "ProcessName: feature M of N".
		/// </remarks>
		void ReportProgress(int percentage, [CanBeNull] string text);

		/// <summary>
		/// Report that processing was stopped by the user.
		/// </summary>
		void ReportStopped();

		/// <summary>
		/// Report that processing has completed (with or without errors).
		/// </summary>
		void ReportCompleted();

		/// <summary>
		/// Return true if the user requests cancelling the process.
		/// </summary>
		/// <remarks>
		/// Processes should frequently check this property.
		/// When detecting a cancel request, the typical reaction is
		/// to throw an OperationCanceledException.
		/// </remarks>
		bool CancellationPending { get; }
	}

	[TypeConverter(typeof(ProcessDatasetNameConverter))]
	public class ProcessDatasetName
	{
		public ProcessDatasetName(string datasetName, string whereClause = null)
		{
			if (datasetName == null)
				throw new ArgumentNullException(nameof(datasetName));

			DatasetName = datasetName.Trim();
			WhereClause = whereClause;
		}

		public string DatasetName { get; }
		public string WhereClause { get; }
		// Note: RepClassName no longer needed

		public static ProcessDatasetName Parse(string text)
		{
			if (text == null)
				throw new ArgumentNullException();

			string datasetName = text;
			string whereClause = null;

			int index = text.IndexOf(';');
			if (index >= 0)
			{
				datasetName = text.Substring(0, index);
				whereClause = text.Substring(index + 1);
			}

			return new ProcessDatasetName(datasetName, whereClause);
		}

		public override string ToString()
		{
			if (string.IsNullOrEmpty(WhereClause))
				return DatasetName;
			return string.Format("{0}; {1}", DatasetName, WhereClause);
		}
	}

	public class ProcessDatasetNameConverter : TypeConverter
	{
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value is ProcessDatasetName datasetName)
				return datasetName;
			var text = Convert.ToString(value);
			if (! string.IsNullOrEmpty(text))
				return ProcessDatasetName.Parse(text);
			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
		                                 Type destinationType)
		{
			if (destinationType == typeof(string))
			{
				return value.ToString();
			}
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}

	public class ProcessingDataset
	{
		public string DatasetName { get; }
		public string WhereClause { get; }
		public FeatureClass FeatureClass { get; }
		public GeometryType ShapeType { get; }
		public double XYTolerance { get; }
		public SpatialReference SpatialReference { get; }

		public ProcessingDataset(ProcessDatasetName datasetName, Geodatabase database)
		{
			DatasetName = datasetName.DatasetName;
			WhereClause = datasetName.WhereClause;
			FeatureClass = database.OpenDataset<FeatureClass>(datasetName.DatasetName); // MCT

			var definition = FeatureClass.GetDefinition(); // bombs on joined FC
			ShapeType = definition.GetShapeType(); // MCT
			SpatialReference = definition.GetSpatialReference(); // MCT
			XYTolerance = SpatialReference.XYTolerance;
		}
	}

	public class ProProcessingContext : IProcessingContext
	{
		public ProProcessingContext([NotNull] Geodatabase geodatabase)
		{
			Geodatabase = geodatabase ?? throw new ArgumentNullException(nameof(geodatabase));
		}

		public Polygon GetProcessingPerimeter()
		{
			return null;
		}

		public Geodatabase Geodatabase { get; }

		public bool CanExecute(CartoProcess process)
		{
			return true;
		}

		public bool AllowModification(Feature feature, out string reason)
		{
			reason = string.Empty;
			return true;
		}

		public void SetSystemFields(RowBuffer row, Table table)
		{
			// TODO (for now assume no system fields)
		}

		public IEnumerable<Feature> GetFeatures(FeatureClass featureClass, string whereClause, Geometry extent = null,
		                                        bool recycling = false)
		{
			// TODO support for Selected Features

			var filter = CreateFilter(whereClause, extent);
			using (var cursor = featureClass.Search(filter, recycling))
			{
				while (cursor.MoveNext())
				{
					using (var row = cursor.Current)
					{
						if (row is Feature feature)
						{
							yield return feature;
						}
					}
				}
			}
		}

		public int CountFeatures(FeatureClass featureClass, string whereClause, Geometry extent = null)
		{
			// TODO support for Selected Features

			QueryFilter filter = CreateFilter(whereClause, extent);

			return featureClass.GetCount(filter);
		}

		private static QueryFilter CreateFilter(string whereClause, Geometry extent)
		{
			QueryFilter filter;

			if (extent != null)
			{
				filter = new SpatialQueryFilter
				         {
					         FilterGeometry = extent,
					         SpatialRelationship = SpatialRelationship.Intersects
				         };
			}
			else
			{
				filter = new QueryFilter();
			}

			if (! string.IsNullOrEmpty(whereClause))
			{
				filter.WhereClause = whereClause;
			}

			return filter;
		}
	}

	public class ProProcessingFeedback : IProcessingFeedback
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public string CurrentGroup { get; set; }
		public CartoProcess CurrentProcess { get; set; }
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

	public abstract class CartoProcess
	{
		public abstract string Name { get; }

		public abstract bool Validate(CartoProcessConfig config);

		public abstract void Initialize(CartoProcessConfig config);

		public abstract IEnumerable<ProcessDatasetName> GetOriginDatasets();

		public abstract IEnumerable<ProcessDatasetName> GetDerivedDatasets();

		public abstract bool CanExecute(IProcessingContext context);

		public abstract void Execute(IProcessingContext context, IProcessingFeedback feedback);
	}

	public abstract class CartoProcessEngineBase : IDisposable
	{
		protected CartoProcessEngineBase(string name, IProcessingContext context, IProcessingFeedback feedback)
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
			return dataset == null ? null : new ProcessingDataset(dataset, Context.Geodatabase);
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
		protected IList<ProcessingDataset> OpenDatasetList(
			params ProcessDatasetName[] datasets)
		{
			var result = new List<ProcessingDataset>(datasets.Length);

			result.AddRange(datasets.Select(dataset => OpenDataset(dataset)));

			return result;
		}

		#endregion

		protected int CountFeatures(ProcessingDataset dataset, Geometry extent = null)
		{
			return Context.CountFeatures(dataset.FeatureClass, dataset.WhereClause, extent);
		}

		protected IEnumerable<Feature> GetFeatures(ProcessingDataset dataset, Geometry extent = null, bool recycling = false)
		{
			return Context.GetFeatures(dataset.FeatureClass, dataset.WhereClause, extent, recycling);
		}

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
