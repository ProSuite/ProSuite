using System;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	[CLSCompliant(false)]
	public abstract class ProductionModel : Model
	{
		private ErrorMultipointDataset _errorMultipointDataset;
		private ErrorMultiPatchDataset _errorMultiPatchDataset;
		private ErrorLineDataset _errorLineDataset;
		private ErrorPolygonDataset _errorPolygonDataset;
		private ErrorTableDataset _errorTableDataset;

		/// <summary>
		/// Initializes a new instance of the <see cref="ProductionModel"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected ProductionModel() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ProductionModel"/> class.
		/// </summary>
		/// <param name="name">The name of the model.</param>
		protected ProductionModel(string name) : base(name) { }

		[CLSCompliant(false)]
		public ErrorMultipointDataset ErrorMultipointDataset
		{
			get
			{
				AssignSpecialDatasets();
				return _errorMultipointDataset;
			}
		}

		[CLSCompliant(false)]
		public ErrorMultiPatchDataset ErrorMultiPatchDataset
		{
			get
			{
				AssignSpecialDatasets();
				return _errorMultiPatchDataset;
			}
		}

		[CLSCompliant(false)]
		public ErrorLineDataset ErrorLineDataset
		{
			get
			{
				AssignSpecialDatasets();
				return _errorLineDataset;
			}
		}

		[CLSCompliant(false)]
		public ErrorPolygonDataset ErrorPolygonDataset
		{
			get
			{
				AssignSpecialDatasets();
				return _errorPolygonDataset;
			}
		}

		public ErrorTableDataset ErrorTableDataset
		{
			get
			{
				AssignSpecialDatasets();
				return _errorTableDataset;
			}
		}

		protected override void CheckAssignSpecialDatasetCore(Dataset dataset)
		{
			if (dataset is ErrorMultipointDataset)
			{
				_errorMultipointDataset = (ErrorMultipointDataset) dataset;
			}
			else if (dataset is ErrorLineDataset)
			{
				_errorLineDataset = (ErrorLineDataset) dataset;
			}
			else if (dataset is ErrorPolygonDataset)
			{
				_errorPolygonDataset = (ErrorPolygonDataset) dataset;
			}
			else if (dataset is ErrorMultiPatchDataset)
			{
				_errorMultiPatchDataset = (ErrorMultiPatchDataset) dataset;
			}
			else if (dataset is ErrorTableDataset)
			{
				_errorTableDataset = (ErrorTableDataset) dataset;
			}
		}
	}
}