using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
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

		public ErrorMultipointDataset ErrorMultipointDataset
		{
			get
			{
				AssignSpecialDatasets();
				return _errorMultipointDataset;
			}
		}

		public ErrorMultiPatchDataset ErrorMultiPatchDataset
		{
			get
			{
				AssignSpecialDatasets();
				return _errorMultiPatchDataset;
			}
		}

		public ErrorLineDataset ErrorLineDataset
		{
			get
			{
				AssignSpecialDatasets();
				return _errorLineDataset;
			}
		}

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
			if (dataset is ErrorMultipointDataset multipointDataset)
			{
				_errorMultipointDataset = multipointDataset;
			}
			else if (dataset is ErrorLineDataset lineDataset)
			{
				_errorLineDataset = lineDataset;
			}
			else if (dataset is ErrorPolygonDataset polygonDataset)
			{
				_errorPolygonDataset = polygonDataset;
			}
			else if (dataset is ErrorMultiPatchDataset multiPatchDataset)
			{
				_errorMultiPatchDataset = multiPatchDataset;
			}
			else if (dataset is ErrorTableDataset tableDataset)
			{
				_errorTableDataset = tableDataset;
			}
		}
	}
}
