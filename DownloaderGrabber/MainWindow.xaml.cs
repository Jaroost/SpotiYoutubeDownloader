using DotNetTools.SharpGrabber;
using DotNetTools.SharpGrabber.Converter;
using DotNetTools.SharpGrabber.Grabbed;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Interactions;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.Emit;
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
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public DownloadManager DownloadManager { get; set; } = null;
        public IConfigurationRoot configuration;
        public int ConcurentThreads { get; set; } = 7;
        public int ConcurrentSeleniums { get; set; } = 4;
        public string SpotifyPlaylistId { get; set; } = "4A64AfkCrZ0B8orJ6kpWPH";
        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
            configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .Build();
        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            DownloadManager = new DownloadManager(SpotifyPlaylistId, configuration, ConcurentThreads, ConcurrentSeleniums);
            await DownloadManager.DoWork();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            DownloadManager?.Dispose();
        }
    }
}
