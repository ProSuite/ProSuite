using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts
{
	public abstract class DbStatusSchema
	{
		#region Constructors

		/// <summary>
		///     Initializes a new instance of the <see cref="DbStatusSchema" /> class.
		/// </summary>
		/// <param name="fieldName">Name of the status field.</param>
		/// <param name="todoValue">The field value for status <c>Todo</c>.</param>
		/// <param name="doneValue">The field value for status <c>Done</c>.</param>
		protected DbStatusSchema([NotNull] string fieldName,
		                         object todoValue,
		                         object doneValue)
		{
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

			FieldName = fieldName;
			TodoValue = todoValue;
			DoneValue = doneValue;
		}

		#endregion

		public string FieldName { get; }

		public object TodoValue { get; }

		public object DoneValue { get; }
	}
}
