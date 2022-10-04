using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Properties;

namespace ProSuite.DdxEditor.Framework.Help
{
	public class HelpProviderCommand : CommandBase
	{
		[NotNull] private readonly IHelpProvider _helpProvider;
		[NotNull] private readonly IWin32Window _owner;
		[NotNull] private static readonly Image _image = Resources.ShowOnlineHelpCmd;

		public HelpProviderCommand([NotNull] IHelpProvider helpProvider,
		                           [NotNull] IWin32Window owner)
		{
			Assert.ArgumentNotNull(helpProvider, nameof(helpProvider));
			Assert.ArgumentNotNull(owner, nameof(owner));

			_helpProvider = helpProvider;
			_owner = owner;
		}

		public override Image Image => _image;

		public override string Text => _helpProvider.Name;

		protected override void ExecuteCore()
		{
			_helpProvider.ShowHelp(_owner);
		}

		protected override bool EnabledCore =>
			base.EnabledCore && _helpProvider.CanShowHelp;
	}
}
