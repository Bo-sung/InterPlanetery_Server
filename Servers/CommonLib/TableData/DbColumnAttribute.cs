namespace CommonLib.TableData
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class DbColumnAttribute : Attribute
    {
        public string ColumnName { get; }

        public DbColumnAttribute(string columnName)
        {
            ColumnName = columnName;
        }
    }
}
