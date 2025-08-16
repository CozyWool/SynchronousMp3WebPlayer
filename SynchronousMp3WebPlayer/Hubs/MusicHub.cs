using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using SynchronousMp3WebPlayer.Models;
using Yandex.Music.Api;
using Yandex.Music.Api.Common;
using Yandex.Music.Client;

namespace SynchronousMp3WebPlayer.Hubs;

public class MusicHub : Hub
{
    private static SongModel? CurrentSong { get; set; }
    private static int CurrentSongIndex { get; set; }
    private static List<SongModel> SongQueue { get; set; } = new();
    private static List<UserModel> Users { get; } = new();
    private static bool IsHostGranted { get; set; }
    private readonly string _yandexTokenVlad;
    private readonly string _yandexTokenElvir;
    private static bool _loadedQueueFromFile;

    public MusicHub(IConfiguration configuration)
    {
        _yandexTokenVlad = configuration.GetValue<string>("TOKEN_VLAD") ?? throw new NullReferenceException();
        _yandexTokenElvir = configuration.GetValue<string>("TOKEN_ELVIR") ?? throw new NullReferenceException();
        if (File.Exists("wwwroot/queue.json") && !_loadedQueueFromFile)
        {
            SongQueue = JsonConvert.DeserializeObject<List<SongModel>>(File.ReadAllText("wwwroot/queue.json")) ?? [];
            _loadedQueueFromFile = true;
        }
    }

    public async Task JoinGroup()
    {
        if (CurrentSong is not null)
        {
            await Clients.Caller.SendAsync("ChangeSong", CurrentSong);
        }

        foreach (var song in SongQueue)
        {
            await Clients.Caller.SendAsync("AddToQueue", song);
        }

        var user = new UserModel
                   {
                       ConnectionId = Context.ConnectionId,
                   };
        if (!IsHostGranted)
        {
            user.IsHost = true;
            IsHostGranted = true;
            await Clients.Caller.SendAsync("BecomeHost");
        }

        Users.Add(user);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var user = Users.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
        if (user is not null)
        {
            if (user.IsHost)
            {
                IsHostGranted = false;
            }

            Users.Remove(user);
            if (Users.Count > 0 && !IsHostGranted)
            {
                var newHost = Users.First();
                newHost.IsHost = true;
                IsHostGranted = true;
                await Clients.Client(newHost.ConnectionId).SendAsync("BecomeHost");
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task ChangeSong(SongModel song)
    {
        await CheckFile(song);

        CurrentSong = song;
        var songInQueue = SongQueue.FirstOrDefault(x => x.Id == CurrentSong.Id);
        if (songInQueue is null)
        {
            CurrentSong.QueueIndex = SongQueue.Count;
            await AddToQueue(CurrentSong);
        }
        else
        {
            CurrentSong.QueueIndex = songInQueue.QueueIndex;
        }

        CurrentSongIndex = CurrentSong.QueueIndex;
        await Clients.All.SendAsync("ChangeSong", song);
    }

    private async Task CheckFile(SongModel song)
    {
        if (!File.Exists("wwwroot/" + song.FileName))
        {
            Console.WriteLine($"'{song.Title}' не найдено, пытаюсь скачать с ЯМ...");
            await DownloadSong(song);
        }
    }

    private async Task DownloadSong(SongModel song)
    {
        try
        {
            var apiVlad = new YandexMusicApi();
            var apiElvir = new YandexMusicApi();
            
            var vladAuthStorage = new AuthStorage();
            var elvirAuthStorage = new AuthStorage();
            
            await apiVlad.User.AuthorizeAsync(vladAuthStorage, _yandexTokenVlad);
            await apiElvir.User.AuthorizeAsync(elvirAuthStorage, _yandexTokenElvir);

            var isVladSong = true;
            var isElvirSong = false;
            var yTrack = (await apiVlad.Track.GetAsync(vladAuthStorage, song.Id)).Result.First();
            if (yTrack.Error is not null)
            {
                isVladSong = false;
                yTrack = (await apiElvir.Track.GetAsync(elvirAuthStorage, song.Id)).Result.First();
                if (yTrack.Error is not null)
                {
                    Console.WriteLine($"ERROR: '{song.Title}' не получилось скачать с ЯМ.");
                    return;
                }
            }
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

            if (!Directory.Exists("wwwroot/music/"))
            {
                Directory.CreateDirectory("wwwroot/music/");
            }

            if (!File.Exists($"wwwroot/music/{validFileName}_artist_{validAuthorName}.mp3"))
            {
                if (isVladSong)
                {
                    await apiVlad.Track.ExtractToFileAsync(vladAuthStorage,
                                                       yTrack,
                                                       $"wwwroot/music/{validFileName}_artist_{validAuthorName}.mp3");
                }
                else
                {
                    await apiElvir.Track.ExtractToFileAsync(elvirAuthStorage,
                                                           yTrack,
                                                           $"wwwroot/music/{validFileName}_artist_{validAuthorName}.mp3");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public async Task ChangeSongByQueueIndex(string queueIndex)
    {
        var index = int.TryParse(queueIndex, out var queueIndexInt) ? queueIndexInt : -1;
        if (index < 0 || index >= SongQueue.Count)
        {
            return;
        }

        var song = SongQueue[index];
        await CheckFile(song);

        CurrentSong = song;
        CurrentSongIndex = SongQueue.IndexOf(CurrentSong);
        await Clients.All.SendAsync("ChangeSong", song);
    }

    public async Task NextSong()
    {
        if (SongQueue.Count == 0)
        {
            return;
        }

        if (CurrentSongIndex == SongQueue.Count - 1)
        {
            CurrentSongIndex = 0;
        }
        else
        {
            CurrentSongIndex++;
        }

        var song = SongQueue[CurrentSongIndex];

        CurrentSong = song;
        await Clients.All.SendAsync("ChangeSong", song);
    }

    public async Task PreviousSong()
    {
        if (SongQueue.Count == 0)
        {
            return;
        }

        if (CurrentSongIndex == 0)
        {
            CurrentSongIndex = SongQueue.Count - 1;
        }
        else
        {
            CurrentSongIndex--;
        }

        var song = SongQueue[CurrentSongIndex];

        CurrentSong = song;
        await Clients.All.SendAsync("ChangeSong", song);
    }

    public async Task PauseSong()
    {
        await Clients.Others.SendAsync("PauseSong");
    }

    public async Task PlaySong(decimal time)
    {
        await Clients.Others.SendAsync("PlaySong", time);
    }

    public async Task AddToQueue(SongModel song)
    {
        song.QueueIndex = SongQueue.Count;
        SongQueue.Add(song);

        await Clients.All.SendAsync("AddToQueue", song);
        await CheckFile(song);
        if (SongQueue.Count == 1)
        {
            await ChangeSongByQueueIndex("0");
        }
    }

    public async Task ClearQueue()
    {
        SongQueue.Clear();
        CurrentSongIndex = 0;
        await Clients.All.SendAsync("ClearQueue");
    }

    public async Task ShuffleAlLSongs()
    {
        await ClearQueue();

        var clientVlad = new YandexMusicClient();
        var clientElvir = new YandexMusicClient();
        clientVlad.Authorize(_yandexTokenVlad);
        clientElvir.Authorize(_yandexTokenElvir);

        var random = new Random();
        var tracks = clientVlad.GetLikedTracks()
                               .Concat(clientElvir.GetLikedTracks())
                               .DistinctBy(x => x.Id)
                               .OrderBy(_ => random.Next())
                               .ToList();
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
                validAuthorName = string.Concat(yTrack.Artists
                                                      .First()
                                                      .Name
                                                      .Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            }

            var song = new SongModel
                       {
                           Id = yTrack.Id,
                           FileName = $"/music/{validFileName}_artist_{validAuthorName}.mp3",
                           Title = yTrack.Title,
                           Author = yTrack.Artists.Count > 0 ? yTrack.Artists[0].Name : "Неизвестен",
                           CoverUri = yTrack.CoverUri is not null
                                          ? "https://" + yTrack.CoverUri[..^2] + "200x200"
                                          : "/img/no-cover_200x200.jpg"
                       };
            await AddToQueue(song);
        }
    }
}