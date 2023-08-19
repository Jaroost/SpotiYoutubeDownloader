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
using System.ComponentModel;
using OpenQA.Selenium.Chrome;
using System.Xaml;

namespace DownloaderGrabber
{
    public class DownloadManager : INotifyPropertyChanged, IDisposable
    {
        public bool IsRunning { get; set; } = true;
        public bool WorkInProgress { get; set; }

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

        public event PropertyChangedEventHandler? PropertyChanged;

        

        public DownloadManager(string spotifyPlaylistId, IConfigurationRoot configuration, int concurentThreads, int concurrentSeleniums)
        {
            SpotifyPlaylistId = spotifyPlaylistId;
            ConcurentThreads= concurentThreads;
            ConcurrentSeleniums = concurrentSeleniums;
            Directory.CreateDirectory(PlaylistsDirectory);
            this.configuration = configuration;
        }

        public async Task DoWork()
        {
            WorkInProgress = true;
            await Deserialize();
            await CreateConcurrentDrivers();
            await DownloadTracks();
            WorkInProgress = false;
        }

        private Task CreateConcurrentDrivers()
        {
            return Task.Run(() =>
            {
                Step = $"Creating {ConcurrentSeleniums} seleniums";
                for (var i = 0; i < ConcurrentSeleniums; i++)
                {

                    var service = ChromeDriverService.CreateDefaultService(); 
                    service.HideCommandPromptWindow = true;
                    allServices.Add(service);
                    var options = new ChromeOptions();
                    options.AddArguments("--headless=new");
                    var driver = new ChromeDriver(service, options);
                    driver.Url = "https://www.youtube.com/?hl=FR";
                    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(100);
                    var consentForm = driver.FindElement(By.CssSelector("[aria-label = \"Accepter l'utilisation de cookies et d'autres données aux fins décrites\"]"));
                    consentForm?.Click();
                    FreeDrivers.Enqueue(driver);
                    AllDrivers.Add(driver);
                }
            });
        }


        public Queue<IWebDriver> FreeDrivers { get; set; } =new Queue<IWebDriver>();
        public List<IWebDriver> AllDrivers { get; set; } = new List<IWebDriver>();
        private List<ChromeDriverService> allServices = new List<ChromeDriverService>();
        public int ConcurentThreads { get; set; } = 5;
        public int ConcurrentSeleniums { get; set; } = 4;


        static async Task InvokeAsync(IEnumerable<Func<Task>> taskFactories, int maxDegreeOfParallelism)
        {
            Queue<Func<Task>> queue = new Queue<Func<Task>>(taskFactories);

            if (queue.Count == 0)
            {
                return;
            }

            List<Task> tasksInFlight = new List<Task>(maxDegreeOfParallelism);

            do
            {
                while (tasksInFlight.Count < maxDegreeOfParallelism && queue.Count != 0)
                {
                    Func<Task> taskFactory = queue.Dequeue();

                    tasksInFlight.Add(taskFactory());
                }

                Task completedTask = await Task.WhenAny(tasksInFlight).ConfigureAwait(false);

                // Propagate exceptions. In-flight tasks will be abandoned if this throws.
                await completedTask.ConfigureAwait(false);

                tasksInFlight.Remove(completedTask);
            }
            while (queue.Count != 0 || tasksInFlight.Count != 0);
        }

        private async Task DownloadTracks()
        {
            Step = "Downloading tracks";
            List<Func<Task>> taskFactories = new List<Func<Task>>();
            foreach(var track in Tracks)
            {
                taskFactories.Add(()=>track.DoWork(this));
            }
            await InvokeAsync(taskFactories, maxDegreeOfParallelism: ConcurentThreads);



            //return Task.Run(() =>
            //{
            //    Step = "Getting youtube url from  track informations";



            //    for(var i=0;i<Tracks.Count; i++)
            //    {
            //        var track = Tracks[i];
            //        if (IsRunning)
            //        {
            //            Step = $"Getting youtube url from  track informations ({i + 1} / {Tracks.Count}, {(i + 1) * 100 / Tracks.Count}%)";
            //            if (string.IsNullOrEmpty(track.YoutubeUrl))
            //            {
            //                try
            //                {
            //                    track.SearchYoutubeUrl(driver);
            //                    Serialize();
            //                }
            //                catch { }

            //            }
            //        }
            //    }
            //});
        }

        public async Task Deserialize()
        {
            Step = "Extracting tracks from spotify playlist";
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

        public void Dispose()
        {
            foreach(var driver in AllDrivers)
            {
                driver.Dispose();
            }

            foreach(var service in allServices)
            {
                service.Dispose();
            }
        }
    }
}
