using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloaderGrabber
{
    public class TrackInfo
    {
        public string Name { get; set; }
        public List<string> Artists { get; set; } = new List<string>();

        public string YoutubeUrl { get; set; } = null;

        public string YoutubeSearch
        {
            get
            {
                return $"{Name} - {string.Join('/', Artists)}";
            }
        }


        public TrackInfo(string name, List<string> artists)
        {
            Name = name;
            Artists = artists;
        }   
    
    }
}
