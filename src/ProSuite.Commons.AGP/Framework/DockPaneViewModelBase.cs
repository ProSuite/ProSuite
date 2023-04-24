using System.Windows.Controls;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Framework
{
	public abstract class DockPaneViewModelBase : DockPane
	{
		[NotNull] private readonly Control _contentControl;

		protected DockPaneViewModelBase([NotNull] Control contentControl)
		{
			Assert.ArgumentNotNull(contentControl, nameof(contentControl));

			_contentControl = contentControl;
		}

		protected override Control OnCreateContent()
		{
			_contentControl.DataContext = this;

			return _contentControl;
		}
	}
}
