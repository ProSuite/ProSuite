using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.QA
{
	public class ErrorTableObject : TableObject, IErrorObject
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ErrorTableObject"/> class.
		/// </summary>
		/// <param name="row">The row.</param>
		/// <param name="dataset">The dataset.</param>
		/// <param name="fieldIndexCache">The optional field index cache.</param>
		internal ErrorTableObject([NotNull] IRow row,
		                          [NotNull] ErrorTableDataset dataset,
		                          [CanBeNull] IFieldIndexCache fieldIndexCache)
			: base(row, dataset, fieldIndexCache) { }

		#region IErrorObject Members

		public int? QualityConditionId => GetValue(AttributeRole.ErrorConditionId) as int?;

		public string QualityConditionName => (string) GetValue(AttributeRole.ErrorConditionName);

		public string ErrorDescription => (string) GetValue(AttributeRole.ErrorDescription);

		public string AffectedComponent =>
			(string) GetValue(AttributeRole.ErrorAffectedComponent, true);

		public ErrorType ErrorType => (ErrorType) GetValue(AttributeRole.ErrorErrorType);

		public string RawInvolvedObjects
		{
			get
			{
				object value = GetValue(AttributeRole.ErrorObjects);

				return value != DBNull.Value
					       ? (string) value
					       : string.Empty;
			}
		}

		#endregion
	}
}
