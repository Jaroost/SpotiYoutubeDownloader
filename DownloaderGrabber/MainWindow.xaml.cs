using DotNetTools.SharpGrabber;
using DotNetTools.SharpGrabber.Converter;
using DotNetTools.SharpGrabber.Grabbed;
using Microsoft.Extensions.Configuration;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DownloaderGrabber
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<YoutubeDownloader> YoutubeDownloaders { get; set; } = new ObservableCollection<YoutubeDownloader>();
        public IConfigurationRoot configuration;
        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
            configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .Build();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            YoutubeDownloaders.Add(new YoutubeDownloader("https://www.youtube.com/watch?v=ymNFyxvIdaM"));
            YoutubeDownloaders.Add(new YoutubeDownloader("https://www.youtube.com/watch?v=q7PieJatM_k"));
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var config = SpotifyClientConfig
            .CreateDefault()
            .WithAuthenticator(new ClientCredentialsAuthenticator(configuration["SpotifyClientId"], configuration["SpotifySecret"]));


            var spotify = new SpotifyClient(config);

            var paging = await spotify.Playlists.GetItems("4A64AfkCrZ0B8orJ6kpWPH");
            await foreach (var item in spotify.Paginate(paging))
            {
                var track=(FullTrack)item.Track;
                var artists=string.Join(", ", track.Artists.Select(artist=>artist.Name).ToList());
                var fullName = $"{track.Name}-{artists}";
                //var fullName = item.Track.Album.Name + " " + item.Track.Name;
                // you can use "break" here!
            }
        }
    }
}
