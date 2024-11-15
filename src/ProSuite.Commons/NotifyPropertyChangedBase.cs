using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons
{
	public class NotifyPropertyChangedBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected bool SetProperty<T>(ref T backingField, T value,
		                              [CallerMemberName] string propertyName = null)
		{
			if (Equals(backingField, value))
			{
				return false;
			}

			backingField = value;

			OnPropertyChanged(propertyName);

			return true;
		}
	}
}
