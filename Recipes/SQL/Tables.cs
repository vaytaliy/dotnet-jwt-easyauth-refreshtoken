using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Recipes.SQL
{
    public static class Tables
    {
        public enum TableNames //if tables are added or schemas are changed
        {
            Users,
            Recipes
        }

        public static string GetTableName(TableNames tableName)
        {
            var strName = Enum.GetName(typeof(TableNames), tableName);
            return strName;
        }
    }
}
