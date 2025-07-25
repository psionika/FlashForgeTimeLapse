using Accord.Video.FFMPEG;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FlashForgeTimeLapse.Helpers;
using FlashForgeTimeLapse.MJpegWrapper;
using FlashForgeTimeLapse.ThreadExecuters;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace FlashForgeTimeLapse.ViewModel
{
    public class MainViewModel : ObservableObject
    {
        #region Properties

        public string Title => "Flashforge 5M Timelapse";

        public string StreamUrl
        {
            get => Properties.Settings.Default.StreamUrl;
            set
            {
                if (value != null &&
                    value != string.Empty &&
                    value != Properties.Settings.Default.StreamUrl)
                {
                    Properties.Settings.Default.StreamUrl = value;
                    Properties.Settings.Default.Save();

                    OnPropertyChanged(nameof(StreamUrl));
                }
            }
        }

        private bool _isStarting = false;
        public bool IsStarting 
        { 
            get => _isStarting;
            set
            {
                if (value != _isStarting)
                {
                    _isStarting = value;

                    OnPropertyChanged(nameof(IsStarting));
                }
            }
        }

        private string _videoCodesString = "VP9";
        public string VideoCodecString
        {
            get => _videoCodesString;
            set
            {
                if (value != null && value != string.Empty &&
                    value != _videoCodesString)
                {
                    _videoCodesString = value;
                    OnPropertyChanged(nameof(VideoCodecString));
                }
            }
        }

        public string TimeoutScreenshot
        {
            get => Properties.Settings.Default.TimeoutScreenshot.ToString();
            set
            {
                if (value != null &&
                    value != string.Empty &&
                    value != Properties.Settings.Default.TimeoutScreenshot.ToString())
                {
                    try
                    {
                        Properties.Settings.Default.TimeoutScreenshot = Convert.ToInt32(value);
                        Properties.Settings.Default.Save();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("QQ " + ex);
                    }

                    OnPropertyChanged(nameof(TimeoutScreenshot));
                }
            }
        }

        private BitmapSource _imageStream;
        public BitmapSource ImageStream
        { 
            get { return _imageStream; }
            set
            {
                if (value != null && value != _imageStream)
                {
                    _imageStream = value;
                    OnPropertyChanged(nameof(ImageStream));
                }
            }
        }

        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        #endregion Properties

        #region Commands

        public ICommand ReconnectVideoCommand => new RelayCommand(() => 
        {
            if (videoStreamCancellationToken != null)
            {
                videoStreamCancellationToken.Cancel();

                Thread.Sleep(100);
                Task.Run(() => StartVideoAsync());
            }
        });

        public ICommand CreateOutputVideoCommand => new RelayCommand(() =>
        {
            IsLoading = true;
            Thread thread = new Thread(CreateOutputVideo)
            {
                IsBackground = true
            };
            thread.Start();
        });

        public ICommand StartCommand => new RelayCommand(() =>
        {
            IsStarting = true;
        });

        public ICommand StopCommand => new RelayCommand(() =>
        {
            IsStarting = false;
        });

        public ICommand ClearFilesCommand => new RelayCommand(ClearFiles);

        #endregion Commands

        #region Constructor 

        private CancellationTokenSource videoStreamCancellationToken = null;

        public MainViewModel()
        {
            Task.Run(() => StartVideoAsync());
        }

        #endregion Constructor 

        #region Methods

        DateTime lastScreenShot = DateTime.Now;
        async Task StartVideoAsync()
        {
            using (var cts = new CancellationTokenSource())
            {
                videoStreamCancellationToken = cts;

                try
                {
                    await SimpleMJPEGDecoder.StartAsync(
                        image =>
                        {
                            Executers.ExecuteInUiThreadSync(() =>
                            {
                                try
                                {
                                    ImageStream = image;

                                    if (IsStarting)
                                    {
                                        TimeSpan duration = DateTime.Now.Subtract(lastScreenShot);

                                        if (duration.TotalSeconds > Properties.Settings.Default.TimeoutScreenshot)
                                        {
                                            string filename = DateTime.Now.Ticks.ToString() + ".png";

                                            string destinationPath = Path.Combine(AppFolders.Images, filename);

                                            if (!Directory.Exists(AppFolders.Images))
                                            {
                                                Directory.CreateDirectory(AppFolders.Images);
                                            }

                                            using (var fileStream = new FileStream(destinationPath, FileMode.Create))
                                            {
                                                BitmapEncoder encoder = new PngBitmapEncoder();
                                                encoder.Frames.Add(BitmapFrame.Create(image));
                                                encoder.Save(fileStream);
                                            }

                                            lastScreenShot = DateTime.Now;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex);
                                }
                            });
                        },
                        Properties.Settings.Default.StreamUrl,
                        null,
                        null,
                        cts.Token,
                        1024);
                }
                catch (OperationCanceledException ex)
                {
                    Debug.WriteLine("QQ OperationCanceledException " + ex);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("QQ Exception " + ex);
                }
            }
        }

        private void CreateOutputVideo()
        {
            if (!Directory.Exists(AppFolders.Images))
            {
                MessageBox.Show("Images folder not found!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;

                string extension = VideoCodecString == "VP9" ? ".webm" : ".avi";

                using (var writer = new VideoFileWriter())
                {
                    VideoCodec videoCodec = VideoCodecString == "VP9" ? VideoCodec.VP9 : VideoCodec.Raw;

                    writer.Open(Path.Combine(AppFolders.Exe, "output" + extension), 640, 480, 25, videoCodec);

                    foreach (string file in Directory.EnumerateFiles(AppFolders.Images, "*.png"))
                    {
                        using (var img = (Bitmap)Image.FromFile(file))
                        {
                            writer.WriteVideoFrame(img);
                        }
                    }
                    writer.Close();
                }

                _ = Process.Start("explorer.exe", $"/select,\"{Path.Combine(AppFolders.Exe, "output" + extension)}\"");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ClearFiles()
        {
            var Result = MessageBox.Show(
                "Do you want to delete all saved image and video files?", "Do you want to delete?", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (Result == MessageBoxResult.Yes)
            {
                if (Directory.Exists(AppFolders.Images))
                {
                    Directory.Delete(AppFolders.Images, true);
                }

                string fileWebm = Path.Combine(AppFolders.Exe, "output.webm");
                string fileAvi = Path.Combine(AppFolders.Exe, "output.avi");

                if (File.Exists(fileWebm)) File.Delete(fileWebm);
                if (File.Exists(fileAvi)) File.Delete(fileAvi);
            }
        }

        #endregion Methods
    }
}
