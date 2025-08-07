using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SynchronousMp3WebPlayer.Models;
using Yandex.Music.Api;
using Yandex.Music.Api.Common;
using Yandex.Music.Api.Models.Track;
using Yandex.Music.Client;

namespace SynchronousMp3WebPlayer.Controllers;

[Route("music")]
public class MusicController : Controller
{
    private readonly string _tokenVlad;
    private readonly string _tokenElvir;

    public MusicController(IConfiguration configuration)
    {
        _tokenVlad = configuration.GetValue<string>("tokenVlad") ?? throw new NullReferenceException();
        _tokenElvir = configuration.GetValue<string>("tokenElvir") ?? throw new NullReferenceException();
    }

    [HttpGet("index")]
    [Route("/")]
    public IActionResult Index()
    {
        var clientVlad = new YandexMusicClient();
        clientVlad.Authorize(_tokenVlad);
        var tracksVlad = clientVlad.GetLikedTracks();
        var clientElvir = new YandexMusicClient();
        clientElvir.Authorize(_tokenElvir);
        var tracksElvir = clientElvir.GetLikedTracks();
        var model = new IndexViewModel
        {
           Tracks = new List<(List<YTrack>, string)>
                    {
                        (tracksVlad, "Влада"),
                        (tracksElvir, "Эльвира"),
                    }
        };
        
        ViewData["Title"] = "OTT Плеер";
        return View(model);
    }

    [HttpPost("download")]
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
}