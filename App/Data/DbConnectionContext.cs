using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using App.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace App.Data
{
    public class DbConnectionContext
    {
        private readonly IConfiguration _configuration;
        public DbConnectionContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IDbConnection CreateConnection()
        {
            var connString = _configuration.GetConnectionString("Db");
            return new SqlConnection(connString);
        }
    }
}
