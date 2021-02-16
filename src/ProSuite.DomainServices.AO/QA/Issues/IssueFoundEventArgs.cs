using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	/// <summary>
	/// Used to intercept issue writing to allow for streaming to client.
	/// </summary>
	public class IssueFoundEventArgs : EventArgs
	{
		private Issue _issue;
		[NotNull] private readonly QaError _qaError;

		public IssueFoundEventArgs(
			[NotNull] QualitySpecificationElement qualitySpecificationElement,
			[NotNull] QaError qaError,
			bool isAllowable,
			string involvedObjectsString)
		{
			Assert.ArgumentNotNull(qualitySpecificationElement,
			                       nameof(qualitySpecificationElement));
			Assert.ArgumentNotNull(qaError, nameof(qaError));

			QualitySpecificationElement = qualitySpecificationElement;
			_qaError = qaError;
			ErrorGeometry = qaError.Geometry;
			IsAllowable = isAllowable;

			LegacyInvolvedObjectsString = involvedObjectsString;
		}

		public bool IsAllowable { get; }

		[CanBeNull]
		public IGeometry ErrorGeometry { get; }

		[NotNull]
		public QualitySpecificationElement QualitySpecificationElement { get; }

		public string LegacyInvolvedObjectsString { get; }

		[NotNull]
		public Issue Issue
		{
			get
			{
				if (_issue != null)
				{
					return _issue;
				}

				return _issue = new Issue(QualitySpecificationElement, _qaError);
			}
		}
	}
}
