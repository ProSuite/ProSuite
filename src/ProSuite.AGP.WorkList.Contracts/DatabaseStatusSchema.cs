using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts
{
	public class DatabaseStatusSchema
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="DatabaseStatusSchema" /> class.
		/// </summary>
		/// <param name="fieldName">Name of the status field.</param>
		/// <param name="fieldIndex"></param>
		/// <param name="todoValue">The field value for status <c>Todo</c>.</param>
		/// <param name="doneValue">The field value for status <c>Done</c>.</param>
		public DatabaseStatusSchema([NotNull] string fieldName,
		                            int fieldIndex,
		                            object todoValue,
		                            object doneValue)
		{
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

			FieldName = fieldName;
			FieldIndex = fieldIndex;
			TodoValue = todoValue;
			DoneValue = doneValue;
		}

		public string FieldName { get; }

		public int FieldIndex { get; }

		public object TodoValue { get; }

		public object DoneValue { get; }
	}
}
