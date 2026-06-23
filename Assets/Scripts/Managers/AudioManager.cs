using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private AudioSource musicSource;
    private AudioSource bossSource;
    private AudioSource sfxSource;

    private int      currentTheme;
    private Coroutine musicFade;

    private const float MusicVolume  = 0.18f;
    private const float FadeDuration = 0.4f;

    private void Awake()
    {
        Instance = this;

        musicSource      = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = MusicVolume;

        bossSource       = gameObject.AddComponent<AudioSource>();
        bossSource.loop  = true;
        bossSource.volume = 0f;

        sfxSource        = gameObject.AddComponent<AudioSource>();
        sfxSource.volume = 0.35f;

        GameEvents.BossVisibilityChanged += OnBossVisibility;
    }

    private void OnDestroy() => GameEvents.BossVisibilityChanged -= OnBossVisibility;

    // ── Music ─────────────────────────────────────────────────────────────────

    public void PlayMusic(int theme)
    {
        currentTheme = theme;
        musicSource.clip = CreateMusic(theme);
        musicSource.volume = MusicVolume;
        musicSource.Play();
    }

    private void OnBossVisibility(bool visible)
    {
        if (musicFade != null) StopCoroutine(musicFade);
        musicFade = StartCoroutine(visible ? FadeToBoss() : FadeToNormal());
    }

    private IEnumerator FadeToBoss()
    {
        // Fade out normal music
        float start = musicSource.volume;
        for (float t = 0f; t < FadeDuration; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(start, 0f, t / FadeDuration);
            yield return null;
        }
        musicSource.Stop();
        musicSource.volume = MusicVolume;

        // Fade in boss music
        bossSource.clip   = CreateBossMusic(currentTheme);
        bossSource.volume = 0f;
        bossSource.Play();
        for (float t = 0f; t < FadeDuration; t += Time.deltaTime)
        {
            bossSource.volume = Mathf.Lerp(0f, MusicVolume, t / FadeDuration);
            yield return null;
        }
        bossSource.volume = MusicVolume;
        musicFade = null;
    }

    private IEnumerator FadeToNormal()
    {
        // Fade out boss music
        float start = bossSource.volume;
        for (float t = 0f; t < FadeDuration; t += Time.deltaTime)
        {
            bossSource.volume = Mathf.Lerp(start, 0f, t / FadeDuration);
            yield return null;
        }
        bossSource.Stop();
        bossSource.volume = 0f;

        // Fade in normal music
        musicSource.clip   = CreateMusic(currentTheme);
        musicSource.volume = 0f;
        musicSource.Play();
        for (float t = 0f; t < FadeDuration; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, MusicVolume, t / FadeDuration);
            yield return null;
        }
        musicSource.volume = MusicVolume;
        musicFade = null;
    }

    // ── SFX ──────────────────────────────────────────────────────────────────

    public void PlayShot()      => PlayTone(760f,  0.07f, 0.18f);
    public void PlayExplosion() => PlayNoise(0.18f, 0.25f);
    public void PlayDamage()    => PlayTone(130f,  0.2f,  0.3f);
    public void PlayPowerUp()   => PlaySweep(420f, 920f,  0.25f, 0.25f);
    public void PlayBossHit()   => PlayTone(220f,  0.08f, 0.18f);
    public void PlayBossDeath() => PlayNoise(0.7f,  0.4f);

    private void PlayTone(float frequency, float duration, float volume) =>
        sfxSource.PlayOneShot(CreateTone(frequency, duration), volume);
    private void PlayNoise(float duration, float volume) =>
        sfxSource.PlayOneShot(CreateNoise(duration), volume);
    private void PlaySweep(float start, float end, float duration, float volume) =>
        sfxSource.PlayOneShot(CreateSweep(start, end, duration), volume);

    // ── Clip builders — SFX ───────────────────────────────────────────────────

    private static AudioClip CreateTone(float frequency, float duration)
    {
        int rate = 22050;
        int length = Mathf.CeilToInt(rate * duration);
        float[] samples = new float[length];
        for (int index = 0; index < length; index++)
        {
            float envelope = 1f - index / (float)length;
            samples[index] = Mathf.Sin(2f * Mathf.PI * frequency * index / rate) * envelope;
        }
        AudioClip clip = AudioClip.Create("Arcade Tone", length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateNoise(float duration)
    {
        int rate = 22050;
        int length = Mathf.CeilToInt(rate * duration);
        float[] samples = new float[length];
        for (int index = 0; index < length; index++)
        {
            float envelope = Mathf.Pow(1f - index / (float)length, 2f);
            samples[index] = Random.Range(-1f, 1f) * envelope;
        }
        AudioClip clip = AudioClip.Create("Arcade Explosion", length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateSweep(float start, float end, float duration)
    {
        int rate = 22050;
        int length = Mathf.CeilToInt(rate * duration);
        float[] samples = new float[length];
        float phase = 0f;
        for (int index = 0; index < length; index++)
        {
            float t = index / (float)length;
            phase += 2f * Mathf.PI * Mathf.Lerp(start, end, t) / rate;
            samples[index] = Mathf.Sin(phase) * (1f - t * 0.4f);
        }
        AudioClip clip = AudioClip.Create("Arcade Sweep", length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // ── Clip builders — Music ─────────────────────────────────────────────────

    // Normal level music — 160 BPM, 4 bars, kick/snare/hat + bass + arp + melody
    private static AudioClip CreateMusic(int theme)
    {
        const int   rate       = 22050;
        const float bpm        = 160f;
        const float beat       = 60f / bpm;      // 0.375 s
        const float sixteenth  = beat / 4f;      // 0.09375 s
        const int   totalBeats = 16;             // 4 bars × 4 beats
        const int   totalSteps = totalBeats * 4; // 64 sixteenth notes
        float clipLen = totalBeats * beat;       // 6.0 s
        int   length  = Mathf.CeilToInt(rate * clipLen);
        float[] data  = new float[length];

        float[] bassSeq, arpSeq, melSeq;
        switch (theme)
        {
            case 1:
                bassSeq = new float[]
                {
                     82.41f,  82.41f,  73.42f,  82.41f,
                     98.00f,  98.00f,  82.41f,  61.74f,
                     82.41f,  82.41f,  73.42f,  82.41f,
                    110.00f,  98.00f,  73.42f,  82.41f
                };
                arpSeq = new float[] { 164.81f, 196.00f, 246.94f, 329.63f };
                melSeq = new float[] { 329.63f, 246.94f, 196.00f, 246.94f,
                                       329.63f, 392.00f, 246.94f, 196.00f };
                break;
            case 2:
                bassSeq = new float[]
                {
                     73.42f,  73.42f,  98.00f,  73.42f,
                     65.41f,  65.41f,  73.42f,  55.00f,
                     87.31f,  87.31f,  98.00f,  87.31f,
                     73.42f,  65.41f,  55.00f,  73.42f
                };
                arpSeq = new float[] { 146.83f, 174.61f, 220.00f, 293.66f };
                melSeq = new float[] { 220.00f, 174.61f, 146.83f, 130.81f,
                                       220.00f, 261.63f, 293.66f, 220.00f };
                break;
            default:
                bassSeq = new float[]
                {
                    110.00f, 110.00f,  98.00f, 110.00f,
                    110.00f,  82.41f,  98.00f, 110.00f,
                    130.81f, 110.00f,  98.00f, 110.00f,
                    110.00f,  82.41f,  98.00f,  82.41f
                };
                arpSeq = new float[] { 220.00f, 261.63f, 329.63f, 220.00f };
                melSeq = new float[] { 329.63f, 261.63f, 220.00f, 261.63f,
                                       329.63f, 392.00f, 261.63f, 220.00f };
                break;
        }

        var noise = BakeNoise(length, 0xDEADBEEFu);
        float bassPhase = 0f, arpPhase = 0f, melPhase = 0f;

        for (int i = 0; i < length; i++)
        {
            float tLoop    = (i / (float)rate) % clipLen;
            float tInStep  = tLoop % sixteenth;
            float tInBeat  = tLoop % beat;
            int   stepIdx  = (int)(tLoop / sixteenth) % totalSteps;
            int   beatIdx  = stepIdx / 4;
            int   stepInBar = stepIdx % 16;

            bassPhase += bassSeq[beatIdx] / rate;
            arpPhase  += arpSeq[stepIdx % arpSeq.Length] / rate;
            melPhase  += melSeq[stepIdx / 8 % melSeq.Length] / rate;
            if (bassPhase >= 1f) bassPhase -= 1f;
            if (arpPhase  >= 1f) arpPhase  -= 1f;
            if (melPhase  >= 1f) melPhase  -= 1f;

            float bassEnv = 0.55f + 0.45f * Mathf.Exp(-tInBeat * 8f);
            float bass    = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * bassPhase));
            float arpEnv  = Mathf.Exp(-tInStep * 18f);
            float arp     = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * arpPhase));
            float melEnv  = 0.45f + 0.55f * Mathf.Exp(-tInStep * 6f);
            float mel     = 4f * Mathf.Abs(melPhase - 0.5f) - 1f;

            float kick = 0f;
            if (stepInBar == 0 || stepInBar == 8)
            {
                float kp = (80f / 40f) * (1f - Mathf.Exp(-tInStep * 40f)) + 50f * tInStep;
                kick = Mathf.Exp(-tInStep * 22f) * Mathf.Sin(2f * Mathf.PI * kp);
            }

            float snare = (stepInBar == 4 || stepInBar == 12)
                ? Mathf.Exp(-tInStep * 28f) * noise[i] : 0f;

            float hihat = (stepIdx % 2 == 0)
                ? Mathf.Exp(-tInStep * 60f) * noise[(i + 11111) % length] : 0f;

            data[i] = Mathf.Clamp(
                bass * bassEnv * 0.30f + arp * arpEnv * 0.15f +
                mel  * melEnv  * 0.11f + kick * 0.42f +
                snare * 0.22f  + hihat * 0.08f, -1f, 1f);
        }

        var clip = AudioClip.Create($"ActionTheme_{theme}", length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Boss music — 200 BPM, galloping kick, double arp, continuous hi-hat, ghost snares
    private static AudioClip CreateBossMusic(int theme)
    {
        const int   rate       = 22050;
        const float bpm        = 200f;
        const float beat       = 60f / bpm;      // 0.3 s
        const float sixteenth  = beat / 4f;      // 0.075 s
        const int   totalBeats = 16;             // 4 bars × 4 beats
        const int   totalSteps = totalBeats * 4; // 64 sixteenth notes
        float clipLen = totalBeats * beat;       // 4.8 s
        int   length  = Mathf.CeilToInt(rate * clipLen);
        float[] data  = new float[length];

        // Same harmonic roots as normal music, arp expanded to 8-note up-down sweep
        float[] bassSeq, arpSeq, arp2Seq, melSeq;
        switch (theme)
        {
            case 1: // E minor — boss
                bassSeq = new float[]
                {
                     82.41f,  82.41f,  73.42f,  82.41f,
                     98.00f,  98.00f,  82.41f,  61.74f,
                     82.41f,  82.41f,  73.42f,  82.41f,
                    110.00f,  98.00f,  73.42f,  82.41f
                };
                arpSeq  = new float[] { 164.81f, 196.00f, 246.94f, 329.63f,
                                        246.94f, 196.00f, 164.81f, 123.47f };
                arp2Seq = new float[] { 329.63f, 392.00f, 493.88f, 659.26f,
                                        493.88f, 392.00f, 329.63f, 246.94f };
                melSeq  = new float[] { 329.63f, 246.94f, 196.00f, 246.94f,
                                        329.63f, 392.00f, 246.94f, 196.00f };
                break;
            case 2: // D minor — boss
                bassSeq = new float[]
                {
                     73.42f,  73.42f,  98.00f,  73.42f,
                     65.41f,  65.41f,  73.42f,  55.00f,
                     87.31f,  87.31f,  98.00f,  87.31f,
                     73.42f,  65.41f,  55.00f,  73.42f
                };
                arpSeq  = new float[] { 146.83f, 174.61f, 220.00f, 293.66f,
                                        220.00f, 174.61f, 146.83f, 110.00f };
                arp2Seq = new float[] { 293.66f, 349.23f, 440.00f, 587.33f,
                                        440.00f, 349.23f, 293.66f, 220.00f };
                melSeq  = new float[] { 220.00f, 174.61f, 146.83f, 130.81f,
                                        220.00f, 261.63f, 293.66f, 220.00f };
                break;
            default: // A minor — boss
                bassSeq = new float[]
                {
                    110.00f, 110.00f,  98.00f, 110.00f,
                    110.00f,  82.41f,  98.00f, 110.00f,
                    130.81f, 110.00f,  98.00f, 110.00f,
                    110.00f,  82.41f,  98.00f,  82.41f
                };
                arpSeq  = new float[] { 220.00f, 261.63f, 329.63f, 392.00f,
                                        329.63f, 261.63f, 220.00f, 164.81f };
                arp2Seq = new float[] { 440.00f, 523.25f, 659.26f, 783.99f,
                                        659.26f, 523.25f, 440.00f, 329.63f };
                melSeq  = new float[] { 329.63f, 261.63f, 220.00f, 261.63f,
                                        329.63f, 392.00f, 261.63f, 220.00f };
                break;
        }

        var noise = BakeNoise(length, 0xCAFEBABEu);
        float bassPhase = 0f, arpPhase = 0f, arp2Phase = 0f, melPhase = 0f;

        for (int i = 0; i < length; i++)
        {
            float tLoop    = (i / (float)rate) % clipLen;
            float tInStep  = tLoop % sixteenth;
            float tInBeat  = tLoop % beat;
            int   stepIdx  = (int)(tLoop / sixteenth) % totalSteps;
            int   beatIdx  = stepIdx / 4;
            int   stepInBar = stepIdx % 16;

            bassPhase  += bassSeq[beatIdx] / rate;
            arpPhase   += arpSeq [stepIdx % arpSeq.Length]  / rate;
            arp2Phase  += arp2Seq[stepIdx % arp2Seq.Length] / rate;
            melPhase   += melSeq [stepIdx / 8 % melSeq.Length] / rate;
            if (bassPhase  >= 1f) bassPhase  -= 1f;
            if (arpPhase   >= 1f) arpPhase   -= 1f;
            if (arp2Phase  >= 1f) arp2Phase  -= 1f;
            if (melPhase   >= 1f) melPhase   -= 1f;

            float bassEnv = 0.55f + 0.45f * Mathf.Exp(-tInBeat * 10f);
            float bass    = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * bassPhase));
            float arpEnv  = Mathf.Exp(-tInStep * 22f);
            float arp1    = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * arpPhase));
            float arp2    = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * arp2Phase));
            float melEnv  = 0.45f + 0.55f * Mathf.Exp(-tInStep * 8f);
            float mel     = 4f * Mathf.Abs(melPhase - 0.5f) - 1f;

            // Galloping kick: 0, 3, 8, 11
            float kick = 0f;
            if (stepInBar == 0 || stepInBar == 3 || stepInBar == 8 || stepInBar == 11)
            {
                float kp = (80f / 40f) * (1f - Mathf.Exp(-tInStep * 45f)) + 50f * tInStep;
                kick = Mathf.Exp(-tInStep * 25f) * Mathf.Sin(2f * Mathf.PI * kp);
            }

            // Snare + ghost snares
            float snare = 0f;
            if (stepInBar == 4 || stepInBar == 12)
                snare = Mathf.Exp(-tInStep * 25f) * noise[i];
            else if (stepInBar == 6 || stepInBar == 14)
                snare = Mathf.Exp(-tInStep * 40f) * noise[i] * 0.35f;

            // Continuous 16th-note hi-hat (4× denser than normal)
            float hihat = Mathf.Exp(-tInStep * 65f) * noise[(i + 22222) % length];

            data[i] = Mathf.Clamp(
                bass * bassEnv * 0.28f + arp1 * arpEnv * 0.13f +
                arp2 * arpEnv  * 0.09f + mel  * melEnv * 0.10f +
                kick * 0.40f   + snare * 0.20f + hihat * 0.09f,
                -1f, 1f);
        }

        var clip = AudioClip.Create($"BossTheme_{theme}", length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Deterministic xorshift32 noise — same seed gives same result every call
    private static float[] BakeNoise(int length, uint seed)
    {
        var buf = new float[length];
        uint rng = seed;
        for (int i = 0; i < length; i++)
        {
            rng ^= rng << 13; rng ^= rng >> 17; rng ^= rng << 5;
            buf[i] = (rng / (float)uint.MaxValue) * 2f - 1f;
        }
        return buf;
    }
}
