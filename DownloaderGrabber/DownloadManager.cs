using Microsoft.Extensions.Configuration;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using Jint.Native;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium;
using System.Threading;
using Google.Apis.YouTube.v3.Data;
using System.Windows.Threading;
using System.Windows;

namespace DownloaderGrabber
{
    public class DownloadManager
    {
        public string SpotifyPlaylistId { get; set; }

        public string PlaylistsDirectory
        {
            get
            {
                return Path.Combine(Directory.GetCurrentDirectory(), "SpotifyPlaylists");
            }
        }

        public string SpotifyPlaylistJsonFile { 
            get
            {
                return Path.Combine(PlaylistsDirectory, $"{SpotifyPlaylistId}.json");
            } 
        }

        public ObservableCollection<Track> Tracks { get; set; }

        public string Step { get; set; } = "Waiting to start";

        private IConfigurationRoot configuration;
        public DownloadManager(string spotifyPlaylistId, IConfigurationRoot configuration)
        {
            SpotifyPlaylistId = spotifyPlaylistId;
            Directory.CreateDirectory(PlaylistsDirectory);
            this.configuration = configuration;
        }

        public async Task DoWork()
        {
            await Deserialize();
            await GetAllYoutubeUrls();
        }

        private Task GetAllYoutubeUrls()
        {
            return Task.Run(() =>
            {
                IWebDriver driver = new FirefoxDriver();
                driver.Url = "https://www.youtube.com/?hl=FR";
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(100);
                var consentForm = driver.FindElement(By.CssSelector("[aria-label = \"Accepter l'utilisation de cookies et d'autres données aux fins décrites\"]"));
                consentForm?.Click();


                foreach (var track in Tracks)
                {

                    if (string.IsNullOrEmpty(track.YoutubeUrl))
                    {
                        driver.Url = "https://www.youtube.com/?hl=FR";
                        var searchInput = driver.FindElement(By.Id("search-input"));
                        searchInput.Click();
                        new OpenQA.Selenium.Interactions.Actions(driver).SendKeys(track.YoutubeSearch).Perform();
                        var searchButton = driver.FindElement(By.CssSelector("button[aria-label = \"Rechercher\"]"));
                        searchButton.Click();
                        Thread.Sleep(3000);
                        driver.FindElement(By.CssSelector("ytd-video-renderer")).Click();
                        track.YoutubeUrl = driver.Url;
                        Serialize();
                    }
                   
                }
            });
        }

        public async Task Deserialize()
        {
            if (File.Exists(SpotifyPlaylistJsonFile))
            {
                try
                {
                    var traks = JsonSerializer.Deserialize<ObservableCollection<Track>>(File.ReadAllText(SpotifyPlaylistJsonFile));
                    Tracks = traks;
                }
                catch (Exception e)
                {
                    await SpotifyInformations();
                }
            }
            else
            {
                await SpotifyInformations();
            }
           
        }

        private async Task SpotifyInformations()
        {
            var config = SpotifyClientConfig.CreateDefault().WithAuthenticator(
                    new ClientCredentialsAuthenticator(configuration["SpotifyClientId"], configuration["SpotifySecret"]));


            var spotify = new SpotifyClient(config);
            var paging = await spotify.Playlists.GetItems(SpotifyPlaylistId);
            var allTraks = await spotify.PaginateAll(paging);

            Tracks = new ObservableCollection<Track>();
            foreach (var item in allTraks)
            {
                var track = (FullTrack)item.Track;

                Tracks.Add(new Track(track.Name, track.Artists.Select(artist => artist.Name).ToList(), configuration));
            }
            Serialize();
        }

        private void Serialize()
        {
            string json = JsonSerializer.Serialize(Tracks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SpotifyPlaylistJsonFile, json);
        }
    }
}
