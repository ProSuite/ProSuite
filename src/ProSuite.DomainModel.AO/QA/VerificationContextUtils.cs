using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Notifications;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.QA
{
	[CLSCompliant(false)]
	public static class VerificationContextUtils
	{
		[Obsolete("Use GetIssueDatasets()")]
		[NotNull]
		public static IEnumerable<IErrorDataset> GetErrorDatasets(
			[NotNull] IVerificationContext context)
		{
			return GetIssueDatasets(context);
		}

		[NotNull]
		public static IEnumerable<IErrorDataset> GetIssueDatasets(
			[NotNull] IVerificationContext context)
		{
			Assert.ArgumentNotNull(context, nameof(context));

			if (context.NoGeometryIssueDataset != null)
			{
				yield return context.NoGeometryIssueDataset;
			}

			if (context.MultipointIssueDataset != null)
			{
				yield return context.MultipointIssueDataset;
			}

			if (context.MultiPatchIssueDataset != null)
			{
				yield return context.MultiPatchIssueDataset;
			}

			if (context.LineIssueDataset != null)
			{
				yield return context.LineIssueDataset;
			}

			if (context.PolygonIssueDataset != null)
			{
				yield return context.PolygonIssueDataset;
			}
		}

		public static bool DetermineIfIssuesCanBeNavigated(
			[NotNull] IDatasetContext datasetContext,
			[NotNull] IEnumerable<IErrorDataset> errorDatasets,
			[CanBeNull] NotificationCollection notifications = null)
		{
			Assert.ArgumentNotNull(datasetContext, nameof(datasetContext));
			Assert.ArgumentNotNull(errorDatasets, nameof(errorDatasets));

			var unableToNavigate = false;
			List<IErrorDataset> list = errorDatasets.ToList();

			if (list.Count == 0)
			{
				unableToNavigate = true;
				NotificationUtils.Add(notifications, "No error datasets exist in model");
			}

			foreach (IErrorDataset dataset in list)
			{
				if (dataset == null)
				{
					continue;
				}

				if (datasetContext.OpenObjectClass(dataset) == null)
				{
					unableToNavigate = true;
					NotificationUtils.Add(notifications, "Dataset {0} does not exist in workspace",
					                      dataset.Name);
				}
			}

			// ok if there is at least dataset for rows without geometry
			if (! list.OfType<ErrorTableDataset>().Any())
			{
				unableToNavigate = true;
				NotificationUtils.Add(notifications,
				                      "Error dataset for errors without geometry is required");
			}

			return ! unableToNavigate;
		}

		public static bool DetermineIfIssuesCanBeWritten(
			[NotNull] IDatasetContext datasetContext,
			[NotNull] IEnumerable<IErrorDataset> errorDatasets,
			[CanBeNull] NotificationCollection notifications = null)
		{
			Assert.ArgumentNotNull(datasetContext, nameof(datasetContext));
			Assert.ArgumentNotNull(errorDatasets, nameof(errorDatasets));

			var unableToWrite = false;
			List<IErrorDataset> list = errorDatasets.ToList();

			if (list.Count == 0)
			{
				unableToWrite = true;
				NotificationUtils.Add(notifications, "No error datasets exist in model");
			}

			foreach (IErrorDataset dataset in list)
			{
				if (dataset == null)
				{
					continue;
				}

				if (! CanWriteToDataset(dataset, datasetContext))
				{
					unableToWrite = true;
					NotificationUtils.Add(notifications, "No write access to error dataset {0}",
					                      dataset.Name);
				}
			}

			// ok if there is at least dataset for rows without geometry
			if (! list.OfType<ErrorTableDataset>().Any())
			{
				unableToWrite = true;
				NotificationUtils.Add(notifications,
				                      "Error dataset for errors without geometry is required");
			}

			return ! unableToWrite;
		}

		/// <summary>
		/// Gets the issue datasets by geometry type
		/// </summary>
		/// <param name="context">The context.</param>
		/// <returns></returns>
		[NotNull]
		public static IDictionary<esriGeometryType, IErrorDataset>
			GetIssueDatasetsByGeometryType([NotNull] IVerificationContext context)
		{
			Assert.ArgumentNotNull(context, nameof(context));

			var properties =
				new Dictionary<esriGeometryType, Func<IVerificationContext, IErrorDataset>>
				{
					{esriGeometryType.esriGeometryNull, c => c.NoGeometryIssueDataset},
					{esriGeometryType.esriGeometryPolygon, c => c.PolygonIssueDataset},
					{esriGeometryType.esriGeometryPolyline, c => c.LineIssueDataset},
					{esriGeometryType.esriGeometryMultipoint, c => c.MultipointIssueDataset},
					{esriGeometryType.esriGeometryMultiPatch, c => c.MultiPatchIssueDataset}
				};

			var result = new Dictionary<esriGeometryType, IErrorDataset>();

			foreach (
				KeyValuePair<esriGeometryType, Func<IVerificationContext, IErrorDataset>> pair in
				properties)
			{
				Func<IVerificationContext, IErrorDataset> getIssueDataset = pair.Value;

				IErrorDataset issueDataset = getIssueDataset(context);
				if (issueDataset != null)
				{
					result.Add(pair.Key, issueDataset);
				}
			}

			return result;
		}

		private static bool CanWriteToDataset([NotNull] IObjectDataset objectDataset,
		                                      [NotNull] IDatasetContext datasetContext)
		{
			IObjectClass objectClass = datasetContext.OpenObjectClass(objectDataset);

			// TODO check for file geodatabase/personal geodatabase
			return objectClass != null && DatasetUtils.UserHasWriteAccess(objectClass);
		}
	}
}
