using Microsoft.Extensions.Configuration;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloaderGrabber
{
    public class DownloadManager
    {
        public string SpotifyPlaylistId { get; set; }

        public ObservableCollection<YoutubeDownloader> YoutubeDownloaders { get; set; } = new ObservableCollection<YoutubeDownloader>();
        public string Step { get; set; } = "Waiting to start";

        private IConfigurationRoot configuration;
        public DownloadManager(string spotifyPlaylistId, IConfigurationRoot configuration)
        {
            SpotifyPlaylistId = spotifyPlaylistId;
            this.configuration = configuration;
        }

        public async Task DoWork()
        {
           await GetAllSpotifyInformation();
        }

        public async Task GetAllSpotifyInformation()
        {
            var config = SpotifyClientConfig
            .CreateDefault()
            .WithAuthenticator(new ClientCredentialsAuthenticator(configuration["SpotifyClientId"], configuration["SpotifySecret"]));


            var spotify = new SpotifyClient(config);
            var paging = await spotify.Playlists.GetItems(SpotifyPlaylistId);
            var allTraks = await spotify.PaginateAll(paging);

            int counter = 0;
            foreach (var item in allTraks)
            {
                var track = (FullTrack)item.Track;
                var artists = string.Join(", ", track.Artists.Select(artist => artist.Name).ToList());
                var fullName = $"{track.Name}-{artists}";

                if (counter < 5)
                {
                    YoutubeDownloaders.Add(new YoutubeDownloader(fullName, configuration));
                }                
                counter++;
            }
        }
    }
}
