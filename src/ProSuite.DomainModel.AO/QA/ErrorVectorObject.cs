using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.QA
{
	public class ErrorVectorObject : VectorObject, IErrorObject
	{
		protected ErrorVectorObject([NotNull] IFeature feature,
		                            [NotNull] ErrorVectorDataset dataset,
		                            [CanBeNull] IFieldIndexCache fieldIndexCache)
			: base(feature, dataset, fieldIndexCache) { }

		#region IErrorObject Members

		public int? QualityConditionId => GetValue(AttributeRole.ErrorConditionId) as int?;

		public string QualityConditionName => GetString(AttributeRole.ErrorConditionName);

		public string ErrorDescription => GetString(AttributeRole.ErrorDescription);

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
