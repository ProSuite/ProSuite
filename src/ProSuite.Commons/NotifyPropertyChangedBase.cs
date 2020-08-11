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
	}
}
