using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace SynchronousMp3WebPlayer.Hubs;

public class Song
{
    public string Title { get; set; }
    public string Author { get; set; }
    public string FileName { get; set; }
    public string CoverUri { get; set; }
    public int QueueIndex { get; set; }
}

public class MusicHub : Hub
{
    private static Song? CurrentSong { get; set; }
    private static int CurrentSongIndex { get; set; }
    private static List<Song> SongQueue { get; } = new();

    public MusicHub()
    {
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
    }

    public async Task ChangeSong(Song song)
    {
        if (File.Exists("wwwroot/" + song.FileName))
        {
            Console.WriteLine($"{song.Title} exists");
        }

        CurrentSong = song;
        CurrentSong.QueueIndex = SongQueue.Count;
        await AddToQueue(CurrentSong);
        CurrentSongIndex = SongQueue.Count - 1;
        await Clients.All.SendAsync("ChangeSong", song);
    }
    public async Task ChangeSongByQueueIndex(string queueIndex)
    {
        var index = int.TryParse(queueIndex, out var queueIndexInt) ? queueIndexInt : -1;
        if (index < 0 || index >= SongQueue.Count)
        {
            return;
        }
        var song = SongQueue[index];
        if (File.Exists("wwwroot/" + song.FileName))
        {
            Console.WriteLine($"{song.Title} exists");
        }

        CurrentSong = song;
        CurrentSongIndex = SongQueue.IndexOf(CurrentSong);
        await Clients.All.SendAsync("ChangeSong", song);
    }

    public async Task NextSong()
    {
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
        await Clients.All.SendAsync("AddToQueue", song);
    }
}