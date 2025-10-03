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

		public double Tolerance { get; set; } = double.NaN;

		public double Resolution { get; set; } = double.NaN;

		public string CoordinateSystem { get; set; }

		#region Equality members

		protected bool Equals(QualityVerificationDataset other)
		{
			return Equals(_dataset, other._dataset);
		}

		public override bool Equals(object obj)
		{
			if (obj is null)
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((QualityVerificationDataset) obj);
		}

		public override int GetHashCode()
		{
			return (_dataset != null ? _dataset.GetHashCode() : 0);
		}

		#endregion
	}
}
