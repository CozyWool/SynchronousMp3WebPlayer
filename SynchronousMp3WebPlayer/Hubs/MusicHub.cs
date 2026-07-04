using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using SynchronousMp3WebPlayer.Helpers;
using SynchronousMp3WebPlayer.Models;
using Yandex.Music.Api;
using Yandex.Music.Api.Extensions.API;
using Yandex.Music.Client;

namespace SynchronousMp3WebPlayer.Hubs;

public class MusicHub : Hub
{
    private readonly ILogger _logger;
    private static SongModel? CurrentSong { get; set; }
    private static int CurrentSongIndex { get; set; }
    private static List<SongModel> SongQueue { get; set; } = [];
    private static List<UserModel> Users { get; } = [];
    private static bool IsHostGranted { get; set; }
    private static bool IsPlaying { get; set; }
    private static decimal LastPlaybackPosition { get; set; }
    private static DateTimeOffset LastPlaybackPositionUpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    private readonly string _yandexTokenVlad;
    private readonly string _yandexTokenElvir;
    private readonly string _yandexTokenMakar;
    private static bool _loadedQueueFromFile;

    public MusicHub(IConfiguration configuration, ILogger<MusicHub> logger)
    {
        _logger = logger;
        _yandexTokenVlad = configuration.GetValue<string>("TOKEN_VLAD") ?? throw new NullReferenceException();
        _yandexTokenElvir = configuration.GetValue<string>("TOKEN_ELVIR") ?? throw new NullReferenceException();
        _yandexTokenMakar = configuration.GetValue<string>("TOKEN_MAKAR") ?? throw new NullReferenceException();
        if (File.Exists("wwwroot/queue.json") && !_loadedQueueFromFile)
        {
            SongQueue = JsonConvert.DeserializeObject<List<SongModel>>(File.ReadAllText("wwwroot/queue.json")) ?? [];
            _loadedQueueFromFile = true;
        }
    }

    private void LogSong(string message, SongModel currentSong)
    {
        _logger.Log(LogLevel.Information, "[{ContextConnectionId}] {message}: {CurrentSongTitle} - {CurrentSongAuthor}",
                    Context.ConnectionId, message, currentSong.Title, currentSong.Author);
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

        if (CurrentSong is not null)
        {
            if (IsPlaying)
            {
                await Clients.Caller.SendAsync("PlaySong", GetCurrentPlaybackPosition());
            }
            else
            {
                await Clients.Caller.SendAsync("PauseSong", LastPlaybackPosition);
            }
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
        LogSong("Now playing", CurrentSong);
        StartPlayback(0);
        await Clients.All.SendAsync("ChangeSong", song);
        await Clients.All.SendAsync("PlaySong", LastPlaybackPosition);
    }

    private async Task CheckFile(SongModel song)
    {
        if (!File.Exists("wwwroot/" + song.FileName))
        {
            _logger.LogInformation("'{SongTitle}' не найдено, пытаюсь скачать с ЯМ...", song.Title);
            await DownloadSong(song);
        }
    }

    private async Task DownloadSong(SongModel song)
    {
        try
        {
            var songDownloader = new SongDownloader();
            List<string> tokens = [_yandexTokenVlad, _yandexTokenElvir, _yandexTokenMakar];
            foreach (var token in tokens)
            {
                var isUserSong = await songDownloader.TryDownloadSong(song, token);
                if (isUserSong)
                {
                    return;
                }
            }

            _logger.LogError("'{SongTitle}' не получилось скачать с ЯМ.", song.Title);
            throw new ArgumentException();
        }
        catch (Exception ex)
        {
            _logger.LogError("'Message: {Message}'\nStackTrace: {StackTrace}", ex.Message, ex.StackTrace);
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
        LogSong("Now playing", CurrentSong);
        StartPlayback(0);
        await Clients.All.SendAsync("ChangeSong", song);
        await Clients.All.SendAsync("PlaySong", LastPlaybackPosition);
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
        LogSong("Now playing", CurrentSong);
        StartPlayback(0);
        await Clients.All.SendAsync("ChangeSong", song);
        await Clients.All.SendAsync("PlaySong", LastPlaybackPosition);
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
        LogSong("Now playing", CurrentSong);
        StartPlayback(0);
        await Clients.All.SendAsync("ChangeSong", song);
        await Clients.All.SendAsync("PlaySong", LastPlaybackPosition);
    }

    public async Task PauseSong(decimal time)
    {
        PausePlayback(time);
        await Clients.Others.SendAsync("PauseSong", LastPlaybackPosition);
    }

    public async Task PlaySong(decimal time)
    {
        StartPlayback(time);
        await Clients.Others.SendAsync("PlaySong", LastPlaybackPosition);
    }

    private static decimal GetCurrentPlaybackPosition()
    {
        if (!IsPlaying)
        {
            return LastPlaybackPosition;
        }

        var elapsedSeconds = (decimal)(DateTimeOffset.UtcNow - LastPlaybackPositionUpdatedAt).TotalSeconds;
        return LastPlaybackPosition + elapsedSeconds;
    }

    private static void StartPlayback(decimal time)
    {
        LastPlaybackPosition = Math.Max(0, time);
        LastPlaybackPositionUpdatedAt = DateTimeOffset.UtcNow;
        IsPlaying = true;
    }

    private static void PausePlayback(decimal time)
    {
        LastPlaybackPosition = Math.Max(0, time);
        LastPlaybackPositionUpdatedAt = DateTimeOffset.UtcNow;
        IsPlaying = false;
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

    public async Task AddNextInQueue(SongModel song)
    {
        var newSongIndex = CurrentSongIndex + 1;
        song.QueueIndex = newSongIndex;
        for (var i = CurrentSongIndex + 1; i < SongQueue.Count; i++)
        {
            SongQueue[i].QueueIndex++;
        }

        SongQueue.Insert(newSongIndex, song);

        await Clients.All.SendAsync("AddNextInQueue", song);
        await CheckFile(song);
        if (SongQueue.Count == 1)
        {
            await ChangeSongByQueueIndex("0");
        }
    }

    public async Task RemoveFromQueue(string queueIndex)
    {
        var index = int.TryParse(queueIndex, out var queueIndexInt) ? queueIndexInt : -1;
        if (index < 0 || index >= SongQueue.Count)
        {
            return;
        }

        var removedCurrentSong = CurrentSong is not null && CurrentSong.QueueIndex == index;
        SongQueue.RemoveAt(index);
        ReindexQueue();

        if (SongQueue.Count == 0)
        {
            CurrentSong = null;
            CurrentSongIndex = 0;
            await Clients.All.SendAsync("ClearQueue");
            return;
        }

        if (removedCurrentSong)
        {
            CurrentSongIndex = Math.Min(index, SongQueue.Count - 1);
            await Clients.All.SendAsync("RemoveFromQueue", index, CurrentSongIndex);
            await ChangeSongByQueueIndex(CurrentSongIndex.ToString());
            return;
        }

        if (index < CurrentSongIndex)
        {
            CurrentSongIndex--;
        }

        await Clients.All.SendAsync("RemoveFromQueue", index, CurrentSongIndex);
    }

    public async Task MoveInQueue(string fromQueueIndex, string toQueueIndex)
    {
        var fromIndex = int.TryParse(fromQueueIndex, out var fromQueueIndexInt) ? fromQueueIndexInt : -1;
        var toIndex = int.TryParse(toQueueIndex, out var toQueueIndexInt) ? toQueueIndexInt : -1;
        if (fromIndex < 0 || fromIndex >= SongQueue.Count || toIndex < 0 || toIndex >= SongQueue.Count || fromIndex == toIndex)
        {
            return;
        }

        var song = SongQueue[fromIndex];
        SongQueue.RemoveAt(fromIndex);
        SongQueue.Insert(toIndex, song);

        if (fromIndex == CurrentSongIndex)
        {
            CurrentSongIndex = toIndex;
        }
        else if (fromIndex < CurrentSongIndex && toIndex >= CurrentSongIndex)
        {
            CurrentSongIndex--;
        }
        else if (fromIndex > CurrentSongIndex && toIndex <= CurrentSongIndex)
        {
            CurrentSongIndex++;
        }

        ReindexQueue();
        CurrentSong = SongQueue[CurrentSongIndex];
        await Clients.All.SendAsync("QueueReordered", SongQueue, CurrentSongIndex);
    }

    private static void ReindexQueue()
    {
        for (var i = 0; i < SongQueue.Count; i++)
        {
            SongQueue[i].QueueIndex = i;
        }
    }

    public async Task ClearQueue()
    {
        SongQueue.Clear();
        CurrentSongIndex = 0;
        CurrentSong = null;
        PausePlayback(0);
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
            var invalidChars = new[] {'/', '\\', '?', '|', '>', '<', ':', '*', '"', '#'};
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
                           Author = yTrack.Artists.Count > 0
                                        ? string.Join(", ", yTrack.Artists.Select(artist => artist.Name))
                                        : "Неизвестен",
                           CoverUri = yTrack.CoverUri is not null
                                          ? "https://" + yTrack.CoverUri[..^2] + "400x400"
                                          : "/img/no-cover_200x200.jpg"
                       };
            await AddToQueue(song);
        }
    }
}
