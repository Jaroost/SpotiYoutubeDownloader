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
        public int ConcurentThreads { get; set; } = 1;
        public int ConcurrentSeleniums { get; set; } = 1;
        public bool WantHeadless { get; set; } = false;
        public string SpotifyPlaylistId { get; set; } = "1dnM7WcHkCeWjNCmzbHtLJ";

        public MainWindow()
        {
            CheckDependencies();
            DataContext = this;
            InitializeComponent();
            configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .Build();
        }

        private void CheckDependencies() 
        {
            if (!File.Exists(System.IO.Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg.exe")))
            {
                MessageBox.Show("To convert audio to AAC this software use ffmpeg.exe please download it and place it in the same directory as this application (https://www.gyan.dev/ffmpeg/builds/)\nApplication will close");
                Environment.Exit(-1);
            }
        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            DownloadManager = new DownloadManager(SpotifyPlaylistId, configuration, ConcurentThreads, ConcurrentSeleniums, WantHeadless);
            await DownloadManager.DoWork();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            DownloadManager?.Dispose();
        }
    }
}
