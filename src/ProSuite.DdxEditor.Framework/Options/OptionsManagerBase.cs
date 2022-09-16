using System.Windows.Forms;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Framework.Options
{
	public abstract class OptionsManagerBase<T> : IOptionsManager where T : class, new()
	{
		[NotNull] private readonly ISettingsPersister<T> _persister;

		protected OptionsManagerBase([NotNull] ISettingsPersister<T> persister)
		{
			Assert.ArgumentNotNull(persister, nameof(persister));

			_persister = persister;
		}

		public void RestoreOptions()
		{
			T settings = _persister.Read();

			ApplyOptions(settings);
		}

		public void SaveOptions()
		{
			T settings = GetOptions();

			_persister.Write(settings);
		}

		public abstract void ShowOptionsDialog(
			IApplicationController applicationController,
			IWin32Window owner);

		protected abstract void ApplyOptions([NotNull] T options);

		[NotNull]
		protected abstract T GetOptions();
	}
}
