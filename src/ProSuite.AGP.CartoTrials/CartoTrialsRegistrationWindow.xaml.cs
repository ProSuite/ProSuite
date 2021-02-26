using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Controls;

namespace ProSuite.AGP.CartoTrials
{
	/// <summary>
	/// Interaction logic for CartoTrialsRegistrationWindow.xaml
	/// </summary>
	public partial class CartoTrialsRegistrationWindow : ProWindow, INotifyPropertyChanged
	{
		private ICommand _authorizeCommand;

		public CartoTrialsRegistrationWindow()
		{
			InitializeComponent();

			((FrameworkElement) Content).DataContext = this;
		}

		public string AuthorizationID
		{
			get => CartoTrialsModule.AuthorizationID;
			set
			{
				CartoTrialsModule.AuthorizationID = value;
				OnPropertyChanged();
			}
		}

		public ICommand AuthorizeCommand => _authorizeCommand ?? (_authorizeCommand = CreateAuthorizeCommand());

		private ICommand CreateAuthorizeCommand()
		{
			return new RelayCommand(() =>
			{
				Authorize();
				Close();
			});
		}

		private void Authorize()
		{
			if (CartoTrialsModule.CheckLicensing(
				CartoTrialsModule.AuthorizationID))
			{
				MessageBox.Show(
					"Add-in authorized. Thank you",
					"Success!",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			}
			else
			{
				MessageBox.Show(
					"Invalid Product ID",
					"Authorization Failed",
					MessageBoxButton.OK,
					MessageBoxImage.Error);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged = delegate { };

		protected virtual void OnPropertyChanged([CallerMemberName] string propName = "")
		{
			PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}
	}
}
