using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Validation
{
	public interface IValidator
	{
		[NotNull]
		Notification Validate([CanBeNull] object target);

		[NotNull]
		NotificationMessage[] ValidateByField([NotNull] object target,
		                                      [NotNull] string propertyName);
	}
}