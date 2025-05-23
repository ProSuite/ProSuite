using System;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ProSuite.AGP.Editing;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.AGP.WorkList.Domain
{
	public class ProductionModelIssueWorkList : IssueWorkList
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public ProductionModelIssueWorkList([NotNull] IWorkItemRepository repository,
		                                    [NotNull] Geometry areaOfInterest,
		                                    [NotNull] string name,
		                                    [NotNull] string displayName) :
			base(repository, areaOfInterest, name, displayName) { }

		public bool CanToggleCurrentItemAllowed()
		{
			var currentItem = (DbStatusWorkItem) Current;

			if (currentItem == null)
			{
				return false;
			}

			// TODO: Check if the current status is NOT 'Hard'

			return Project.Current?.IsEditingEnabled == true;
		}

		/// <summary>
		/// Toggles the current item's issue type between Allowed and Soft.
		/// Must be called on the MCT.
		/// </summary>
		/// <returns></returns>
		public async Task<bool> ToggleCurrentItemAllowed()
		{
			var currentItem = (DbStatusWorkItem) Current;

			if (currentItem == null)
			{
				return false;
			}

			var dbStatusRepository = (DbStatusWorkItemRepository) Repository;

			bool result = await QueuedTaskUtils.Run(
				              async () =>
				              {
					              Row workItemRow = GetCurrentItemSourceRow();

					              if (workItemRow == null)
					              {
						              return false;
					              }

					              ISourceClass sourceClass =
						              dbStatusRepository.SourceClasses.FirstOrDefault(
							              s => s.Uses(currentItem.GdbRowProxy.TableReference));

					              IAttributeReader attributeReader =
						              Assert.NotNull(sourceClass).AttributeReader;

					              string errorTypeFieldName =
						              Assert.NotNull(attributeReader).GetName(Attributes.IssueType);

					              object errorTypeValue = workItemRow[errorTypeFieldName];

					              if (errorTypeValue == null || errorTypeValue == DBNull.Value)
					              {
						              _msg.Warn(
							              "The current item has no value in the error type field. " +
							              "Cannot toggle to allowed.");
						              return false;
					              }

					              ErrorType originalErrorType = (ErrorType) errorTypeValue;

					              if (originalErrorType == ErrorType.Hard)
					              {
						              _msg.Warn(
							              "The current issue is 'Hard' (Error) and cannot be set to 'Allowed'");

						              return false;
					              }

					              ErrorType newErrorType = originalErrorType == ErrorType.Soft
						                                       ? ErrorType.Allowed
						                                       : ErrorType.Soft;

					              bool success =
						              await GdbPersistenceUtils.ExecuteInTransactionAsync(
							              (editContext) =>
							              {
								              workItemRow[errorTypeFieldName] = newErrorType;
								              workItemRow.Store();
								              editContext.Invalidate(workItemRow);

								              return true;
							              }, "Toggle Set Issue to Allowed",
							              new[] { workItemRow.GetTable() }
						              );

					              return success;
				              });

			return result;
		}
	}
}
