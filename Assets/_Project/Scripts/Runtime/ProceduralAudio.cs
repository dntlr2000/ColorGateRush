using UnityEngine;

namespace ColorGateRush
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class ProceduralAudio : MonoBehaviour
    {
        private AudioSource _source;
        private const int SampleRate = 44100;

        // Caches the audio source used for procedural one-shot playback.
        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
            }

            _source.playOnAwake = false;
        }

        // Plays a short rising collect tone based on the active combo.
        public void PlayCollect(int combo)
        {
            float frequency = 520f + Mathf.Clamp(combo, 0, 10) * 28f;
            PlayTone(frequency, 0.065f, 0.35f);
        }

        // Plays the gate color-change tone.
        public void PlayGate()
        {
            PlayTone(740f, 0.11f, 0.45f);
        }

        // Plays a low buzz for wrong-color shards and obstacle hits.
        public void PlayWrong()
        {
            PlayBuzz(145f, 0.18f, 0.45f);
        }

        // Plays a short procedural arpeggio for the finish event.
        public void PlayFinish()
        {
            PlayTone(880f, 0.08f, 0.35f);
            Invoke(nameof(PlayFinishSecondTone), 0.08f);
            Invoke(nameof(PlayFinishThirdTone), 0.16f);
        }

        // Plays the second note in the finish arpeggio.
        private void PlayFinishSecondTone()
        {
            PlayTone(1108f, 0.08f, 0.34f);
        }

        // Plays the final note in the finish arpeggio.
        private void PlayFinishThirdTone()
        {
            PlayTone(1320f, 0.13f, 0.36f);
        }

        // Synthesizes and plays a decaying sine tone.
        private void PlayTone(float frequency, float duration, float volume)
        {
            if (_source == null)
            {
                return;
            }

            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float normalized = i / (float)sampleCount;
                float envelope = 1f - normalized;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create("tone_" + frequency.ToString("0"), sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            _source.PlayOneShot(clip);
        }

        // Synthesizes and plays a deterministic noisy buzz without imported audio assets.
        private void PlayBuzz(float frequency, float duration, float volume)
        {
            if (_source == null)
            {
                return;
            }

            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float normalized = i / (float)sampleCount;
                float envelope = 1f - normalized;
                float tone = Mathf.Sin(2f * Mathf.PI * frequency * t);
                float noise = Mathf.Repeat(Mathf.Sin(i * 12.9898f) * 43758.5453f, 1f) * 2f - 1f;
                samples[i] = (tone * 0.6f + noise * 0.4f) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create("buzz_" + frequency.ToString("0"), sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            _source.PlayOneShot(clip);
        }
    }
}
