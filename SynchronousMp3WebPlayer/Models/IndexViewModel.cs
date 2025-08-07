using System.Collections;
using Yandex.Music.Api.Models.Track;

namespace SynchronousMp3WebPlayer.Models;

public class IndexViewModel
{ 
    public List<(List<YTrack>, string)> Tracks { get; set; }
}