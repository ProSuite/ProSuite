using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Components.Web;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.UI.QA;
using Radzen.Blazor;

namespace ProSuite.DdxEditor.Content.Blazor;

public partial class QualityConditionRazor : IQualityConditionView
{
	public RadzenDataGrid<ParameterValueListItem> DataGrid { get; set; }

	public QualityConditionRazor()
	{

	}

	protected override void OnInitialized()
	{
		// to early: presenter is not instantiated yet
	}

	protected override void OnParametersSet()
	{
		// to early: presenter is not instantiated yet
	}
	
	// todo correct event?
	protected override void OnAfterRender(bool firstRender)
	{
		if (firstRender)
		{
			OnLoad();
		}
	}

	private void OnLoad()
	{
		Load?.Invoke(this, EventArgs.Empty);
	}
	
	private void ValueButtonsClicked()
	{
		object result = FindTestDescriptorDelegate();

		if (result == null)
		{
			return;
		}

	}

	public IntPtr Handle { get; }
	public IQualityConditionObserver Observer { get; set; }

	public void BindTo(QualityCondition target) { }

	public event EventHandler Load;
	public Func<object> FindTestDescriptorDelegate { get; set; }

	public void BindToParameterValues(BindingList<ParameterValueListItem> parameterValues)
	{
		DataGrid.Data = parameterValues.ToList();
		DataGrid.Reload();
	}

	public void SetTestDescription(string description) { }

	public void SetParameterDescriptions(IList<TestParameter> paramList) { }

	public void SetConfigurator(ITestConfigurator configurator) { }

	public bool ExportEnabled { get; set; }
	public bool ImportEnabled { get; set; }
	public string IssueTypeDefault { get; set; }
	public string StopOnErrorDefault { get; set; }
	public bool HasSelectedQualitySpecificationReferences { get; }
	public bool RemoveFromQualitySpecificationsEnabled { get; set; }
	public int FirstQualitySpecificationReferenceIndex { get; }
	public bool TestDescriptorLinkEnabled { get; set; }
	public string QualitySpecificationSummary { get; set; }

	public void BindToQualitySpecificationReferences(
		IList<QualitySpecificationReferenceTableRow> tableRows) { }

	public IList<QualitySpecificationReferenceTableRow>
		GetSelectedQualitySpecificationReferenceTableRows()
	{
		return new List<QualitySpecificationReferenceTableRow>(0);
	}

	public bool Confirm(string message, string title)
	{
		return true;
	}

	public void UpdateScreen() { }

	public void RenderCategory(string categoryText) { }

	public void SaveState() { }

	public void SelectQualitySpecifications(IEnumerable<QualitySpecification> specsToSelect) { }
}
