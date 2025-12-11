using System.Drawing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.Properties;

namespace ProSuite.UI.Core.QA.ResourceLookup
{
	public static class QualityVerificationImageLookup
	{
		[NotNull] private static readonly Image _notFulfilled;
		[NotNull] private static readonly Image _fulfilledWarnings;
		[NotNull] private static readonly Image _fulfilledNoIssues;

		private const int _sortIndexFulfilledNoIssues = 0;
		private const int _sortIndexFulfilledWarnings = 1;
		private const int _sortIndexNotFulfilled = 2;

		static QualityVerificationImageLookup()
		{
			_notFulfilled = QualityVerificationImages.QualityVerificationNotFulfilled;
			_fulfilledWarnings = QualityVerificationImages.QualityVerificationFulfilledWarnings;
			_fulfilledNoIssues = QualityVerificationImages.QualityVerificationFulfilledNoIssues;

			// for sorting
			_notFulfilled.Tag = _sortIndexNotFulfilled;
			_fulfilledWarnings.Tag = _sortIndexFulfilledWarnings;
			_fulfilledNoIssues.Tag = _sortIndexFulfilledNoIssues;
		}

		[NotNull]
		public static Image GetImage([NotNull] QualityVerification qualityVerification)
		{
			Assert.ArgumentNotNull(qualityVerification, nameof(qualityVerification));

			if (! qualityVerification.Fulfilled)
			{
				return _notFulfilled;
			}

			return qualityVerification.IssueCount <= 0
				       ? _fulfilledNoIssues
				       : _fulfilledWarnings;
		}

		public static int GetDefaultSortIndex([NotNull] QualityVerification qualityVerification)
		{
			Assert.ArgumentNotNull(qualityVerification, nameof(qualityVerification));

			if (! qualityVerification.Fulfilled)
			{
				return _sortIndexNotFulfilled;
			}

			return qualityVerification.IssueCount <= 0
				       ? _sortIndexFulfilledNoIssues
				       : _sortIndexFulfilledWarnings;
		}
	}
}
