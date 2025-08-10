namespace SynchronousMp3WebPlayer.Models;

public class SongModel
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string FileName { get; set; }
    public string CoverUri { get; set; }
    public int QueueIndex { get; set; }
}