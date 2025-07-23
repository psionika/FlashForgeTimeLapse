using Accord.Video.FFMPEG;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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

        #endregion Properties

        #region Commands

        public ICommand CreateOutputVideoCommand => new RelayCommand(CreateOutputVideo);

        public ICommand StartCommand => new RelayCommand(() =>
        {
            IsStarting = true;
        });

        public ICommand StopCommand => new RelayCommand(() =>
        {
            IsStarting = false;
        });

        #endregion Commands

        #region Constructor 

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

                                            string destFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                                            string filename = DateTime.Now.Ticks.ToString() + ".png";

                                            string destinationPath = Path.Combine(destFolder, "Images", filename);

                                            if (!Directory.Exists(Path.Combine(destFolder, "Images")))
                                            {
                                                Directory.CreateDirectory(Path.Combine(destFolder, "Images"));
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
                    Debug.WriteLine("QQ " + ex);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("QQ " + ex);
                }
            }
        }

        private void CreateOutputVideo()
        {
            string destFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string imagesFolder = Path.Combine(destFolder, "Images");

            if (!Directory.Exists(imagesFolder))
            {
                MessageBox.Show("Images folder not found!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var writer = new VideoFileWriter())
            {
                writer.Open(Path.Combine(destFolder, "output.webm"), 640, 480, 25, VideoCodec.VP9);

                foreach (string file in Directory.EnumerateFiles(imagesFolder, "*.png"))
                {
                    using (var img = (Bitmap)Image.FromFile(file))
                    {
                        writer.WriteVideoFrame(img);
                    }
                }
                writer.Close();
            }

            _ = Process.Start("explorer.exe", $"/select,\"{Path.Combine(destFolder, "output.webm")}\"");
        }

        #endregion Methods
    }
}
