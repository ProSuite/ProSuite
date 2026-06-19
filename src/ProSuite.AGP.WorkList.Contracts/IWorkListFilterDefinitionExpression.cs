namespace ProSuite.AGP.WorkList.Contracts;

public interface IWorkListFilterDefinitionExpression
{
	WorkListFilterDefinition FilterDefinition { get; }

	string Expression { get; set; }
}
