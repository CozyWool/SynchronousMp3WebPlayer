using WebApplication1;

CreateHostBuilder(args).Build().Run();
return;

static IHostBuilder CreateHostBuilder(string[] args)
{
    return Host.CreateDefaultBuilder(args)
               .ConfigureAppConfiguration(builder => builder.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json"))
               .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
}