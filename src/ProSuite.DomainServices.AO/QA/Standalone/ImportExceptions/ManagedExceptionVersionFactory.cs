using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.Standalone.ImportExceptions
{
	public class ManagedExceptionVersionFactory : ExceptionObjectFactoryBase
	{
		private readonly int _managedLineageUuidIndex;
		private readonly int _managedVersionBeginDateIndex;
		private readonly int _managedVersionEndDateIndex;
		private readonly int _managedVersionUuidIndex;
		private readonly int _managedVersionOriginIndex;
		private readonly int _managedOriginIndex;

		[NotNull] private readonly IDictionary<IssueAttribute, int> _fieldIndexes =
			new Dictionary<IssueAttribute, int>();

		[NotNull] private readonly List<IssueAttribute> _editableAttributes;

		[CLSCompliant(false)]
		public ManagedExceptionVersionFactory(
			[NotNull] ITable table,
			[NotNull] IIssueTableFields fields,
			[NotNull] IEnumerable<IssueAttribute> editableAttributes) :
			base(table, fields)
		{
			Assert.ArgumentNotNull(editableAttributes, nameof(editableAttributes));

			_editableAttributes = editableAttributes.ToList();

			_managedLineageUuidIndex = GetIndex(IssueAttribute.ManagedExceptionLineageUuid);
			_managedVersionBeginDateIndex =
				GetIndex(IssueAttribute.ManagedExceptionVersionBeginDate);
			_managedVersionEndDateIndex =
				GetIndex(IssueAttribute.ManagedExceptionVersionEndDate);
			_managedVersionUuidIndex = GetIndex(IssueAttribute.ManagedExceptionVersionUuid);
			_managedVersionOriginIndex =
				GetIndex(IssueAttribute.ManagedExceptionVersionOrigin);
			_managedOriginIndex = GetIndex(IssueAttribute.ManagedExceptionOrigin);

			// editable attributes
			foreach (IssueAttribute attribute in _editableAttributes)
			{
				_fieldIndexes.Add(attribute, GetIndex(attribute));
			}
		}

		[NotNull]
		[CLSCompliant(false)]
		public ManagedExceptionVersion CreateExceptionVersion([NotNull] IRow row)
		{
			var result = new ManagedExceptionVersion(
				row.OID,
				Assert.NotNull(GetGuid(row, _managedLineageUuidIndex)).Value,
				Assert.NotNull(GetGuid(row, _managedVersionUuidIndex)).Value,
				GetString(row, _managedVersionOriginIndex),
				GetString(row, _managedOriginIndex),
				GetDateTime(row, _managedVersionBeginDateIndex),
				GetDateTime(row, _managedVersionEndDateIndex));

			foreach (IssueAttribute attribute in _editableAttributes)
			{
				result.SetValue(attribute, GetValue(row, _fieldIndexes[attribute]));
			}

			return result;
		}
	}
}
