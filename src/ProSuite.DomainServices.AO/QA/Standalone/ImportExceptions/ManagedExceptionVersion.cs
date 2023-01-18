using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.Standalone.ImportExceptions
{
	public class ManagedExceptionVersion
	{
		[NotNull] private readonly IDictionary<IssueAttribute, object> _editableAttributes =
			new Dictionary<IssueAttribute, object>();

		public ManagedExceptionVersion(long objectId, Guid lineageUuid, Guid versionUuid,
		                               string versionImportOrigin,
		                               string importOrigin,
		                               DateTime? versionBeginDate,
		                               DateTime? versionEndDate)
		{
			ObjectID = objectId;
			LineageUuid = lineageUuid;
			VersionUuid = versionUuid;
			VersionImportOrigin = versionImportOrigin;
			ImportOrigin = importOrigin;
			VersionBeginDate = versionBeginDate;
			VersionEndDate = versionEndDate;
		}

		public IEnumerable<IssueAttribute> EditableAttributes => _editableAttributes.Keys;

		public ExceptionObjectStatus Status
		{
			get
			{
				string statusValue = GetValue(IssueAttribute.ExceptionStatus) as string;

				return ExceptionObjectUtils.ParseStatus(statusValue, ExceptionObjectStatus.Active);
			}
		}

		public long ObjectID { get; }

		public Guid LineageUuid { get; }

		public Guid VersionUuid { get; }

		public string VersionImportOrigin { get; }

		public string ImportOrigin { get; }

		public DateTime? VersionBeginDate { get; }

		public DateTime? VersionEndDate { get; }

		public ManagedExceptionVersion Clone()
		{
			var result = new ManagedExceptionVersion(ObjectID, LineageUuid, VersionUuid,
			                                         VersionImportOrigin, ImportOrigin,
			                                         VersionBeginDate, VersionEndDate);

			foreach (KeyValuePair<IssueAttribute, object> pair in _editableAttributes)
			{
				result._editableAttributes.Add(pair.Key, pair.Value);
			}

			return result;
		}

		[CanBeNull]
		public object GetValue(IssueAttribute attribute)
		{
			object value;
			return _editableAttributes.TryGetValue(attribute, out value)
				       ? value
				       : null;
		}

		public void SetValue(IssueAttribute attribute, [CanBeNull] object value)
		{
			if (attribute == IssueAttribute.ExceptionStatus)
			{
				// normalize the status value
				value = ExceptionObjectUtils.GetNormalizedStatus(value as string);
			}

			_editableAttributes[attribute] = value;
		}
	}
}
