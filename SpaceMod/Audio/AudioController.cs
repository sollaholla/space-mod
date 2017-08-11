using System.IO;
using GTA;
using NAudio.Wave;

namespace GTS.Audio
{
    public enum AudioType
    {
        Launch01,
        Detach01
    }

    public static class AudioController
    {
        private const string Path = "./scripts/Space/Audio/";

        private static WaveFileReader _wave;
        private static DirectSoundOut _output;

        public static void PlayAudio(AudioType type, float volume)
        {
            var path = Path + type + ".wav";
            if (!File.Exists(path)) return;

            _wave = new WaveFileReader(path);
            _output = new DirectSoundOut();
            var waveChannel32 = new WaveChannel32(_wave) { Volume = volume };
            _output.Init(waveChannel32);
            _output.Play();
        }

        public static void Continue()
        {
            _output?.Play();
        }

        public static void Pause()
        {
            _output?.Pause();
        }
    }
}
