using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.UI.QA.PropertyEditors
{
	public abstract class VectorDatasetPropertyBase<T> : DatasetProperty<T>
		where T : DatasetConfig
	{
		protected VectorDatasetPropertyBase([NotNull] T dataset) : base(dataset) { }

		protected override Type GetParameterType()
		{
			return typeof(IFeatureClass);
		}
	}
}