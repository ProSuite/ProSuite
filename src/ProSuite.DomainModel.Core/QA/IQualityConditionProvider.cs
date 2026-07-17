using System.Collections.Generic;
using System.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA
{
	/// <summary>
	/// Encapsulates the server access that provides quality conditions by name.
	/// </summary>
	public interface IQualityConditionProvider
	{
		/// <summary>
		/// Gets the fully populated quality condition with the given name.
		/// </summary>
		/// <param name="qualityConditionName"></param>
		/// <returns></returns>
		[CanBeNull]
		QualityCondition GetCondition([NotNull] string qualityConditionName);

		/// <summary>
		/// Gets the fully populated quality conditions with the given data dictionary ids.
		/// Conditions for unknown ids are silently omitted from the result.
		/// </summary>
		/// <param name="conditionIds"></param>
		/// <returns></returns>
		[NotNull]
		Task<IList<QualityCondition>> GetConditions([NotNull] IList<int> conditionIds);
	}
}
