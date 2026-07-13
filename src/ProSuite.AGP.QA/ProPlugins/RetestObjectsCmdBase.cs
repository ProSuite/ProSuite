using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.QA.VerificationProgress;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.UI.Core.QA.VerificationProgress;

namespace ProSuite.AGP.QA.ProPlugins
{
	/// <summary>
	/// Re-verifies the objects involved in the current issue work item and updates the work list.
	/// This is the Pro SDK equivalent of the ArcObjects ErrorCorrectionRetestObjectsCommand: the
	/// "current error" is the navigator's current work item, the "involved objects" are resolved
	/// from the current item and the verification perimeter is the current item's extent.
	/// </summary>
	public abstract class RetestObjectsCmdBase : ButtonCommandBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const string _caption = "Retest Involved Objects";

		/// <summary>
		/// The session context providing the verification environment and project workspace.
		/// </summary>
		protected abstract IVerificationSessionContext SessionContext { get; }

		/// <summary>
		/// The opener used to open or refresh the issue work list after the verification.
		/// </summary>
		protected abstract IWorkListOpener WorkListOpener { get; }

		/// <summary>
		/// The active issue work list whose current item's involved objects are retested. This is
		/// supplied by the product because the active-work-list registry is not visible from here.
		/// </summary>
		[CanBeNull]
		protected abstract IWorkList ActiveIssueWorkList { get; }

		protected virtual
			Func<IQualityVerificationResult, ErrorDeletionInPerimeter, bool, Task<int>>
			SaveAction => null;

		public bool CanRetestCurrentIssue([CanBeNull] IWorkList workList, out string reason)
		{
			if (SessionContext == null)
			{
				reason = "Quality verification is not available";
				return false;
			}

			if (! SessionContext.CanVerifyQuality(out reason))
			{
				reason ??= "Quality verification is not available";
				return false;
			}

			if (workList?.CurrentItem == null)
			{
				reason = "There is no current issue in the work list";
				return false;
			}

			reason = null;
			return true;
		}

		public async Task<bool> RetestCurrentIssueAsync([CanBeNull] IWorkList workList)
		{
			if (SessionContext?.VerificationEnvironment == null)
			{
				Dialog.Warning(_caption, "No quality verification environment is configured.");
				return false;
			}

			if (! CanRetestCurrentIssue(workList, out string reason))
			{
				Dialog.Warning(_caption, reason ?? "Quality verification is not available.");
				return false;
			}

			MapView mapView = MapView.Active;

			if (mapView == null)
			{
				Dialog.Warning(_caption, "No active map.");
				return false;
			}

			IQualityVerificationEnvironment qaEnvironment =
				Assert.NotNull(SessionContext.VerificationEnvironment);

			IQualitySpecificationReference qualitySpecification =
				qaEnvironment.CurrentQualitySpecificationReference;

			if (qualitySpecification == null)
			{
				Dialog.Warning(_caption, "No quality specification is selected.");
				return false;
			}

			IWorkItem currentItem = workList.CurrentItem;

			IList<Row> involvedRows =
				await QueuedTask.Run(() => GetInvolvedRows(workList, currentItem));

			if (involvedRows.Count == 0)
			{
				Dialog.Warning(
					_caption,
					"No involved objects could be resolved for the current issue. Make sure the " +
					"involved feature layers are present and visible in the active map.");
				return false;
			}

			// The current item's extent is the retest perimeter. May be null for no-geometry issues,
			// in which case only the involved objects (not a perimeter) are verified.
			Geometry perimeter = currentItem.Extent;

			var progressTracker = new QualityVerificationProgressTracker
			                      {
				                      CancellationTokenSource = new CancellationTokenSource()
			                      };

			var projectWorkspace = (ProjectWorkspace) SessionContext.ProjectWorkspace;
			SpatialReference spatialRef = projectWorkspace?.ModelSpatialReference;

			var appController = new AgpBackgroundVerificationController(
				WorkListOpener, mapView, perimeter, spatialRef,
				qaEnvironment, SaveAction);

			var qaProgressViewmodel =
				new VerificationProgressViewModel
				{
					ProgressTracker = progressTracker,
					VerificationAction =
						() => Verify(involvedRows, perimeter, progressTracker),
					ApplicationController = appController
				};

			// For a retest, keep the previously found issues in the work list by default (the merge
			// adds the newly found issues rather than collapsing the filtered list to just this run).
			// The user can still uncheck it. Other verification commands keep their own default.
			qaProgressViewmodel.UpdateOptions.KeepPreviousIssues = true;

			string actionTitle = $"{qualitySpecification.Name}: {_caption}";

			Window window = VerificationProgressWindow.Create(qaProgressViewmodel);

			string backendDisplayName = Assert.NotNullOrEmpty(qaEnvironment.BackendDisplayName);

			VerifyUtils.ShowProgressWindow(window, qualitySpecification,
			                               backendDisplayName, actionTitle);

			return true;
		}

		protected override void OnUpdateCore()
		{
			if (! CanRetestCurrentIssue(ActiveIssueWorkList, out string reason))
			{
				Enabled = false;
				DisabledTooltip = reason;
				return;
			}

			Enabled = true;
			DisabledTooltip = null;
		}

		protected override async Task<bool> OnClickAsyncCore()
		{
			return await RetestCurrentIssueAsync(ActiveIssueWorkList);
		}

		private async Task<ServiceCallStatus> Verify(
			[NotNull] IList<Row> involvedObjects,
			[CanBeNull] Geometry perimeter,
			[NotNull] QualityVerificationProgressTracker progressTracker)
		{
			Task<ServiceCallStatus> verificationTask =
				await BackgroundTask.Run(
					() =>
					{
						IQualityVerificationEnvironment qaEnvironment =
							Assert.NotNull(SessionContext.VerificationEnvironment);

						return qaEnvironment.VerifySelection(
							involvedObjects, perimeter, progressTracker);
					},
					BackgroundProgressor.None);

			return await verificationTask;
		}

		/// <summary>
		/// Resolves the source rows involved in the current work item from the visible map layers.
		/// Must be called on the MCT.
		/// </summary>
		[NotNull]
		private IList<Row> GetInvolvedRows([NotNull] IWorkList workList,
		                                   [NotNull] IWorkItem currentItem)
		{
			var result = new List<Row>();

			Row sourceRow = workList.GetCurrentItemSourceRow();

			if (sourceRow == null)
			{
				_msg.Warn("No source row for the current work item. It might have been deleted.");
				return result;
			}

			IAttributeReader reader = workList.GetAttributeReader(currentItem.UniqueTableId);

			if (reader == null)
			{
				_msg.Warn("No attribute reader for the current work item's source class.");
				return result;
			}

			string involvedString = reader.GetValue<string>(sourceRow, Attributes.InvolvedObjects);

			if (string.IsNullOrEmpty(involvedString))
			{
				_msg.Debug("The current issue has no involved objects.");
				return result;
			}

			MapView mapView = MapView.Active;
			Map map = mapView?.Map;

			if (map == null)
			{
				return result;
			}

			IList<InvolvedTable> involvedTables =
				reader.ParseInvolved(involvedString, sourceRow is Feature);

			foreach (InvolvedTable involved in involvedTables)
			{
				// Negative OIDs mean 'entire table' / dirty area - they cannot be retested.
				List<long> oids = involved.RowReferences
				                          .Where(r => r.UsesOID)
				                          .Select(r => (long) r.OID)
				                          .Where(oid => oid >= 0)
				                          .ToList();

				if (oids.Count == 0)
				{
					continue;
				}

				IDisplayTable displayTable = MapUtils
				                             .GetDisplayTables<IDisplayTable>(
					                             map.GetLayersAsFlattenedList(),
					                             l => IsBasedOnTable(l, involved.TableName))
				                             .FirstOrDefault();

				Table table = displayTable?.GetTable();

				if (table == null)
				{
					_msg.WarnFormat(
						"No visible layer references {0}. Its involved objects will not be retested.",
						involved.TableName);
					continue;
				}

				result.AddRange(GdbQueryUtils.GetRows<Row>(table, oids, null, false));
			}

			return result;
		}

		private static bool IsBasedOnTable([NotNull] IDisplayTable displayTable,
		                                   [NotNull] string tableName)
		{
			// TODO: More robust table comparison than just the name (see SelectInvolvedObjectsCmdBase)

			string candidateName = displayTable.GetTable()?.GetName();

			if (string.IsNullOrEmpty(candidateName))
			{
				return false;
			}

			// Involved rows are typically harvested with unqualified names, while the layer
			// typically references a qualified dataset from the production model.
			if (ModelElementNameUtils.IsQualifiedName(candidateName) &&
			    ! ModelElementNameUtils.IsQualifiedName(tableName))
			{
				candidateName = ModelElementNameUtils.GetUnqualifiedName(candidateName);
			}

			if (ModelElementNameUtils.IsQualifiedName(tableName) &&
			    ! ModelElementNameUtils.IsQualifiedName(candidateName))
			{
				tableName = ModelElementNameUtils.GetUnqualifiedName(tableName);
			}

			return string.Equals(candidateName, tableName, StringComparison.OrdinalIgnoreCase);
		}
	}
}
