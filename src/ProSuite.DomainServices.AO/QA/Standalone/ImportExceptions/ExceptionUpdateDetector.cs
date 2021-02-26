using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.Standalone.ImportExceptions
{
	public class ExceptionUpdateDetector
	{
		[NotNull] private readonly IDictionary<Guid, ExceptionLineage> _lineages =
			new Dictionary<Guid, ExceptionLineage>();

		[NotNull] private readonly IList<IssueAttribute> _editableAttributes;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public ExceptionUpdateDetector(
			[NotNull] IEnumerable<IssueAttribute> editableAttributes)
		{
			Assert.ArgumentNotNull(editableAttributes, nameof(editableAttributes));

			_editableAttributes = editableAttributes.ToList();
		}

		public void AddExistingException([NotNull] ManagedExceptionVersion exceptionVersion)
		{
			Assert.ArgumentNotNull(exceptionVersion, nameof(exceptionVersion));

			ExceptionLineage lineage;
			if (! _lineages.TryGetValue(exceptionVersion.LineageUuid, out lineage))
			{
				lineage = new ExceptionLineage(exceptionVersion.LineageUuid);
				_lineages.Add(lineage.Uuid, lineage);
			}

			lineage.Include(exceptionVersion);
		}

		public bool HasChange(
			[NotNull] ManagedExceptionVersion updateExceptionVersion,
			[NotNull] out ManagedExceptionVersion mergedExceptionVersion,
			[CanBeNull] out ManagedExceptionVersion replacedExceptionVersion,
			[NotNull] out IList<ExceptionAttributeConflict> conflicts)
		{
			conflicts = new List<ExceptionAttributeConflict>();

			ExceptionLineage lineage;
			if (! _lineages.TryGetValue(updateExceptionVersion.LineageUuid, out lineage))
			{
				// lineage not found -> all properties from update, no replaced rows
				mergedExceptionVersion = updateExceptionVersion.Clone();
				replacedExceptionVersion = null;
				return true;
			}

			ManagedExceptionVersion current = lineage.Current;

			if (current == null)
			{
				// there is no active exception in the lineage --> all properties from update, no replaced rows
				mergedExceptionVersion = updateExceptionVersion.Clone();
				replacedExceptionVersion = null;
				return true;
			}

			ManagedExceptionVersion original =
				lineage.GetVersion(updateExceptionVersion.VersionUuid);
			if (original == null)
			{
				// original exception no longer exists --> all properties from update, no replaced rows
				mergedExceptionVersion = updateExceptionVersion.Clone();
				replacedExceptionVersion = current;
				return true;
			}

			if (HasChange(updateExceptionVersion, current, original,
			              out mergedExceptionVersion,
			              out conflicts))
			{
				replacedExceptionVersion = current;
				return true;
			}

			replacedExceptionVersion = null;
			return false;
		}

		private bool HasChange([NotNull] ManagedExceptionVersion update,
		                       [NotNull] ManagedExceptionVersion current,
		                       [NotNull] ManagedExceptionVersion original,
		                       [NotNull] out ManagedExceptionVersion merged,
		                       [NotNull] out IList<ExceptionAttributeConflict> conflicts)
		{
			merged = update.Clone();

			var changedAttributes = new List<IssueAttribute>();
			conflicts = new List<ExceptionAttributeConflict>();

			if (update.Status == ExceptionObjectStatus.Active &&
			    current.Status == ExceptionObjectStatus.Active)
			{
				// 1: update: active; current: active --> update of active exception
				// --> update attributes regularly
			}
			else if (update.Status == ExceptionObjectStatus.Inactive &&
			         current.Status == ExceptionObjectStatus.Active)
			{
				// 2: update: inactive; current: active --> "deletion" of active exception
				// --> update attributes regularly
			}
			else if (update.Status == ExceptionObjectStatus.Active &&
			         current.Status == ExceptionObjectStatus.Inactive)
			{
				// 3: update: active; current: inactive 
				if (original.Status == ExceptionObjectStatus.Active)
				{
					// set to inactive by previous import
					// --> ignore
					_msg.DebugFormat(
						"Exception ({0}) was set to inactive by previous update, ignore update (OID: {1})",
						ExceptionObjectUtils.FormatGuid(current.LineageUuid), update.ObjectID);
					return false;
				}

				// resurrection
				return true;
				// --> ignore, no change
			}
			else if (update.Status == ExceptionObjectStatus.Inactive &&
			         current.Status == ExceptionObjectStatus.Inactive)
			{
				// 4: update: inactive; current: inactive
				// --> update attributes regularly
				_msg.DebugFormat(
					"Exception ({0}) was set to inactive by previous update, ignore update (OID: {1})",
					ExceptionObjectUtils.FormatGuid(current.LineageUuid), update.ObjectID);
			}

			foreach (IssueAttribute attribute in _editableAttributes)
			{
				object newValue = update.GetValue(attribute);
				object currentValue = current.GetValue(attribute);
				object originalValue = original.GetValue(attribute);

				if (Equals(newValue, originalValue) || Equals(newValue, currentValue))
				{
					// the value was not changed in the update, or it is equal to the current value
					merged.SetValue(attribute, currentValue);
				}
				else
				{
					// different from current, changed in update
					changedAttributes.Add(attribute);

					merged.SetValue(attribute, newValue);

					if (! Equals(currentValue, originalValue))
					{
						// also changed in current --> conflict
						conflicts.Add(CreateConflict(attribute,
						                             newValue, currentValue, originalValue,
						                             update.LineageUuid,
						                             original.VersionUuid));
					}
				}
			}

			return changedAttributes.Count > 0;
		}

		[NotNull]
		private ExceptionAttributeConflict CreateConflict(
			IssueAttribute attribute, object newValue, object currentValue,
			object originalValue, Guid lineageUuid, Guid startVersionGuid)
		{
			ExceptionLineage lineage = _lineages[lineageUuid];

			DateTime? currentValueImportDate;
			string currentValueOrigin = GetCurrentValueOrigin(
				attribute, currentValue, startVersionGuid, lineage,
				out currentValueImportDate);

			return new ExceptionAttributeConflict(attribute,
			                                      newValue, currentValue, originalValue,
			                                      currentValueOrigin,
			                                      currentValueImportDate);
		}

		[CanBeNull]
		private static string GetCurrentValueOrigin(IssueAttribute attribute,
		                                            [CanBeNull] object currentValue,
		                                            Guid startVersionGuid,
		                                            [NotNull] ExceptionLineage lineage,
		                                            out DateTime? importDate)
		{
			string origin = null;
			importDate = null;

			foreach (ManagedExceptionVersion exceptionObject in
				lineage.GetAll()
				       .OrderByDescending(e => e.VersionBeginDate))
			{
				if (! Equals(exceptionObject.GetValue(attribute), currentValue))
				{
					// value is different --> next version (by date) must have introduced this value
					break;
				}

				if (exceptionObject.VersionUuid == startVersionGuid)
				{
					// start version for search (excluded) reached
					break;
				}

				// value is equal
				origin = exceptionObject.VersionImportOrigin;
				importDate = exceptionObject.VersionBeginDate;
			}

			return origin;
		}
	}
}
