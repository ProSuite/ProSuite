using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
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
		/// <param name="testedRows"></param>
		public QaErrorEventArgs([NotNull] QaError qaError,
		                        IList<IRow> testedRows = null)
		{
			Assert.ArgumentNotNull(qaError, nameof(qaError));

			QaError = qaError;
			TestedRows = testedRows;
		}

		[NotNull]
		public QaError QaError { get; }

		[CanBeNull]
		public IList<IRow> TestedRows { get; }

		public bool Cancel { get; set; }
	}
}
