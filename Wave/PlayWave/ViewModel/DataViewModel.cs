using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualBasic;
using NAudio.Mixer;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PlayWave.Model;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace PlayWave.ViewModel
{
    public enum NumberChannelOptions
    {
        None = 0,
        One,
        Two
    }

    public class DataViewModel : ObservableObject
    {
        #region Private fields

        private ICommand? _openCommand;

        private ICommand? _generateCommand;

        private ICommand? _playCommand;
        
        private ICommand? _pauseCommand;
        
        private ICommand? _stopCommand;

        private WaveStream reader;

        const double SliderMax = 10.0;

        #endregion Private fields

        #region Private properties

        private Data Data { get; set; }

        public IWavePlayer WavePlayer { get; set; }

        private double sliderPosition;
        
        private double sliderGainPosition = -1.0;

        private bool chooseOneChannel = true;

        private string sliderGainPositionStr = "";

        private string choosedFreqValue = string.Format("{0}", 44100);

        private string statusBarStr = "";

        #endregion Private properties

        #region Private methods

        private void Open()
        {
            // Configure open file dialog box
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "Document"; // Default file name
            dialog.DefaultExt = ".wav"; // Default file extension
            dialog.Filter = "Audio (.wav)|*.wav|All files (*.*)|*.*"; // Filter files by extension

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                WaveFilePath = dialog.FileName;

                OnPropertyChanged(nameof(WaveFilePath));

                using (NAudio.Wave.WaveFileReader wave = new NAudio.Wave.WaveFileReader(Properties.Settings.Default.LastWaveFile))
                {
                    var format = wave.WaveFormat;

                    if (format.Channels == 1)
                    {
                        IsOneChannel = true;
                    }
                    else if (format.Channels == 2)
                    {
                        IsTwoChannel = true;
                    }
                    else
                    {
                        StatusBarText = string.Format("ERROR! Количество каналов {0} не поддерживается!", format.Channels);
                    }
                }
            }
        }

        private void Generate()
        {
            if (reader != null)
            {
                reader.Close();
            }

            float amplitude = (float)Properties.Settings.Default.SliderGainPosition;

            int channelCount = Properties.Settings.Default.ChooseOneChannel ? 1 : 2;

            int sampleFreq = Convert.ToInt32(Properties.Settings.Default.ChoosedSampleFreq);

            // A440 - гудок
            float frequency_A440 = 440;
            WaveFormat outFormat = new WaveFormat(sampleFreq, 8, channelCount);

            string fileName = "test02_A440_8_Bits_Mono.wav";

            int durationSeconds = 5;

            SignalGenerator sg = new SignalGenerator(44100, channelCount)
            {
                Gain = amplitude,
                Frequency = frequency_A440,
                Type = SignalGeneratorType.Sin
            };

            ISampleProvider sp = sg.Take(TimeSpan.FromSeconds(durationSeconds)).ToMono();

            using (var writer = new WaveFileWriter(fileName, outFormat))
            {
                float[] buffer = new float[1000];

                while (true)
                {
                    int countSample = sp.Read(buffer, 0, buffer.Length);
                    if (countSample == 0)
                    {
                        // end of source provider
                        break;
                    }

                    byte[] byteToWrite = new byte[countSample];

                    for (int p = 0; p < countSample; p += 1)
                    {
                        byteToWrite[p] = (byte)((buffer[p] /2 + 0.5 ) * (byte.MaxValue));
                    }

                    // Write will throw exception if WAV file becomes too large
                    writer.Write(byteToWrite, 0, byteToWrite.Length);
                }
            }

            StatusBarText = "Генерация завершена";
        }

        private string GetHex(string fullWavePath)
        {
            string hexData = "";

            using (WaveFileReader reader = new WaveFileReader(fullWavePath))
            {
                if(reader.WaveFormat.BitsPerSample == 8)
                {

                }
                else
                {
                    if(reader.WaveFormat.BitsPerSample == 16)
                    {

                    }
                }

            }

            using (FileStream fs = new FileStream(fullWavePath, FileMode.Open))
            {
                int hexIn;

                long len = fs.Length;
                long readBytes = 0;

                // Header size
                long readBytesMax = 44;

                for (int i = 0; readBytes < readBytesMax; i++)
                {
                    hexIn = fs.ReadByte();
                    if(hexIn == -1)
                    {
                        break;
                    }

                    readBytes += 1;

                    hexData += string.Format("{0:X2} ", hexIn);
                    if(readBytes % 8 == 0)
                    {
                        hexData += "\n";
                    }
                }
            }

            return hexData;
        }

        private void CreatePlayer()
        {
            WavePlayer = new WaveOutEvent();
            WavePlayer.PlaybackStopped += WavePlayerOnPlaybackStopped;

            OnPropertyChanged("wavePlayer");
        }

        private void WavePlayerOnPlaybackStopped(object sender, StoppedEventArgs stoppedEventArgs)
        {
            if (reader != null)
            {
                SliderPosition = 0;
            }

            if (stoppedEventArgs.Exception != null)
            {

            }

            OnPropertyChanged("IsPlaying");
            OnPropertyChanged("IsStopped");
        }

        #endregion Private methods

        #region Command

        public ICommand? OpenCommand
        {
            get
            {
                return _openCommand;
            }
        }

        public ICommand? GenerateCommand
        {
            get
            {
                return _generateCommand;
            }
        }

        public ICommand? PlayCommand
        {
            get
            {
                return _playCommand;
            }
        }

        public ICommand? PauseCommand
        {
            get
            {
                return _pauseCommand;
            }
        }

        public ICommand? StopCommand
        {
            get
            {
                return _stopCommand;
            }
        }


        #endregion Command

        #region Public properties

        public string? WaveDate
        {
            get
            {
                return Data.WaveDate;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }

                if (Data.WaveDate == value)
                {
                    return;
                }

                Data.WaveDate = value;

                OnPropertyChanged(nameof(WaveDate));
            }
        }

        public string? WaveFilePath
        {
            get
            {
                return Data.WaveFilePath;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }

                if (Data.WaveFilePath == value)
                {
                    return;
                }

                string fullWavePath = value;

                Properties.Settings.Default.LastWaveFile = fullWavePath;
                Properties.Settings.Default.Save();

                Data.WaveFilePath = Path.GetFileName(fullWavePath);

                OnPropertyChanged(nameof(WaveFilePath));

                Data.WaveDate = GetHex(fullWavePath);

                OnPropertyChanged(nameof(WaveDate));
            }
        }

        public double SliderPosition
        {
            get => sliderPosition;
            set
            {
                if (sliderPosition != value)
                {
                    sliderPosition = value;
                    if (reader != null)
                    {
                        var pos = (long)(reader.Length * sliderPosition / SliderMax);
                        reader.Position = pos; // media foundation will worry about block align for us
                    }
                    
                    OnPropertyChanged("SliderPosition");
                }
            }
        }

        public bool IsPlaying => WavePlayer != null && (WavePlayer.PlaybackState == PlaybackState.Playing || WavePlayer.PlaybackState == PlaybackState.Paused);

        public bool IsPaused => WavePlayer != null && (WavePlayer.PlaybackState != PlaybackState.Paused && WavePlayer.PlaybackState != PlaybackState.Stopped);

        public bool IsStopped => WavePlayer == null || (WavePlayer.PlaybackState == PlaybackState.Stopped || WavePlayer.PlaybackState == PlaybackState.Paused);

        public string SliderGainPositionValue 
        {
            get 
            { 
                return sliderGainPositionStr; 
            }

            set
            {
                sliderGainPositionStr = value;

                OnPropertyChanged("SliderGainPositionValue");
            }
        }

        public double SliderGainPosition
        {
            get => sliderGainPosition;
            set
            {
                if (sliderGainPosition != value)
                {
                    sliderGainPosition = value;

                    Properties.Settings.Default.SliderGainPosition = sliderGainPosition;
                    Properties.Settings.Default.Save();

                    SliderGainPositionValue = string.Format("Амплитуда: {0:0.000}", value);

                    OnPropertyChanged("SliderGainPosition");
                }
            }
        }

        public bool IsOneChannel
        {
            get => chooseOneChannel;
            set
            {
                if (chooseOneChannel != value)
                {
                    chooseOneChannel = value;

                    Properties.Settings.Default.ChooseOneChannel = chooseOneChannel;
                    Properties.Settings.Default.Save();

                    OnPropertyChanged("ChannelCount");
                }
            }
        }

        public bool IsTwoChannel
        {
            get => !chooseOneChannel;
            set
            {
                if (!chooseOneChannel != value)
                {
                    chooseOneChannel = !value;

                    Properties.Settings.Default.ChooseOneChannel = chooseOneChannel;
                    Properties.Settings.Default.Save();

                    OnPropertyChanged("ChannelCount");
                }
            }
        }

        public string ChoosedSampleFreq
        {
            get => choosedFreqValue;
            set
            {
                if (choosedFreqValue != value)
                {
                    choosedFreqValue = value;

                    Properties.Settings.Default.ChoosedSampleFreq = choosedFreqValue;
                    Properties.Settings.Default.Save();

                    OnPropertyChanged("ChoosedSampleFreq");
                }
            }
        }

        public string[] SampleFreqList
        {
            get
            {
                string[] sampleFreqs =
                {
                    "8000", "11025", "22050", "44100"
                };

                return sampleFreqs;
            }
        }

        public string StatusBarText 
        {
            get { return statusBarStr; }

            set
            {
                if(statusBarStr != value)
                {
                    statusBarStr = value;

                    OnPropertyChanged("StatusBarText");
                }
            } 
        }

        #endregion Public properties

        #region Public methods

        public DataViewModel()
        {
            Data = new Data();

            WaveFilePath = Properties.Settings.Default.LastWaveFile;
            SliderGainPosition = Properties.Settings.Default.SliderGainPosition;
            IsOneChannel = Properties.Settings.Default.ChooseOneChannel;
            ChoosedSampleFreq = Properties.Settings.Default.ChoosedSampleFreq;

            _openCommand = new RelayCommand(() => Open());

            _generateCommand = new RelayCommand(() => Generate());

            _playCommand = new RelayCommand(() => Play());
            _pauseCommand = new RelayCommand(() => Pause());
            _stopCommand = new RelayCommand(() => Stop());

            StatusBarText = "Запущено";
        }

        public void Play()
        {
            string fullWavePath = Properties.Settings.Default.LastWaveFile;

            if (String.IsNullOrEmpty(fullWavePath))
            {
                return;
            }
            
            if (WavePlayer == null)
            {
                CreatePlayer();
            }
            
            if (reader != null)
            {
                reader.Dispose();
                reader = null;
            }
            
            if (reader == null)
            {
                reader = new MediaFoundationReader(fullWavePath);

                if(WavePlayer != null && WavePlayer.PlaybackState != PlaybackState.Paused)
                {
                    WavePlayer.Init(reader);
                }
            }

            WavePlayer.Play();

            OnPropertyChanged("IsPlaying");
            OnPropertyChanged("IsPaused");
            OnPropertyChanged("IsStopped");
        }

        public void Pause()
        {
            if (WavePlayer != null)
            {
                WavePlayer.Pause();

                OnPropertyChanged("IsPlaying");
                OnPropertyChanged("IsPaused");
                OnPropertyChanged("IsStopped");
            }
        }
        public void Stop()
        {
            if (WavePlayer != null)
            {
                WavePlayer.Stop();
            }

            OnPropertyChanged("IsPlaying");
            OnPropertyChanged("IsPaused");
            OnPropertyChanged("IsStopped");
        }

        public void Dispose()
        {
            WavePlayer?.Dispose();
            reader?.Dispose();
        }

        #endregion Public methods
    }
}
