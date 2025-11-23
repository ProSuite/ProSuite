using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.UI.QA.VerificationProgress
{
	public class UpdateIssuesOptionsViewModel : INotifyPropertyChanged
	{
		private ErrorDeletionInPerimeter _errorDeletionType;
		private bool _keepPreviousIssues;

		public event PropertyChangedEventHandler PropertyChanged;

		public UpdateIssuesOptionsViewModel()
		{
			ErrorDeletionType = ErrorDeletionInPerimeter.VerifiedQualityConditions;
			KeepPreviousIssues = false;
		}

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public ErrorDeletionInPerimeter ErrorDeletionType
		{
			get => _errorDeletionType;
			set
			{
				_errorDeletionType = value;
				OnPropertyChanged();
			}
		}

		public bool KeepPreviousIssues
		{
			get => _keepPreviousIssues;
			set
			{
				_keepPreviousIssues = value;
				OnPropertyChanged();
			}
		}

		public bool KeepPreviousIssuesEnabled { get; set; } = true;
	}
}
