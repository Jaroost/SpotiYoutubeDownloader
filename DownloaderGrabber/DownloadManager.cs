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
using System.Windows.Automation;

namespace DownloaderGrabber
{
    public class DownloadManager : INotifyPropertyChanged, IDisposable
    {

        public SemaphoreSlim conversionSemaphore { get; set; }= new SemaphoreSlim(1,1);
        public Object serializeLock { get; set; } = new Object();
        public Object seleniumLock { get; set; } = new object();
        public bool IsRunning { get; set; } = true;
        public bool IsFinished { get; set; } = true;

        public string SpotifyPlaylistId { get; set; }

        public bool IsSeleniumHeadless { get; set; }

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

        public ObservableCollection<Track> Tracks { get; set; } = new ObservableCollection<Track>();

        public string Step { get; set; } = "Waiting to start";

        private IConfigurationRoot configuration;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void ReportTracksProgress()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Progression)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProgressionString)));
        }

        public string ProgressionString
        {
            get
            {
                return $"{Progression} %";
            }
        }

        public int Progression
        {
            get
            {
                if(Tracks != null && Tracks.Count>0)
                {
                    return Tracks.Where(t => t.IsFinished).ToList().Count * 100 / Tracks.Count;
                }
                else
                {
                    return 0;
                }
            }
        }

        public DownloadManager(string spotifyPlaylistId, IConfigurationRoot configuration, int concurentThreads, int concurrentSeleniums, bool isSeleniumHeadless = false)
        {
            SpotifyPlaylistId = spotifyPlaylistId;
            IsSeleniumHeadless = isSeleniumHeadless;
            ConcurentThreads = concurentThreads;
            ConcurrentSeleniums = concurrentSeleniums;
            Directory.CreateDirectory(PlaylistsDirectory);
            this.configuration = configuration;
        }

        public async Task DoWork()
        {
            IsFinished = false;
            await Deserialize();
            RemoveDuplicates();
            if(Tracks.Count > 0)
            {
                await CreateConcurrentDrivers();
                await DownloadTracks();
            }
            IsFinished = true;
        }

        private Task CreateConcurrentDrivers()
        {
            return Task.Run(() =>
            {
                
                for (var i = 0; i < ConcurrentSeleniums; i++)
                {
                    Step = $"Creating {ConcurrentSeleniums-(i+1)} seleniums";
                    IWebDriver driver;
                    try
                    {
                        driver = CreateChromeDriver(IsSeleniumHeadless);                        
                    }
                    catch(Exception ex)
                    {
                        driver = CreateFirefoxDriver(IsSeleniumHeadless);
                    }

                    driver.Url = "https://www.youtube.com/?hl=FR";
                    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(100);
                    var consentForm = driver.FindElement(By.CssSelector("[aria-label = \"Accepter l'utilisation de cookies et d'autres données aux fins décrites\"]"));
                    consentForm?.Click();
                    FreeDrivers.Enqueue(driver);
                    AllDrivers.Add(driver);
                }
            });
        }

        private IWebDriver CreateChromeDriver(bool headless = false)
        {
            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            allServices.Add(service);
            var options = new ChromeOptions();
            if (headless)
            {
                options.AddArguments("--headless=new");
            }
            var driver = new ChromeDriver(service, options);
            return driver;
        }

        private IWebDriver CreateFirefoxDriver(bool headless = false)
        {
            var service = FirefoxDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            allServices.Add(service);
            var options = new FirefoxOptions();
            if (headless)
            {
                options.AddArguments("--headless");
            }
            var driver = new FirefoxDriver(service, options);
            return driver;
        }

        public Queue<IWebDriver> FreeDrivers { get; set; } =new Queue<IWebDriver>();
        public List<IWebDriver> AllDrivers { get; set; } = new List<IWebDriver>();
        private List<DriverService> allServices = new List<DriverService>();
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

        public IWebDriver? GetFreeSelenium()
        {
            IWebDriver driver = null;
            lock (seleniumLock)
            {
                if (FreeDrivers.Count > 0)
                {
                    driver = FreeDrivers.Dequeue();
                }
            }
            return driver;
        }

        public bool ReleaseSelenium(IWebDriver driver)
        {
            lock(seleniumLock)
            {
                FreeDrivers.Enqueue(driver);
                return true;
            }
        } 

        private async Task DownloadTracks()
        {
            Step = "Downloading tracks";
            var allTasks= new List<Func<Task>>();
            foreach (var track in Tracks)
            {
                allTasks.Add(()=>track.DoWork(this));
            }
            await InvokeAsync(allTasks, maxDegreeOfParallelism: ConcurentThreads);
        }

        public async Task Deserialize()
        {
            Step = "Extracting tracks from spotify playlist";
            if (File.Exists(SpotifyPlaylistJsonFile))
            {
                try
                {
                    await SpotifyInformations();
                    var tracks = JsonSerializer.Deserialize<ObservableCollection<Track>>(File.ReadAllText(SpotifyPlaylistJsonFile));
                    foreach(var trak in tracks)
                    {
                        trak.DownloadManager = this;
                        var alreadyTrack = Tracks.FirstOrDefault(t => t.YoutubeSearch == trak.YoutubeSearch);
                        if(alreadyTrack != null)
                        {
                            Tracks.Remove(alreadyTrack);
                        }
                        Tracks.Add(trak);
                    }
                    
                    //Tracks = tracks;

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
            try
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

                    Tracks.Add(new Track(track.Name, track.Artists.Select(artist => artist.Name).ToList(), configuration, this));
                }
            }catch(Exception e)
            {
                MessageBox.Show("The spotify playlist id seems to be invalid!");
                Tracks = new ObservableCollection<Track>();
            }            
        }

        public void RemoveDuplicates()
        {
            var list = Tracks.GroupBy(t => $"{t.Name}-{string.Join(',', t.Artists)}").Select(group => group.First()).ToList();
            Tracks = new ObservableCollection<Track>(list); 
        }

        public void Serialize()
        {
            lock (serializeLock)
            {
                string json = JsonSerializer.Serialize(Tracks, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SpotifyPlaylistJsonFile, json);
            }            
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
