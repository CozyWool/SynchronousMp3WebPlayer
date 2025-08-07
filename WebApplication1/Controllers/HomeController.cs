using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using Yandex.Music.Api;
using Yandex.Music.Api.Common;
using Yandex.Music.Api.Common.Debug;
using Yandex.Music.Api.Models.Account;
using Yandex.Music.Client;

namespace WebApplication1.Controllers;

[Route("home")]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IConfiguration _configuration;
    private readonly string tokenVlad;
    private readonly string tokenElvir;

    public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        tokenVlad = _configuration.GetValue<string>("tokenVlad") ?? throw new NullReferenceException();
        tokenElvir = _configuration.GetValue<string>("tokenElvir") ?? throw new NullReferenceException();
    }

    [HttpGet("index")]
    [Route("/")]
    public IActionResult Index()
    {
        var clientVlad = new YandexMusicClient();
        clientVlad.Authorize(tokenVlad);
        var tracksVlad = clientVlad.GetLikedTracks();
        var clientElvir = new YandexMusicClient();
        clientElvir.Authorize(tokenElvir);
        var tracksElvir = clientElvir.GetLikedTracks();
        var model = new IndexViewModel
        {
           TracksVlad = tracksVlad,
           TracksElvir = tracksElvir
        };
        
        ViewData["Title"] = "OTT Плеер";
        return View(model);
    }

    [HttpGet("download")]
    public IActionResult Download(string token)
    {
        var client = new YandexMusicClient();
        client.Authorize(token);
        var tracks = client.GetLikedTracks();
        var api = new YandexMusicApi();
        var authStorage = new AuthStorage();
        api.User.Authorize(authStorage, token);

        foreach (var yTrack in tracks)
        {
            var invalidChars = new[] {'/', '\\', '?', '|', '>', '<', ':', '*', '"'};
            var validFileName = string.Concat(yTrack.Title.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            string validAuthorName;
            if (yTrack.Artists.FirstOrDefault() is null)
            {
                validAuthorName = "Unknown";
            }
            else
            {
                validAuthorName = string.Concat(yTrack.Artists.First().Name
                                                      .Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            }

            if (!System.IO.File.Exists($"wwwroot/music/{validFileName}_artist_{validAuthorName}.mp3"))
            {
                api.Track.ExtractToFile(authStorage, yTrack,
                                        $"wwwroot/music/{validFileName}_artist_{validAuthorName}.mp3");
            }
        }

        return Ok();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}