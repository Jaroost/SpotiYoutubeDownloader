﻿using DotNetTools.SharpGrabber.Grabbed;
using DotNetTools.SharpGrabber;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO.Pipes;
using FFMpegCore;
using FFMpegCore.Enums;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;

namespace DownloaderGrabber
{
    public class Track: IProgress<float>, INotifyPropertyChanged
    {
    
        public string Name { get; set; }
        public List<string> Artists { get; set; } = new List<string>();

        public string YoutubeUrl { get; set; } = null;

        [JsonIgnore]
        public string YoutubeSearch
        {
            get
            {
                return $"{Name} - {string.Join('/', Artists)}";
            }
        }
        [JsonIgnore]
        public GrabbedMedia AudioMedia { get; set; } = null;
        [JsonIgnore]
        public GrabResult GrabResult { get; set; } = null;

        [JsonIgnore]
        public string InputFilename { get; set; } = string.Empty;
        [JsonIgnore]
        public string OutputFilename { get; set; } = string.Empty;
        [JsonIgnore]
        public float Progress { get; set; }
        [JsonIgnore]
        public int PercentProgress { 
            get
            {
                return (int)(Progress * 100);
            }
        }
        [JsonIgnore]
        public bool IsFinished { get; set; } = false;
        public string Step { get; set; } = "Waiting to start";
        [JsonIgnore]
        public string DownloadFolder { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "download");
        [JsonIgnore]
        public string FullInputFilename { 
            get
            {
                return Path.Combine(DownloadFolder,"input", InputFilename);
            } 
        }
        [JsonIgnore]
        public string FullOutputFilename
        {
            get
            {
                return Path.Combine(DownloadFolder, "output", OutputFilename);
            }
        }
        [JsonIgnore]
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();
        private IConfigurationRoot configuration;
        public Track(string name, List<string> artists, IConfigurationRoot configuration) {
            Name = name;
            Artists = artists;
            this.configuration = configuration;
            Directory.CreateDirectory(FullInputFilename);
            Directory.CreateDirectory(FullOutputFilename);
            //DoWork();
        }

        public Track()
        {
            void
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public async Task DoWork()
        {
            //await SearchYoutubeVideo();
            await GrabYoutubeInformation();
            if (!File.Exists(FullOutputFilename))
            {
                if (!File.Exists(FullInputFilename))
                {
                    await Download();
                }
                await ConvertToAAC();                
            }
            Step = "Extraction finished";
            Progress = 1;
            IsFinished = true;
        }

        //private async Task SearchYoutubeVideo()
        //{
        //    Step = $"Searching Youtube video with ({YoutubeSearch})";
        //    Progress = 0;
        //    try
        //    {
        //        var youtubeService = new YouTubeService(new BaseClientService.Initializer()
        //        {
        //            ApiKey = configuration["GoogleKey"],
        //            ApplicationName = this.GetType().ToString()
        //        });
        //        var searchListRequest = youtubeService.Search.List("snippet");
        //        searchListRequest.Q = YoutubeSearch;
        //        searchListRequest.MaxResults = 1;

        //        // Call the search.list method to retrieve results matching the specified query term.
        //        var searchListResponse = await searchListRequest.ExecuteAsync();

        //        List<string> videos = new List<string>();

        //        var item = searchListResponse.Items.FirstOrDefault(search => search.Id.Kind == "youtube#video");
        //        if (item != null)
        //        {
        //            YoutubeUri = $"https://www.youtube.com/watch?v={item.Id.VideoId}";
        //        }
        //    }
        //    catch(Exception ex)
        //    {
        //        var t = 1;
        //    }           

        //    Progress = 1;
        //}

        private async Task ConvertToAAC() 
        {
            Step = "Convert audio to AAC";
            Progress = 0;
            await FFMpegArguments
            .FromFileInput(FullInputFilename)
            .OutputToFile(FullOutputFilename, true, options => options
                .WithAudioCodec(AudioCodec.Aac))
            .ProcessAsynchronously();
            Progress = 1;
        }

        private async Task GrabYoutubeInformation()
        {
            Step = "Grab information from Youtube";
            Progress = 0;
            var grabber = GrabberBuilder.New()
                .UseDefaultServices()
                .AddYouTube()
                .Build();
            GrabResult = await grabber.GrabAsync(new Uri(YoutubeUrl));
            var mediaFiles = GrabResult.Resources<GrabbedMedia>().ToArray();
            AudioMedia= mediaFiles.GetHighestQualityAudio();
            InputFilename = $"{GrabResult.Title}.{AudioMedia.Container}";
            OutputFilename = $"{GrabResult.Title}.aac";
            Progress = 1;
        }

        private async Task Download()
        {
            Step = "Download audio from Youtube";
            Progress = 0;
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5);

                // Create a file stream to store the downloaded data.
                // This really can be any type of writeable stream.
                using (var file = new FileStream(FullInputFilename, FileMode.Create, FileAccess.Write, FileShare.None))
                {

                    // Use the custom extension method below to download the data.
                    // The passed progress-instance will receive the download status updates.
                    await client.DownloadAsync(AudioMedia.ResourceUri.ToString(), file, this, CancellationTokenSource.Token);
                }
            }
        }

        public void Report(float value)
        {
            Progress = value;
        }
    }
}