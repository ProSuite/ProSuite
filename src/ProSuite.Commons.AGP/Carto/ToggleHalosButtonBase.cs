using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI;

namespace ProSuite.Commons.AGP.Carto
{
	public abstract class ToggleHalosButtonBase : Button
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected ToggleHalosButtonBase()
		{
			this.IsChecked = Halos.Instance.ToggleState;
		}

		protected override void OnClick()
		{
			ViewUtils.Try(OnClickCore, _msg);
		}

		private async void OnClickCore()
		{
			this.IsChecked = this.IsChecked ? false : true;

			QueuedTask.Run(() => Halos.Instance.ToggleHalo(this.IsChecked
				                                               ? SymbolSubstitutionType.IndividualSubordinate
				                                               : SymbolSubstitutionType.None));
		}
	}
}
