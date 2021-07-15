using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.CreateFootprint
{
	public class FootprintSourceTargetMapping
	{
		public FeatureClassType Source { get; set; }

		public string BufferDistanceFieldName { get; set; }

		public string ZOffsetFieldName { get; set; }

		public ZCalculationMethod ZCalculationMethod { get; set; }

		public FeatureClassType Target { get; set; }

		/// <summary>
		/// Needed for XML serialization.
		/// </summary>
		public FootprintSourceTargetMapping() { }

		public FootprintSourceTargetMapping(FeatureClassType source, FeatureClassType target,
		                                    string bufferDistanceFieldName,
		                                    ZCalculationMethod zCalculationMethod,
		                                    string zOffsetFieldName)
		{
			Target = target;
			Source = source;

			BufferDistanceFieldName = bufferDistanceFieldName;

			ZCalculationMethod = zCalculationMethod;
			ZOffsetFieldName = zOffsetFieldName;
		}
	}

	[UsedImplicitly]
	public class FeatureClassType
	{
		public FeatureClassType() { }

		public FeatureClassType(string featureClassName, int subtype)
		{
			FeatureClassName = featureClassName;
			Subtype = subtype;
		}

		public string FeatureClassName { get; set; }

		public int? Subtype { get; set; }

		public bool References(IObjectClass objectClass)
		{
			// use unqualified name to support checkouts
			string tableName = DatasetUtils.GetTableName(objectClass);

			return FeatureClassName.Equals(
				tableName, StringComparison.InvariantCultureIgnoreCase);
		}

		public override string ToString()
		{
			return Subtype == null
				       ? string.Format("{0} (no Subtype)", FeatureClassName)
				       : string.Format("{0}, Subtype {1}", FeatureClassName, Subtype);
		}
	}
}
