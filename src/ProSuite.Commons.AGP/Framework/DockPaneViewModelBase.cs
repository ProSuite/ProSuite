using System;
using System.Windows.Controls;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Framework
{
	public abstract class DockPaneViewModelBase : DockPane
	{
		private readonly Control _contentControl;

		protected DockPaneViewModelBase([NotNull] Control contentControl)
		{
			_contentControl =
				contentControl ?? throw new ArgumentNullException(nameof(contentControl));
		}

		protected override Control OnCreateContent()
		{
			_contentControl.DataContext = this;

			return _contentControl;
		}
	}
}
