using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.QA;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.Microservices.Client.QA;
using ProSuite.QA.Configurator;
using ProSuite.QA.SpecificationProviderFile;

namespace ProSuite.AGP.Solution.QA
{
	public class QualityVerificationEnvironment : IQualityVerificationEnvironment
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IMapBasedSessionContext _sessionContext;

		private IList<IQualitySpecificationReference> _qualitySpecifications =
			new List<IQualitySpecificationReference>();

		/// <summary>
		/// Initializes a new instance of the <see cref="QualityVerificationEnvironment"/> class.
		/// This constructor is used to use the gRPC based specification provider.
		/// </summary>
		/// <param name="sessionContext"></param>
		/// <param name="client"></param>
		public QualityVerificationEnvironment(
			[NotNull] IMapBasedSessionContext sessionContext,
			[NotNull] QualityVerificationServiceClient client)
		{
			Assert.ArgumentNotNull(sessionContext, nameof(sessionContext));

			_sessionContext = sessionContext;

			_sessionContext.ProjectWorkspaceChanged += ContextProjectWorkspaceChanged;

			SpecificationProvider = new DdxSpecificationReferencesProvider(sessionContext, client);
			FallbackSpecificationProvider = new QASpecificationProviderXml(
				QAConfiguration.Current.DefaultQASpecConfig.SpecificationsProviderConnection);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QualityVerificationEnvironment"/> class.
		/// This constructor is used to use the XML based specification provider.
		/// </summary>
		public QualityVerificationEnvironment()
		{
			SpecificationProvider = new QASpecificationProviderXml(
				QAConfiguration.Current.DefaultQASpecConfig.SpecificationsProviderConnection);
		}

		/// <summary>
		/// The main specification provider to be used to get the available specifications.
		/// </summary>
		public IQualitySpecificationReferencesProvider SpecificationProvider { get; set; }

		/// <summary>
		/// A fall-back specification provider, in case the <see cref="SpecificationProvider"/> is
		/// not available.
		/// </summary>
		public IQualitySpecificationReferencesProvider FallbackSpecificationProvider { get; set; }

		/// <summary>
		/// The application service that performs the verification by using the appropriate
		/// back-end.
		/// </summary>
		public VerificationServiceBase VerificationService { get; set; }

		/// <summary>
		/// The last current quality specification. This can be used to restore the state of the UI.
		/// </summary>
		public IQualitySpecificationReference LastCurrentSpecification { get; set; }

		public IQualitySpecificationReference CurrentQualitySpecification { get; set; }

		public IList<IQualitySpecificationReference> QualitySpecifications =>
			_qualitySpecifications ?? new List<IQualitySpecificationReference>(0);

		public void RefreshQualitySpecifications()
		{
			LoadQualitySpecifications();
		}

		public event EventHandler QualitySpecificationsRefreshed;

		public Geometry LastVerificationPerimeter { get; set; }

		public string BackendDisplayName => SpecificationProvider.BackendDisplayName;

		public async Task<ServiceCallStatus> VerifyPerimeter(
			Geometry perimeter,
			QualityVerificationProgressTracker progress,
			[CanBeNull] string resultsPath)
		{
			IQualitySpecificationReference specification =
				Assert.NotNull(CurrentQualitySpecification, "No current quality specification");

			ProjectWorkspace projectWorkspace =
				Assert.NotNull(_sessionContext.ProjectWorkspace, "No project workspace");

			Assert.NotNull(VerificationService);

			var result = await VerificationService.VerifyPerimeter(
				             specification, perimeter, projectWorkspace, progress, resultsPath);

			LastVerificationPerimeter = perimeter;

			return result;
		}

		public async Task<ServiceCallStatus> VerifySelection(
			IList<Row> objectsToVerify,
			Geometry perimeter,
			QualityVerificationProgressTracker progress,
			string resultsPath)
		{
			IQualitySpecificationReference specification =
				Assert.NotNull(CurrentQualitySpecification, "No current quality specification");

			ProjectWorkspace projectWorkspace =
				Assert.NotNull(_sessionContext.ProjectWorkspace, "No project workspace");

			Assert.NotNull(VerificationService);

			var result = await VerificationService.VerifySelection(
				             specification, objectsToVerify, perimeter, projectWorkspace, progress,
				             resultsPath);

			LastVerificationPerimeter = perimeter;

			return result;
		}

		private void ContextProjectWorkspaceChanged(object sender, EventArgs e)
		{
			LastVerificationPerimeter = null;

			LoadQualitySpecifications();
		}

		private void LoadQualitySpecifications()
		{
			Task<bool> task = LoadQualitySpecificationsAsync();

			task.ContinueWith(t =>
			                  {
				                  ReadOnlyCollection<Exception> inners =
					                  t.Exception?.InnerExceptions;

				                  if (inners != null)
				                  {
					                  foreach (Exception inner in inners)
					                  {
						                  LogException("Error loading quality specifications",
						                               inner);
					                  }
				                  }
			                  },
			                  TaskContinuationOptions.OnlyOnFaulted);
		}

		private async Task<bool> LoadQualitySpecificationsAsync()
		{
			try
			{
				if (! SpecificationProvider.CanGetSpecifications())
				{
					if (FallbackSpecificationProvider != null &&
					    FallbackSpecificationProvider.CanGetSpecifications())
					{
						_qualitySpecifications =
							await FallbackSpecificationProvider.GetQualitySpecifications();
					}
					else
					{
						_qualitySpecifications.Clear();
					}
				}
				else
				{
					_qualitySpecifications = await SpecificationProvider.GetQualitySpecifications();
				}

				// if there's a current quality specification, check if it is valid
				if (CurrentQualitySpecification != null &&
				    ! _qualitySpecifications.Contains(CurrentQualitySpecification))
				{
					CurrentQualitySpecification = null;
				}

				// if there is no valid current specification, select one 
				if (CurrentQualitySpecification == null)
				{
					SelectCurrentQualitySpecification(_qualitySpecifications);
				}
			}
			finally
			{
				// Even if the process failed, let them know
				QualitySpecificationsRefreshed?.Invoke(this, EventArgs.Empty);
			}

			return _qualitySpecifications.Count > 0;
		}

		private void SelectCurrentQualitySpecification(
			[CanBeNull] IList<IQualitySpecificationReference> qualitySpecifications)
		{
			if (qualitySpecifications == null || qualitySpecifications.Count == 0)
			{
				CurrentQualitySpecification = null;
				return;
			}

			if (LastCurrentSpecification != null)
			{
				// try to load the last one used
				IQualitySpecificationReference result = qualitySpecifications.FirstOrDefault(
					qualitySpecification => qualitySpecification.Equals(LastCurrentSpecification));

				if (result != null)
				{
					CurrentQualitySpecification = result;
					return;
				}
			}

			CurrentQualitySpecification =
				qualitySpecifications.Count == 0 ? null : qualitySpecifications[0];

			StoreLastQualitySpecificationId();
		}

		private void StoreLastQualitySpecificationId()
		{
			if (CurrentQualitySpecification == null)
			{
				return;
			}

			LastCurrentSpecification = CurrentQualitySpecification;
		}

		private static void LogException(string errorMessage, Exception exception)
		{
			_msg.Error($"{errorMessage}: {exception.Message}", exception);
		}
	}
}
