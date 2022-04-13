using System;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.DataModel;

namespace ProSuite.DdxEditor.Content.Models
{
	public interface IModelView<T> : IBoundView<T, IModelObserver> where T : Model
	{
		Func<object> FindUserConnectionProviderDelegate { get; set; }

		Func<object> FindSpatialReferenceDescriptorDelegate { get; set; }

		Func<object> FindSchemaOwnerConnectionProviderDelegate { get; set; }

		Func<object> FindRepositoryOwnerConnectionProviderDelegate { get; set; }

		Func<object> FindAttributeConfiguratorFactoryDelegate { get; set; }

		Func<object> FindDatasetListBuilderFactoryDelegate { get; set; }
	}
}