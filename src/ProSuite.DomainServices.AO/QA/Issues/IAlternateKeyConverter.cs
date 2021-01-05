using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public interface IAlternateKeyConverter
	{
		[NotNull]
		object Convert([NotNull] string tableName,
		               [NotNull] string fieldName,
		               [NotNull] string keyString);
	}
}
