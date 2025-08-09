using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Yandex.Music.Api;
using Yandex.Music.Api.Common;
using Yandex.Music.Client;

namespace SynchronousMp3WebPlayer.Hubs;

public class Song
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string FileName { get; set; }
    public string CoverUri { get; set; }
    public int QueueIndex { get; set; }
}

public class User
{
    public string ConnectionId { get; set; }
    public bool IsHost { get; set; }
}

public class MusicHub : Hub
{
    private static Song? CurrentSong { get; set; }
    private static int CurrentSongIndex { get; set; }
    private static List<Song> SongQueue { get; set; } = new();
    private static List<User> Users { get; } = new();
    private static bool IsHostGranted { get; set; }
    private readonly string _tokenVlad;
    private readonly string _tokenElvir;
    private static bool _loadedQueueFromFile;
    public MusicHub(IConfiguration configuration)
    {
        _tokenVlad = configuration.GetValue<string>("tokenVlad") ?? throw new NullReferenceException();
        _tokenElvir = configuration.GetValue<string>("tokenElvir") ?? throw new NullReferenceException();
        if (File.Exists("wwwroot/queue.json") && !_loadedQueueFromFile)
        {
            SongQueue = JsonConvert.DeserializeObject<List<Song>>(File.ReadAllText("wwwroot/queue.json")) ?? new List<Song>();
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

        var user = new User
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

    public async Task ChangeSong(Song song)
    {
        if (!File.Exists("wwwroot/" + song.FileName))
        {
            Console.WriteLine($"{song.Title} не найдено, пытаюсь скачать с ЯМ");
            await DownloadSong(song);
        }

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

    private async Task DownloadSong(Song song)
    {
        var client = new YandexMusicClient();
        client.Authorize(_tokenVlad);
        var api = new YandexMusicApi();
        var authStorage = new AuthStorage();
        await api.User.AuthorizeAsync(authStorage, _tokenVlad);

        var yTrack = (await api.Track.GetAsync(authStorage, song.Id)).Result.First();
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

        if (!File.Exists($"wwwroot/music/{validFileName}_artist_{validAuthorName}.mp3"))
        {
            await api.Track.ExtractToFileAsync(authStorage,
                                               yTrack,
                                               $"wwwroot/music/{validFileName}_artist_{validAuthorName}.mp3");
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
        if (!File.Exists("wwwroot/" + song.FileName))
        {
            Console.WriteLine($"{song.Title} не найдено, пытаюсь скачать с ЯМ");
            await DownloadSong(song);
        }

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
        if (File.Exists("wwwroot/" + song.FileName))
        {
            Console.WriteLine($"{song.Title} exists");
        }

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
        if (File.Exists("wwwroot/" + song.FileName))
        {
            Console.WriteLine($"{song.Title} exists");
        }

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

    public async Task AddToQueue(Song song)
    {
        SongQueue.Add(song);
        if (SongQueue.Count == 1)
        {
            await ChangeSongByQueueIndex("0");
        }

        await Clients.All.SendAsync("AddToQueue", song);
    }

    public async Task ClearQueue()
    {
        SongQueue.Clear();
        CurrentSongIndex = 0;
        await Clients.All.SendAsync("ClearQueue");
    }
}