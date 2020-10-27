using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Controls;

namespace ProSuite.AGP.Solution.ProTrials
{
	/// <summary>
	/// Interaction logic for ProTrialsRegistrationWindow.xaml
	/// </summary>
	public partial class ProTrialsRegistrationWindow : ProWindow, INotifyPropertyChanged
	{
		private ICommand _authorizeCommand;

		public ProTrialsRegistrationWindow()
		{
			InitializeComponent();

			((FrameworkElement) Content).DataContext = this;
		}

		public string AuthorizationID
		{
			get => ProTrialsModule.AuthorizationID;
			set
			{
				ProTrialsModule.AuthorizationID = value;
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
			if (ProTrialsModule.CheckLicensing(
				ProTrialsModule.AuthorizationID))
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
