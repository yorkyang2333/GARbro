using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ManagedBass;
using GameRes;

namespace GARbro.Avalonia;

public partial class AudioPlayerWindow : Window
{
    private int _streamHandle;
    private DispatcherTimer _timer;

    public AudioPlayerWindow()
    {
        InitializeComponent();
        
        // Initialize BASS with default device
        if (!Bass.Init())
        {
            // If already initialized, it will return false with ErrorCode Already
            if (Bass.LastError != Errors.Already)
            {
                System.Diagnostics.Debug.WriteLine($"BASS Init Error: {Bass.LastError}");
            }
        }

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _timer.Tick += Timer_Tick;
    }

    public void LoadAudio(Entry entry, SoundInput soundInput)
    {
        FileNameText.Text = entry.Name;

        using var ms = new MemoryStream();
        AudioFormat.Wav.Write(soundInput, ms);
        var bytes = ms.ToArray();
        _streamHandle = Bass.CreateStream(bytes, 0, bytes.Length, BassFlags.Default);
        
        if (_streamHandle == 0)
        {
            System.Diagnostics.Debug.WriteLine($"BASS CreateStream Error: {Bass.LastError}");
            return;
        }

        long length = Bass.ChannelGetLength(_streamHandle);
        double duration = Bass.ChannelBytes2Seconds(_streamHandle, length);
        PlaybackProgress.Maximum = duration;

        Play();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (_streamHandle != 0)
        {
            long pos = Bass.ChannelGetPosition(_streamHandle);
            double currentSecs = Bass.ChannelBytes2Seconds(_streamHandle, pos);
            PlaybackProgress.Value = currentSecs;

            if (Bass.ChannelIsActive(_streamHandle) == PlaybackState.Stopped)
            {
                _timer.Stop();
                PlaybackProgress.Value = 0;
                Bass.ChannelSetPosition(_streamHandle, 0);
            }
        }
    }

    private void Play()
    {
        if (_streamHandle != 0)
        {
            Bass.ChannelPlay(_streamHandle);
            _timer.Start();
        }
    }

    private void PlayButton_Click(object? sender, RoutedEventArgs e)
    {
        Play();
    }

    private void PauseButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_streamHandle != 0)
        {
            Bass.ChannelPause(_streamHandle);
            _timer.Stop();
        }
    }

    private void StopButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_streamHandle != 0)
        {
            Bass.ChannelStop(_streamHandle);
            Bass.ChannelSetPosition(_streamHandle, 0);
            PlaybackProgress.Value = 0;
            _timer.Stop();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_streamHandle != 0)
        {
            Bass.StreamFree(_streamHandle);
            _streamHandle = 0;
        }
        base.OnClosed(e);
    }
}
