using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
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
			throw new NotImplementedException();
		}

		public IEnumerable<Feature> GetFeatures(FeatureClass featureClass, string whereClause, Geometry extent = null,
		                                        bool recycling = false)
		{
			throw new NotImplementedException();
		}

		public int CountFeatures(FeatureClass featureClass, string whereClause, Geometry extent = null)
		{
			throw new NotImplementedException();
		}
	}

	public class ProProcessingFeedback : IProcessingFeedback
	{
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
		protected CartoProcessEngineBase(IProcessingContext context, IProcessingFeedback feedback)
		{
			Context = context ?? throw new ArgumentNullException(nameof(context));
			Feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
		}

		protected IProcessingContext Context { get; }
		protected IProcessingFeedback Feedback { get; }

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
	}
}
