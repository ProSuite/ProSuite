using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.QA
{
	public class QualityVerificationDataset
	{
		private readonly Dataset _dataset;
		[UsedImplicitly] private double _loadTime;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="QualityVerificationDataset"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		[UsedImplicitly]
		protected QualityVerificationDataset() { }

		public QualityVerificationDataset([NotNull] Dataset dataset)
		{
			_dataset = dataset;
		}

		#endregion

		[NotNull]
		public Dataset Dataset
		{
			get { return _dataset; }
		}

		public double LoadTime
		{
			get { return _loadTime; }
			set { _loadTime = value; }
		}
	}
}
