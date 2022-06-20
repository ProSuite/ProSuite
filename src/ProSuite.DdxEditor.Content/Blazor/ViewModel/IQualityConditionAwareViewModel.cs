using System;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

// todo daro better name
public interface IViewModel : IViewObserver
{
}

// todo daro use IQualityConditionContextAware!!!
public interface IQualityConditionAwareViewModel : IViewModel
{
	QualityCondition QualityCondition { get; }

	ITestParameterDatasetProvider DatasetProvider { get; }
}
