﻿using Discord;
using Discord.Audio;
using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace NadekoBot.Classes.Music {

    public enum MusicType {
        Radio,
        Normal,
        Local
    }

    public enum StreamState {
        Resolving,
        Queued,
        Buffering, //not using it atm
        Playing,
        Completed
    }

    public class MusicPlayer {
        public static int MaximumPlaylistSize => 50;

        private IAudioClient _client { get; set; }

        private List<Song> _playlist = new List<Song>();
        public IReadOnlyCollection<Song> Playlist => _playlist;
        private readonly object playlistLock = new object();

        public Song CurrentSong { get; set; } = default(Song);
        private CancellationTokenSource SongCancelSource { get; set; }
        private CancellationToken cancelToken { get; set; }

        public bool Paused { get; set; }

        public float Volume { get; private set; }

        public Action<Song> OnCompleted = delegate { };
        public Action<Song> OnStarted = delegate { };

        public Channel PlaybackVoiceChannel { get; private set; }

        public MusicPlayer(Channel startingVoiceChannel, float? defaultVolume) {
            if (startingVoiceChannel == null)
                throw new ArgumentNullException(nameof(startingVoiceChannel));
            if (startingVoiceChannel.Type != ChannelType.Voice)
                throw new ArgumentException("Channel must be of type voice");
            Volume = defaultVolume ?? 1.0f;

            PlaybackVoiceChannel = startingVoiceChannel;
            SongCancelSource = new CancellationTokenSource();
            cancelToken = SongCancelSource.Token;

            Task.Run(async () => {
                while (true) {
                    try {
                        _client = await PlaybackVoiceChannel.JoinAudio();
                    }
                    catch {
                        await Task.Delay(1000);
                        continue;
                    }
                    CurrentSong = GetNextSong();
                    if (CurrentSong != null) {
                        try {
                            OnStarted(CurrentSong);
                            await CurrentSong.Play(_client, cancelToken);
                        }
                        catch (OperationCanceledException) {
                            Console.WriteLine("Song canceled");
                        }
                        catch (Exception ex) {
                            Console.WriteLine($"Exception in PlaySong: {ex}");
                        }
                        try {
                            OnCompleted(CurrentSong);
                        }
                        catch { }
                        SongCancelSource = new CancellationTokenSource();
                        cancelToken = SongCancelSource.Token;
                    }
                    await Task.Delay(1000);
                }
            });
        }

        public void Next() {
            lock (playlistLock) {
                if (!SongCancelSource.IsCancellationRequested) {
                    Paused = false;
                    SongCancelSource.Cancel();
                }
            }
        }

        public void Stop() {
            lock (playlistLock) {
                _playlist.Clear();
                try {
                    if (!SongCancelSource.IsCancellationRequested)
                        SongCancelSource.Cancel();
                }
                catch {
                    Console.WriteLine("STOP");
                }
            }
        }

        public void TogglePause() => Paused = !Paused;

        public void Shuffle() {
            lock (playlistLock) {
                _playlist.Shuffle();
            }
        }

        public int SetVolume(int volume) {
            if (volume < 0)
                volume = 0;
            if (volume > 150)
                volume = 150;

            Volume = volume / 100.0f;
            return volume;
        }

        private Song GetNextSong() {
            lock (playlistLock) {
                if (_playlist.Count == 0)
                    return null;
                var toReturn = _playlist[0];
                _playlist.RemoveAt(0);
                return toReturn;
            }
        }

        public void AddSong(Song s) {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            lock (playlistLock) {
                _playlist.Add(s);
            }
        }

        public void RemoveSong(Song s) {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            lock (playlistLock) {
                _playlist.Remove(s);
            }
        }

        public void RemoveSongAt(int index) {
            lock (playlistLock) {
                if (index < 0 || index >= _playlist.Count)
                    throw new ArgumentException("Invalid index");
                _playlist.RemoveAt(index);
            }
        }

        internal Task MoveToVoiceChannel(Channel voiceChannel) {
            if (_client?.State != ConnectionState.Connected)
                throw new InvalidOperationException("Can't move while bot is not connected to voice channel.");
            PlaybackVoiceChannel = voiceChannel;
            return PlaybackVoiceChannel.JoinAudio();
        }

        internal void ClearQueue() {
            lock (playlistLock) {
                _playlist.Clear();
            }
        }
    }
}
