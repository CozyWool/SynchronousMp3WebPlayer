using SynchronousMp3WebPlayer.Models;
using Yandex.Music.Api;
using Yandex.Music.Api.Common;
using Yandex.Music.Api.Models.Track;

namespace SynchronousMp3WebPlayer.Helpers;

public class SongDownloader(YandexMusicApi yApi)
{
    public SongDownloader() : this(new YandexMusicApi())
    {

    }

    public async Task<bool> TryDownloadSong(SongModel song, string token)
    {
        var (yTrack, authStorage) = await GetTrackByToken(song, token);
        if (yTrack.Error is not null)
        {
            return false;
        }

        await ExtractSongToFile(yTrack, authStorage);
        return true;
    }

    private async Task ExtractSongToFile(YTrack yTrack, AuthStorage authStorage)
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
            validAuthorName = string.Concat(yTrack.Artists.First().Name
                                                  .Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        }

        if (!Directory.Exists("wwwroot/music/"))
        {
            Directory.CreateDirectory("wwwroot/music/");
        }

        if (!File.Exists($"wwwroot/music/{validFileName}_artist_{validAuthorName}.mp3"))
        {
            await yApi.Track.ExtractToFileAsync(authStorage,
                                                yTrack,
                                                $"wwwroot/music/{validFileName}_artist_{validAuthorName}.mp3");
        }
    }

    private async Task<(YTrack, AuthStorage)> GetTrackByToken(SongModel song, string token)
    {
        var authStorage = new AuthStorage();
        await yApi.User.AuthorizeAsync(authStorage, token);
        var yTrack = (await yApi.Track.GetAsync(authStorage, song.Id)).Result.First();
        return (yTrack, authStorage);
    }
}