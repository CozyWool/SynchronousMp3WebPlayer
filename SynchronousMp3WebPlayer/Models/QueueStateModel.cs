namespace SynchronousMp3WebPlayer.Models;

public class QueueStateModel
{
    public int CurrentSongIndex { get; set; }
    public List<SongModel> Songs { get; set; } = [];
}
