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
		QualityCondition GetCondition(string qualityConditionName);
	}
}
