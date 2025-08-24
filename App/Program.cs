using App.Data;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build(); //.Run();

            if (args.Contains("db"))
            {
                using (var scope = host.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    var db = scope.ServiceProvider.GetService<DbConnectionContext>();
                    using var connection = db.CreateConnection();

                    string path = Path.Combine(AppContext.BaseDirectory, "create_db.sql");
                    string dbRecreateSQLCommand = File.ReadAllText(path);
                    string[] batches = Regex.Split(dbRecreateSQLCommand, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

                    foreach (var batchCommand in batches)
                    {
                        connection.Execute(batchCommand);
                    }
                }
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
