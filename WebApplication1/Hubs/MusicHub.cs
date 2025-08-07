using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace WebApplication1.Hubs;

public class Song
{
    public string Title { get; set; }
    public string Author { get; set; }
    public string FileName { get; set; }
    public string CoverUri { get; set; }
}

public class MusicHub : Hub
{
    public MusicHub()
    {
    }

    // public async Task JoinChat()
    // {
    //     
    // }

    public async Task ChangeSong(Song song)
    {
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
}