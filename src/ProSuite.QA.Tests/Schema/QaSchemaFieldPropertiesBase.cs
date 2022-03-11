using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.AO.Geodatabase;

namespace ProSuite.QA.Tests.Schema
{
	public abstract class QaSchemaFieldPropertiesBase : QaSchemaTestBase
	{
		private readonly IReadOnlyTable _table;
		private readonly bool _matchAliasName;
		private FieldSpecifications _fieldSpecifications;

		#region issue codes

		[NotNull] private static readonly FieldPropertiesIssueCodes _codes =
			new FieldPropertiesIssueCodes();

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes;

		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="QaSchemaFieldPropertiesBase"/> class.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="matchAliasName">if set to <c>true</c>, fields are also checked against a 
		/// specification that matches their alias name.</param>
		/// <param name="referenceTable">The reference table containing field properties (read by 
		/// subclass, but needs to be passed to base class to enable constraint filtering etc.).</param>
		protected QaSchemaFieldPropertiesBase([NotNull] IReadOnlyTable table,
		                                      bool matchAliasName,
		                                      [CanBeNull] IReadOnlyTable referenceTable = null)
			: base(table, referenceTable)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			_table = table;
			_matchAliasName = matchAliasName;
		}

		[NotNull]
		protected abstract IEnumerable<FieldSpecification> GetFieldSpecifications();

		public override int Execute()
		{
			if (_fieldSpecifications == null)
			{
				_fieldSpecifications = new FieldSpecifications(
					_table, GetFieldSpecifications(),
					_matchAliasName, _codes);
			}

			return _fieldSpecifications.Verify(
				(fieldName, description, issueCode) =>
					ReportSchemaPropertyError(issueCode, fieldName, description));
		}
	}
}
