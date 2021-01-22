using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	public class UnequalField
	{
		internal UnequalField([NotNull] string fieldName, [NotNull] string message)
		{
			FieldName = fieldName;
			Message = message;
		}

		[NotNull]
		public string FieldName { get; }

		[NotNull]
		public string Message { get; }
	}
}
