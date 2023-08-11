using DotNetTools.SharpGrabber.Grabbed;
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

namespace DownloaderGrabber
{
    public class YoutubeDownloader: IProgress<float>, INotifyPropertyChanged
    {
        public string YoutubeUri { get; set; }
        public GrabbedMedia AudioMedia { get; set; } = null;
        public GrabResult GrabResult { get; set; } = null;
        public string InputFilename { get; set; } = string.Empty;
        public string OutputFilename { get; set; } = string.Empty;

        public float Progress { get; set; }

        public int PercentProgress { 
            get
            {
                return (int)(Progress * 100);
            }
        }

        public string Step { get; set; } = "";

        public string DownloadFolder { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "download");

        public string FullInputFilename { 
            get
            {
                return Path.Combine(DownloadFolder,"webm", InputFilename);
            } 
        }

        public string FullOutputFilename
        {
            get
            {
                return Path.Combine(DownloadFolder, "mp3", OutputFilename);
            }
        }

        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();
        public YoutubeDownloader(string youtubeUri) {
            YoutubeUri = youtubeUri;
            Directory.CreateDirectory(FullInputFilename);
            Directory.CreateDirectory(FullOutputFilename);
            Step = "Waiting to start";
            DoWork();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public async Task DoWork()
        {
            Step = "Grab information from Youtube";
            await GrabInformation();
            Step = "Download audio from Youtube";
            await Download();
            Step = "Convert audio to MP3";
            await ConvertToMp3();
            Step = "Extraction finished";
            Progress = 1;
        }

        private async Task ConvertToMp3() 
        {
            Progress = 0;
            await FFMpegArguments
            .FromFileInput(FullInputFilename)
            .OutputToFile(FullOutputFilename, true, options => options
                .WithAudioCodec(AudioCodec.LibMp3Lame))
            .ProcessAsynchronously();
            Progress = 1;
        }

        private async Task GrabInformation()
        {
            Progress = 0;
            var grabber = GrabberBuilder.New()
                .UseDefaultServices()
                .AddYouTube()
                .Build();
            GrabResult = await grabber.GrabAsync(new Uri(YoutubeUri));
            var mediaFiles = GrabResult.Resources<GrabbedMedia>().ToArray();
            AudioMedia= mediaFiles.GetHighestQualityAudio();
            InputFilename = $"{GrabResult.Title}.{AudioMedia.Container}";
            OutputFilename = $"{GrabResult.Title}.mp3";
            if (File.Exists(FullInputFilename))
            {
                File.Delete(FullInputFilename);
            }

            if (File.Exists(FullOutputFilename))
            {
                File.Delete(FullOutputFilename);
            }
            Progress = 1;
        }

        private async Task Download()
        {
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
