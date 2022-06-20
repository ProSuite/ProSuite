using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.QCon;

public class QualityConditionItemAdapter : QualityConditionItem
{
	[NotNull] private readonly TableState _tableState = new TableState();

	[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;

	public QualityConditionItemAdapter([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
	                                   [NotNull] QualityCondition qualityCondition,
	                                   [CanBeNull] IQualityConditionContainerItem containerItem,
	                                   [NotNull] IRepository<QualityCondition> repository) : base(
		modelBuilder, qualityCondition, containerItem, repository)
	{
		_modelBuilder = modelBuilder;
	}

	protected override Control CreateControlCore(IItemNavigation itemNavigation)
	{
		// Dependency Injection!
		// https://andrewlock.net/exploring-dotnet-6-part-10-new-dependency-injection-features-in-dotnet-6/
		// https://www.stevejgordon.co.uk/aspnet-core-dependency-injection-what-is-the-iservicecollection
		// https://volosoft.com/Blog/ASP.NET-Core-Dependency-Injection
		// https://github.com/alirizaadiyahsi/aspnet-core-dependency-injection-training
		// https://csharp.christiannagel.com/2016/06/04/dependencyinjection/

		IServiceCollection serviceCollection = new ServiceCollection();
		serviceCollection.AddBlazorWebView();

		ServiceProvider provider = serviceCollection.BuildServiceProvider();

		var blazorControl = new QualityConditionBlazor(CreateViewModel(), provider);
		
		var control = new QualityConditionControl(_tableState, blazorControl);
		new QualityConditionPresenter(this, control, itemNavigation);
		
		// todo daro: remove!
		// code generation!
		//ITestConfigurator testConfigurator = presenter.GetTestConfigurator();

		//Unloaded += item_Unloaded;
		//SavedChanges += item_SavedChanges;
		//DiscardedChanges += item_DiscardedChanges;

		return control;
	}

	private QualityConditionViewModel CreateViewModel()
	{
		return new QualityConditionViewModel(this, _modelBuilder.GetTestParameterDatasetProvider());
	}
}
