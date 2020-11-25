using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public class QaErrorEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="QaErrorEventArgs"/> class.
		/// </summary>
		/// <param name="qaError">The error description.</param>
		public QaErrorEventArgs([NotNull] QaError qaError)
		{
			Assert.ArgumentNotNull(qaError, nameof(qaError));

			QaError = qaError;
		}

		[NotNull]
		public QaError QaError { get; }

		public bool Cancel { get; set; }
	}
}
