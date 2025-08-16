using SynchronousMp3WebPlayer;
using SynchronousMp3WebPlayer.Helpers;

try
{
    CreateHostBuilder(args).Build().Run();
}
catch (Exception ex)
{
    Console.WriteLine(ex);
}
finally
{
    Console.Write("Удалить все файлы песен с диска? (y/n): ");
    if (Console.ReadLine()?.ToLower() == "y")
    {
        FileManager.DeleteFilesInDirectory("wwwroot/music");
    }
    else
    {
        Console.WriteLine("Песни не удалены.");
    }
}

return;

static IHostBuilder CreateHostBuilder(string[] args)
{
    DotNetEnv.Env.Load();
    return Host.CreateDefaultBuilder(args)
               .ConfigureAppConfiguration(builder =>
                                              builder
                                                  .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json"))
               .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
}