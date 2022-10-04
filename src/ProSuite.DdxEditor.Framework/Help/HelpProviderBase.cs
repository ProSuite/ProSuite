using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Framework.Help
{
	public abstract class HelpProviderBase : IHelpProvider
	{
		private bool? _canShowHelp;

		protected HelpProviderBase([NotNull] string name)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			Name = name;
		}

		public string Name { get; private set; }

		public void Refresh()
		{
			_canShowHelp = DetermineIfHelpIsAvailable();
		}

		public bool CanShowHelp
		{
			get
			{
				if (_canShowHelp == null)
				{
					_canShowHelp = DetermineIfHelpIsAvailable();
				}

				return _canShowHelp.Value;
			}
		}

		public void ShowHelp(IWin32Window owner)
		{
			ShowHelpCore(owner);
		}

		protected abstract void ShowHelpCore([NotNull] IWin32Window owner);

		protected abstract bool DetermineIfHelpIsAvailable();
	}
}
