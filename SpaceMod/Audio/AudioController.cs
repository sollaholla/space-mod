using System.IO;
using GTS.Utility;
using NAudio.Wave;

namespace GTS.Audio
{
    public static class AudioController
    {
        private static WaveFileReader _wave;
        private static DirectSoundOut _output;

        public static void PlayAudio(string name, float volume)
        {
            var path = GtsSettings.AudioFolder + "\\" + name + ".wav";
            if (!File.Exists(path)) return;

            _wave = new WaveFileReader(path);
            _output = new DirectSoundOut();
            var waveChannel32 = new WaveChannel32(_wave) {Volume = volume};
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