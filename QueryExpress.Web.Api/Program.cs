
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using QueryExpress.Tests.Data;
using QueryExpress.Tests.Data.Entity;
using QueryExpress.Tests.Data.Models;
using System.Globalization;
using System.Text.Json.Serialization;

namespace QueryExpress.Web.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers()
                .AddJsonOptions(opts =>
                {
                    opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
            // Enable CORS for the React app / development use
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddDbContext<TestDataContext>(options =>
            {
                options.UseInMemoryDatabase("PeopleDb");
                options.UseSeeding((ctx, _) =>
                {
                    ctx.Set<Person>().AddRange(SeedData(builder));
                    ctx.SaveChanges();
                });
            });
                

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var startupContext = scope.ServiceProvider.GetRequiredService<TestDataContext>();
                startupContext.Database.EnsureCreated();
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseCors("AllowAll");

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

        public static List<Person> SeedData(WebApplicationBuilder builder)
        {
            if(builder.Configuration.GetValue<bool>("AppSettings:UseSmallDataSet"))
            {
                return Person.GetTestPeople();
            }
            else
            {
                using var reader = new StreamReader($"{Environment.CurrentDirectory}\\People.csv");
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                var records = csv.GetRecords<Person>();
                return records.ToList();
            }
            
        }
    }
}
