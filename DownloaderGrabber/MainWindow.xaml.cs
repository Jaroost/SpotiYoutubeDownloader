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
            DownloadManager = new DownloadManager(SpotifyPlaylistId, configuration);
            await DownloadManager.DoWork();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            IWebDriver driver = new FirefoxDriver();
            driver.Url = "https://www.youtube.com/?hl=FR";
            driver.Manage().Timeouts().ImplicitWait=TimeSpan.FromSeconds(100);
            var element=driver.FindElement(By.CssSelector("[aria-label = \"Accepter l'utilisation de cookies et d'autres données aux fins décrites\"]"));
            element.Click();

            var input = driver.FindElement(By.Id("search-input"));
            input.Click();
            new OpenQA.Selenium.Interactions.Actions(driver).SendKeys("If you want to sing out sign out-Cat stevens / Yusuf").Perform();
            var searchBtn=driver.FindElement(By.CssSelector("button[aria-label = \"Rechercher\"]"));
            searchBtn.Click();
        }
    }
}
