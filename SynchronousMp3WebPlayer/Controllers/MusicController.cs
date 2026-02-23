using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SynchronousMp3WebPlayer.Models;
using Yandex.Music.Api.Models.Track;
using Yandex.Music.Client;

namespace SynchronousMp3WebPlayer.Controllers;

[Route("music")]
public class MusicController(IConfiguration configuration, IMemoryCache cache, ILogger<MusicController> logger) : Controller
{
    private static readonly char[] InvalidChars = ['/', '\\', '?', '|', '>', '<', ':', '*', '"', '#'];
    private readonly string _coverFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "covers");

    private readonly string _tokenVlad =
        configuration.GetValue<string>("TOKEN_VLAD") ?? throw new NullReferenceException();

    private readonly string _tokenElvir =
        configuration.GetValue<string>("TOKEN_ELVIR") ?? throw new NullReferenceException();

    private readonly string _tokenMakar =
        configuration.GetValue<string>("TOKEN_MAKAR") ?? throw new NullReferenceException();


    [HttpGet("index")]
    [Route("/")]
    public IActionResult Index()
    {
        var model = new IndexViewModel
                    {
                        Tracks =
                        [
                            ([], "Влада"),
                            ([], "Эльвира"),
                            ([], "Макара")
                        ]
                    };

        ViewData["Title"] = "OTT Плеер";
        return View(model);
    }

    [HttpGet("liked")]
    public async Task<IActionResult> GetLikedAsync(string user, int skip = 0, int take = 20)
    {
        var token = user switch
                    {
                        "vlad"  => _tokenVlad,
                        "elvir" => _tokenElvir,
                        "makar" => _tokenMakar,
                        _       => throw new ArgumentException("Unknown user")
                    };

        var tracks = GetLikedTracksCached(user, token)
                     .Skip(skip)
                     .Take(take)
                     .ToList();

        var trackDtos = new List<object>();
        foreach (var yTrack in tracks)
        {
            var validFileName = string.Concat(yTrack.Title.Split(InvalidChars, StringSplitOptions.RemoveEmptyEntries));
            var validAuthorName = string.Concat((yTrack.Artists.FirstOrDefault()?.Name ?? "Unknown")
                                                .Split(InvalidChars, StringSplitOptions.RemoveEmptyEntries));
            var localFileName = $"{validFileName}_artist_{validAuthorName}.mp3";

            var coverUri = await GetLocalCoverPathAsync(yTrack);

            trackDtos.Add(new
                          {
                              id = yTrack.Id,
                              fileName = $"/music/{localFileName}",
                              title = yTrack.Title,
                              author = yTrack.Artists.FirstOrDefault()?.Name ?? "Неизвестен",
                              coverUri
                          });
        }

        return Json(trackDtos);
    }

    private List<YTrack> GetLikedTracksCached(string user, string token)
    {
        return cache.GetOrCreate(user, entry =>
                                       {
                                           entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

                                           var client = new YandexMusicClient();
                                           client.Authorize(token);
                                           return client.GetLikedTracks();
                                       })!;
    }

    private async Task<string> GetLocalCoverPathAsync(YTrack yTrack)
    {
        if (!Directory.Exists(_coverFolder))
        {
            Directory.CreateDirectory(_coverFolder);
        }

        var invalidChars = new[] { '/', '\\', '?', '|', '>', '<', ':', '*', '"', '#'};
        var fileName = string.Concat(yTrack.Title.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        var authorName = string.Concat((yTrack.Artists.FirstOrDefault()?.Name ?? "Unknown")
                                       .Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        var localFile = Path.Combine(_coverFolder, $"{fileName}_artist_{authorName}.jpg");

        if (!System.IO.File.Exists(localFile) && !string.IsNullOrEmpty(yTrack.CoverUri))
        {
            logger.LogInformation("Обложка '{SongTitle}' не найдена, пытаюсь скачать с ЯМ...", yTrack.Title);
            using var client = new HttpClient();
            var url = "https://" + yTrack.CoverUri[..^2] + "400x400";
            try
            {
                var bytes = await client.GetByteArrayAsync(url);
                await System.IO.File.WriteAllBytesAsync(localFile, bytes);
            }
            catch
            {
                logger.LogError("Обложку '{SongTitle}' не получилось скачать с ЯМ.", yTrack.Title);
                return "/img/no-cover_200x200.jpg";
            }
        }

        return System.IO.File.Exists(localFile)
                   ? $"/covers/{Path.GetFileName(localFile)}"
                   : "/img/no-cover_200x200.jpg"; // fallback
    }


    // [HttpPost("download")]
    // public IActionResult Download(string token)
    // {
    //     var client = new YandexMusicClient();
    //     client.Authorize(token);
    //     var tracks = client.GetLikedTracks();
    //     var api = new YandexMusicApi();
    //     var authStorage = new AuthStorage();
    //     api.User.Authorize(authStorage, token);
    //
    //     foreach (var yTrack in tracks)
    //     {
    //         var invalidChars = new[] {'/', '\\', '?', '|', '>', '<', ':', '*', '"'};
    //         var validFileName = string.Concat(yTrack.Title.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    //         string validAuthorName;
    //         if (yTrack.Artists.FirstOrDefault() is null)
    //         {
    //             validAuthorName = "Unknown";
    //         }
    //         else
    //         {
    //             validAuthorName = string.Concat(yTrack.Artists.First().Name
    //                                                   .Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    //         }
    //
    //         if (!System.IO.File.Exists($"wwwroot/music/{validFileName}_artist_{validAuthorName}.mp3"))
    //         {
    //             api.Track.ExtractToFile(authStorage, yTrack,
    //                                     $"wwwroot/music/{validFileName}_artist_{validAuthorName}.mp3");
    //         }
    //     }
    //
    //     return Ok();
    // }
}