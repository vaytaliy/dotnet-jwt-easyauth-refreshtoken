using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Recipes.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Recipes.Data
{
    public class RecipesContext
    {
        private readonly IConfiguration _configuration;
        public RecipesContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IDbConnection CreateConnection()
        {
            //var connstr = new SqlConnection(_configuration.GetConnectionString("SqlConnectionsReadonly:mssql")).ConnectionString;
            var connString = _configuration.GetConnectionString("Db");
            return new SqlConnection(connString);
        }
    }
}
