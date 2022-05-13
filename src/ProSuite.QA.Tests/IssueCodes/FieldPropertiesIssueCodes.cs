using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.QA.Tests.IssueCodes
{
	internal class FieldPropertiesIssueCodes : LocalTestIssueCodes,
	                                           IFieldSpecificationsIssueCodes
	{
		[UsedImplicitly] public const string UnexpectedFieldNameForAlias =
			"UnexpectedFieldNameForAlias";

		[UsedImplicitly] public const string MissingField = "MissingField";
		[UsedImplicitly] public const string UnexpectedFieldLength = "UnexpectedFieldLength";
		[UsedImplicitly] public const string UnexpectedAlias = "UnexpectedAlias";
		[UsedImplicitly] public const string NoDomain = "NoDomain";
		[UsedImplicitly] public const string UnexpectedDomain = "UnexpectedDomain";
		[UsedImplicitly] public const string UnexpectedFieldType = "UnexpectedFieldType";

		public FieldPropertiesIssueCodes() : base("FieldProperties") { }

		IssueCode IFieldSpecificationsIssueCodes.UnexpectedFieldNameForAlias =>
			this[UnexpectedFieldNameForAlias];

		IssueCode IFieldSpecificationsIssueCodes.MissingField => this[MissingField];

		IssueCode IFieldSpecificationIssueCodes.UnexpectedFieldLength =>
			this[UnexpectedFieldLength];

		IssueCode IFieldSpecificationIssueCodes.UnexpectedAlias => this[UnexpectedAlias];

		IssueCode IFieldSpecificationIssueCodes.NoDomain => this[NoDomain];

		IssueCode IFieldSpecificationIssueCodes.UnexpectedDomain => this[UnexpectedDomain];

		IssueCode IFieldSpecificationIssueCodes.UnexpectedFieldType => this[UnexpectedFieldType];
	}
}
