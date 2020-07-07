using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.ScreenBinding.Configuration
{
	public interface IBindingData
	{
		[NotNull]
		IScreenBinder Binder { get; }

		[NotNull]
		IPropertyAccessor Accessor { get; }
	}
}
