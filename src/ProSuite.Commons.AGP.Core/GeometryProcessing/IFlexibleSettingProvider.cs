using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing;

/// <summary>
/// Interface that allows the injection of a dataset-specific setting which can
/// override the static setting.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IFlexibleSettingProvider<T> where T : struct
{
	// TODO: Instead of IObject something more flexible like IProcessingContext could be used
	//       which could signify different things depending on the tool (and remove AO-dependency)
	//       -> move to Commons.ManagedOptions
	//       -> for the time being use the class name of the object
	T GetValue([CanBeNull] string tableName, out string notification);
}
