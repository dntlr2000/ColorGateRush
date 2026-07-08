using System.Collections.Generic;
using UnityEngine;

namespace ColorGateRush
{
    public enum MusicType
    {
        Menu,
        Gameplay,
        Completed,
        Failed
    }

    [RequireComponent(typeof(AudioSource))]
    public sealed class ProceduralAudio : MonoBehaviour
    {
        private const int SampleRate = 44100;
        private const string MenuMusicResourcePath = "ColorGateRush/Audio/ColorgateRush_Menu";
        private const string GameplayMusicResourcePath = "ColorGateRush/Audio/ColorgateRush_Ingame";
        private AudioSource _sfxSource;
        private AudioSource _musicSource;
        private MusicType _currentMusicType = MusicType.Menu;
        private int _currentMusicTier = -1;
        private bool _musicDucked;
        private AudioClip _menuMusicClip;
        private AudioClip _importedMenuMusicClip;
        private AudioClip _importedGameplayMusicClip;
        private bool _importedMenuMusicLoadAttempted;
        private bool _importedGameplayMusicLoadAttempted;
        private readonly AudioClip[] _gameplayMusicClips = new AudioClip[4];
        private readonly Dictionary<string, AudioClip> _sfxClipCache = new Dictionary<string, AudioClip>();

        // Caches separate audio sources so music loops never stack with one-shot SFX.
        private void Awake()
        {
            _sfxSource = GetComponent<AudioSource>();
            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
            }

            _sfxSource.playOnAwake = false;
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f;
            _sfxSource.spatialBlend = 0f;
            RefreshSettings();
        }

        // Stops active BGM if the systems object is disabled.
        private void OnDisable()
        {
            StopMusic();
        }

        // Starts a loop or short sting for the requested game state without stacking music.
        public void PlayMusic(MusicType type, int stageIndex = 1)
        {
            _currentMusicType = type;
            _currentMusicTier = GetMusicTier(stageIndex);
            if (type == MusicType.Completed)
            {
                StopMusic();
                PlayCompletedSting();
                return;
            }

            if (type == MusicType.Failed)
            {
                StopMusic();
                PlayFailedSting();
                return;
            }

            if (_musicSource == null || !GameSettings.MusicEnabled)
            {
                StopMusic();
                return;
            }

            AudioClip clip = type == MusicType.Menu
                ? GetMenuMusicClip()
                : GetGameplayMusicClip(_currentMusicTier);
            if (_musicSource.clip == clip && _musicSource.isPlaying)
            {
                ApplyMusicVolume();
                return;
            }

            _musicSource.clip = clip;
            _musicSource.loop = true;
            ApplyMusicVolume();
            _musicSource.Play();
        }

        // Stops the active music loop without affecting pending one-shot SFX.
        public void StopMusic()
        {
            if (_musicSource == null)
            {
                return;
            }

            _musicSource.Stop();
            _musicSource.clip = null;
        }

        // Dims or restores looped music while the game is paused.
        public void SetMusicDucked(bool ducked)
        {
            _musicDucked = ducked;
            ApplyMusicVolume();
        }

        // Applies current settings to audio sources and stops music when disabled.
        public void RefreshSettings()
        {
            if (_sfxSource != null)
            {
                _sfxSource.volume = 1f;
            }

            if (_musicSource != null)
            {
                ApplyMusicVolume();
                if (!GameSettings.MusicEnabled)
                {
                    StopMusic();
                }
            }
        }

        // Plays a short rising collect tone based on the active combo.
        public void PlayCollect(int combo)
        {
            if (!GameSettings.SfxEnabled)
            {
                return;
            }

            float frequency = 520f + Mathf.Clamp(combo, 0, 10) * 28f;
            PlayTone(frequency, 0.065f, 0.32f);
        }

        // Plays the gate color-change tone.
        public void PlayGate()
        {
            if (!GameSettings.SfxEnabled)
            {
                return;
            }

            PlayTone(740f, 0.07f, 0.34f);
            PlayTone(1110f, 0.12f, 0.26f);
        }

        // Plays a low buzz for wrong-color shards and obstacle hits.
        public void PlayWrong()
        {
            if (!GameSettings.SfxEnabled)
            {
                return;
            }

            PlayBuzz(145f, 0.18f, 0.45f);
        }

        // Plays a short procedural arpeggio for the finish event.
        public void PlayFinish()
        {
            if (!GameSettings.SfxEnabled)
            {
                return;
            }

            PlayTone(880f, 0.08f, 0.35f);
            Invoke(nameof(PlayFinishSecondTone), 0.08f);
            Invoke(nameof(PlayFinishThirdTone), 0.16f);
        }

        // Plays the second note in the finish arpeggio.
        private void PlayFinishSecondTone()
        {
            if (!GameSettings.SfxEnabled)
            {
                return;
            }

            PlayTone(1108f, 0.08f, 0.34f);
        }

        // Plays the final note in the finish arpeggio.
        private void PlayFinishThirdTone()
        {
            if (!GameSettings.SfxEnabled)
            {
                return;
            }

            PlayTone(1320f, 0.13f, 0.36f);
        }

        // Plays a short clear sting used on result screens.
        private void PlayCompletedSting()
        {
            if (!GameSettings.SfxEnabled)
            {
                return;
            }

            PlayTone(740f, 0.08f, 0.26f);
            PlayTone(932f, 0.11f, 0.26f);
            PlayTone(1244f, 0.18f, 0.24f);
        }

        // Plays a short failure sting without triggering state changes.
        private void PlayFailedSting()
        {
            if (!GameSettings.SfxEnabled)
            {
                return;
            }

            PlayBuzz(116f, 0.20f, 0.34f);
            PlayTone(174f, 0.16f, 0.18f);
        }

        // Synthesizes and plays a decaying sine tone.
        private void PlayTone(float frequency, float duration, float volume)
        {
            if (_sfxSource == null || !GameSettings.SfxEnabled)
            {
                return;
            }

            AudioClip clip = GetOrCreateToneClip(frequency, duration, volume);
            _sfxSource.PlayOneShot(clip, GameSettings.SfxVolume);
        }

        // Synthesizes and plays a deterministic noisy buzz without imported audio assets.
        private void PlayBuzz(float frequency, float duration, float volume)
        {
            if (_sfxSource == null || !GameSettings.SfxEnabled)
            {
                return;
            }

            AudioClip clip = GetOrCreateBuzzClip(frequency, duration, volume);
            _sfxSource.PlayOneShot(clip, GameSettings.SfxVolume);
        }

        // Reuses generated tone clips so repeated collect/gate sounds do not allocate new clips every time.
        private AudioClip GetOrCreateToneClip(float frequency, float duration, float volume)
        {
            string key = BuildSfxCacheKey("tone", frequency, duration, volume);
            if (_sfxClipCache.TryGetValue(key, out AudioClip cached))
            {
                return cached;
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

            AudioClip clip = AudioClip.Create(key, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            _sfxClipCache[key] = clip;
            return clip;
        }

        // Reuses generated buzz clips for wrong-color and hit feedback.
        private AudioClip GetOrCreateBuzzClip(float frequency, float duration, float volume)
        {
            string key = BuildSfxCacheKey("buzz", frequency, duration, volume);
            if (_sfxClipCache.TryGetValue(key, out AudioClip cached))
            {
                return cached;
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

            AudioClip clip = AudioClip.Create(key, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            _sfxClipCache[key] = clip;
            return clip;
        }

        // Builds a culture-independent cache key for generated SFX clips.
        private static string BuildSfxCacheKey(string prefix, float frequency, float duration, float volume)
        {
            return prefix
                + "_f" + Mathf.RoundToInt(frequency * 10f)
                + "_d" + Mathf.RoundToInt(duration * 1000f)
                + "_v" + Mathf.RoundToInt(volume * 1000f);
        }

        // Returns a cached gentle menu loop.
        private AudioClip GetMenuMusicClip()
        {
            AudioClip importedClip = GetImportedMusicClip(MenuMusicResourcePath, ref _importedMenuMusicClip, ref _importedMenuMusicLoadAttempted);
            if (importedClip != null)
            {
                return importedClip;
            }

            if (_menuMusicClip == null)
            {
                _menuMusicClip = BuildMusicLoop("cgr_menu_loop", 88f, 220f, true);
            }

            return _menuMusicClip;
        }

        // Returns a cached gameplay loop for the requested stage tier.
        private AudioClip GetGameplayMusicClip(int tier)
        {
            AudioClip importedClip = GetImportedMusicClip(GameplayMusicResourcePath, ref _importedGameplayMusicClip, ref _importedGameplayMusicLoadAttempted);
            if (importedClip != null)
            {
                return importedClip;
            }

            int index = Mathf.Clamp(tier - 1, 0, _gameplayMusicClips.Length - 1);
            if (_gameplayMusicClips[index] == null)
            {
                float tempo = 102f + index * 6f;
                float root = 246.94f * Mathf.Pow(2f, index / 12f);
                _gameplayMusicClips[index] = BuildMusicLoop("cgr_gameplay_loop_t" + tier, tempo, root, false);
            }

            return _gameplayMusicClips[index];
        }

        // Loads a user-provided BGM AudioClip from Resources once, falling back to procedural loops if missing.
        private static AudioClip GetImportedMusicClip(string resourcePath, ref AudioClip cachedClip, ref bool loadAttempted)
        {
            if (!loadAttempted)
            {
                cachedClip = Resources.Load<AudioClip>(resourcePath);
                loadAttempted = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (cachedClip == null)
                {
                    Debug.LogWarning("Imported BGM clip missing at Resources/" + resourcePath + ". Falling back to procedural BGM.");
                }
#endif
            }

            return cachedClip;
        }

        // Synthesizes a short seamless-ish loop from simple sine-wave bass, pad, and pluck layers.
        private static AudioClip BuildMusicLoop(string name, float tempo, float rootFrequency, bool menu)
        {
            const float duration = 4f;
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[sampleCount];
            float beatDuration = 60f / tempo;
            int[] chordSteps = menu
                ? new[] { 0, 7, 5, 7 }
                : new[] { 0, 3, 7, 10 };

            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)SampleRate;
                int chord = Mathf.FloorToInt(time / beatDuration) % chordSteps.Length;
                float root = rootFrequency * Mathf.Pow(2f, chordSteps[chord] / 12f);
                float fifth = root * 1.5f;
                float octave = root * 2f;
                float pluckEnvelope = Mathf.Exp(-Mathf.Repeat(time, beatDuration) * (menu ? 5.5f : 7.5f));
                float bass = Mathf.Sin(2f * Mathf.PI * root * 0.5f * time) * 0.18f;
                float pad = (Mathf.Sin(2f * Mathf.PI * root * time) + Mathf.Sin(2f * Mathf.PI * fifth * time)) * 0.070f;
                float pluck = Mathf.Sin(2f * Mathf.PI * octave * time) * pluckEnvelope * (menu ? 0.075f : 0.105f);
                float edgeFade = Mathf.Min(1f, Mathf.Min(time / 0.08f, (duration - time) / 0.08f));
                samples[i] = (bass + pad + pluck) * edgeFade;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        // Applies music volume and pause ducking to the loop source.
        private void ApplyMusicVolume()
        {
            if (_musicSource == null)
            {
                return;
            }

            float duck = _musicDucked ? 0.38f : 1f;
            _musicSource.volume = GameSettings.MusicVolume * duck * 0.52f;
        }

        // Returns the campaign music tier used for subtle gameplay loop variation.
        private static int GetMusicTier(int stageIndex)
        {
            if (stageIndex <= 3)
            {
                return 1;
            }

            if (stageIndex <= 10)
            {
                return 2;
            }

            if (stageIndex <= 20)
            {
                return 3;
            }

            return 4;
        }
    }
}
