using SynchronousMp3WebPlayer.Hubs;

namespace SynchronousMp3WebPlayer;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSignalR(options => { options.MaximumReceiveMessageSize = 1024 * 1024 * 20; });
        services.AddHttpContextAccessor();
        services.AddHttpsRedirection(options =>
                                     {
                                         options.HttpsPort = _configuration.GetValue<int?>("HTTPS_PORT") ?? 7289;
                                     });
        services.AddMvc();
        services.AddMemoryCache();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseStaticFiles();
        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
                         {
                             endpoints.MapControllerRoute("default",
                                                          "{controller=Music}/{action=Index}");
                             endpoints.MapHub<MusicHub>("/musicHub");
                         });
    }
}
