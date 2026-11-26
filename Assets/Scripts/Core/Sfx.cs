using UnityEngine;

// Harici ses dosyası gerektirmeyen retro ses efektleri + 7 farklı dinamik müzik parçası.
public static class Sfx
{
    public enum MusicTrack { Menu, Forest, Crystal, Lava, Castle, Boss, Victory }

    static bool muted;
    public static bool Muted
    {
        get => muted;
        set
        {
            muted = value;
            if (bgmSrc != null) bgmSrc.mute = muted;
        }
    }

    static AudioSource src, bgmSrc;
    static AudioClip jump, dash, slash, laser, enemyShoot, bossRoar, hit, enemyDie, hurt, pickup, key, chest, levelClear, gameOver;
    static AudioClip[] tracks;
    static MusicTrack? currentTrack;

    static void EnsureInit()
    {
        if (src != null) return;
        var host = Camera.main ? Camera.main.gameObject : new GameObject("SfxHost");
        if (Object.FindFirstObjectByType<AudioListener>() == null) host.AddComponent<AudioListener>();

        src = host.AddComponent<AudioSource>();
        src.spatialBlend = 0f;
        src.volume = 0.45f;

        bgmSrc = host.AddComponent<AudioSource>();
        bgmSrc.spatialBlend = 0f;
        bgmSrc.loop = true;
        bgmSrc.volume = 0.18f;
        bgmSrc.mute = muted;

        jump = MakeSweep("Jump", 0.14f, 240, 560, Wave.Square, 0.35f);
        dash = MakeSweep("Dash", 0.11f, 600, 180, Wave.Noise, 0.35f);
        slash = MakeSweep("Slash", 0.09f, 900, 220, Wave.Noise, 0.4f);
        laser = MakeSweep("Gun", 0.10f, 680, 110, Wave.Noise, 0.5f);
        enemyShoot = MakeSweep("EnemyShoot", 0.15f, 480, 180, Wave.Triangle, 0.4f);
        bossRoar = MakeBossRoar();
        hit = MakeSweep("Hit", 0.09f, 260, 90, Wave.Square, 0.45f);
        enemyDie = MakeNotes("EnemyDie", new float[] { 340, 240, 150 }, 0.07f, Wave.Square, 0.4f);
        hurt = MakeSweep("Hurt", 0.22f, 220, 75, Wave.Saw, 0.5f);
        pickup = MakeNotes("Pickup", new float[] { 587, 880 }, 0.07f, Wave.Triangle, 0.4f);
        key = MakeNotes("Key", new float[] { 523, 659, 784, 1046 }, 0.08f, Wave.Square, 0.35f);
        chest = MakeNotes("Chest", new float[] { 392, 523, 659 }, 0.08f, Wave.Triangle, 0.4f);
        levelClear = MakeNotes("LevelClear", new float[] { 523, 659, 784, 1046 }, 0.11f, Wave.Square, 0.4f);
        gameOver = MakeNotes("GameOver", new float[] { 330, 277, 220, 165 }, 0.16f, Wave.Saw, 0.4f);

        tracks = new AudioClip[7];
    }

    public static void StartMusic() => SetMusic(MusicTrack.Menu);

    public static void SetMusic(MusicTrack track)
    {
        EnsureInit();
        if (currentTrack == track && bgmSrc.isPlaying) return;
        currentTrack = track;
        int idx = (int)track;
        if (tracks[idx] == null) tracks[idx] = BuildTrack(track);
        bgmSrc.clip = tracks[idx];
        bgmSrc.volume = track == MusicTrack.Victory ? 0.24f : (track == MusicTrack.Boss ? 0.20f : 0.17f);
        bgmSrc.Play();
    }

    static void Play(AudioClip clip, float pitchJitter = 0.06f)
    {
        if (muted) return;
        EnsureInit();
        if (clip == null) return;
        src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        src.PlayOneShot(clip);
    }

    public static void PlayJump() => Play(jump);
    public static void PlayDash() => Play(dash);
    public static void PlaySlash() => Play(slash, 0.12f);
    public static void PlayLaser() => Play(laser, 0.05f);
    public static void PlayGun() => Play(laser, 0.08f);
    public static void PlayEnemyShoot() => Play(enemyShoot, 0.08f);
    public static void PlayBossRoar() => Play(bossRoar, 0.05f);
    public static void PlayHitEnemy() => Play(hit, 0.1f);
    public static void PlayEnemyDie() => Play(enemyDie, 0.05f);
    public static void PlayPlayerHurt() => Play(hurt, 0.04f);
    public static void PlayPickup() => Play(pickup, 0.05f);
    public static void PlayKey() => Play(key, 0f);
    public static void PlayChest() => Play(chest, 0.04f);
    public static void PlayLevelClear() => Play(levelClear, 0f);
    public static void PlayGameOver() => Play(gameOver, 0f);

    enum Wave { Square, Triangle, Saw, Noise, Sine }

    static AudioClip BuildTrack(MusicTrack t)
    {
        switch (t)
        {
            case MusicTrack.Menu:
                // Sıcak, macera çağrısı yapan ana menü melodisi (A minör -> C majör)
                return MakeTrack("BGM_Menu", 0.26f, Wave.Triangle, Wave.Sine,
                    new float[] {
                        440, 0, 523.25f, 493.88f, 440, 0, 392, 440,
                        523.25f, 0, 659.25f, 587.33f, 523.25f, 0, 493.88f, 0,
                        392, 0, 493.88f, 523.25f, 587.33f, 0, 523.25f, 493.88f,
                        440, 392, 329.63f, 392, 440, 0, 0, 0
                    },
                    new float[] {
                        220, 220, 0, 220, 196, 196, 0, 196,
                        261.63f, 261.63f, 0, 261.63f, 246.94f, 246.94f, 0, 246.94f,
                        196, 196, 0, 196, 174.61f, 174.61f, 0, 196,
                        220, 220, 164.81f, 196, 220, 0, 220, 0
                    });

            case MusicTrack.Crystal:
                // Kristal Mağara: yüksek, çansı, mistik melodi
                return MakeTrack("BGM_Crystal", 0.22f, Wave.Sine, Wave.Triangle,
                    new float[] {
                        659.25f, 0, 783.99f, 987.77f, 783.99f, 0, 659.25f, 0,
                        587.33f, 0, 739.99f, 880f, 739.99f, 0, 587.33f, 0,
                        523.25f, 659.25f, 783.99f, 1046.5f, 987.77f, 783.99f, 659.25f, 0,
                        587.33f, 659.25f, 739.99f, 659.25f, 493.88f, 0, 0, 0
                    },
                    new float[] {
                        164.81f, 0, 246.94f, 0, 164.81f, 0, 246.94f, 0,
                        146.83f, 0, 220f, 0, 146.83f, 0, 220f, 0,
                        130.81f, 0, 196f, 0, 130.81f, 0, 196f, 0,
                        146.83f, 0, 185f, 0, 123.47f, 0, 164.81f, 0
                    });

            case MusicTrack.Lava:
                // Lav Zindanı: sert, hızlı ve gergin
                return MakeTrack("BGM_Lava", 0.16f, Wave.Saw, Wave.Square,
                    new float[] {
                        369.99f, 0, 369.99f, 440f, 415.30f, 0, 329.63f, 0,
                        369.99f, 440f, 493.88f, 0, 440f, 415.30f, 369.99f, 0,
                        293.66f, 0, 329.63f, 369.99f, 415.30f, 0, 369.99f, 329.63f,
                        277.18f, 329.63f, 369.99f, 277.18f, 246.94f, 0, 277.18f, 0
                    },
                    new float[] {
                        92.5f, 92.5f, 110f, 92.5f, 103.8f, 103.8f, 82.4f, 92.5f,
                        92.5f, 92.5f, 123.47f, 92.5f, 110f, 103.8f, 92.5f, 0,
                        73.42f, 73.42f, 82.4f, 92.5f, 103.8f, 103.8f, 92.5f, 82.4f,
                        69.3f, 82.4f, 92.5f, 69.3f, 61.7f, 0, 69.3f, 0
                    });

            case MusicTrack.Castle:
                // Karanlık Kale: gotik, dramatik C minör
                return MakeTrack("BGM_Castle", 0.21f, Wave.Square, Wave.Saw,
                    new float[] {
                        261.63f, 311.13f, 392f, 0, 415.30f, 392f, 311.13f, 293.66f,
                        261.63f, 0, 246.94f, 261.63f, 311.13f, 0, 392f, 0,
                        523.25f, 493.88f, 415.30f, 392f, 349.23f, 311.13f, 293.66f, 0,
                        311.13f, 293.66f, 246.94f, 261.63f, 261.63f, 0, 0, 0
                    },
                    new float[] {
                        130.81f, 0, 130.81f, 155.56f, 103.83f, 0, 103.83f, 123.47f,
                        130.81f, 0, 123.47f, 130.81f, 155.56f, 0, 196f, 0,
                        130.81f, 123.47f, 103.83f, 98f, 87.31f, 103.83f, 123.47f, 0,
                        155.56f, 146.83f, 123.47f, 130.81f, 130.81f, 0, 130.81f, 0
                    });

            case MusicTrack.Boss:
                // Boss Savaşı: hızlı, heyecanlı, güçlü bas yürüyüşlü
                return MakeTrack("BGM_Boss", 0.135f, Wave.Square, Wave.Saw,
                    new float[] {
                        440, 440, 523.25f, 440, 587.33f, 0, 523.25f, 493.88f,
                        440, 440, 659.25f, 587.33f, 523.25f, 493.88f, 440, 392,
                        349.23f, 349.23f, 440, 349.23f, 523.25f, 0, 493.88f, 440,
                        392, 440, 493.88f, 523.25f, 659.25f, 0, 587.33f, 523.25f
                    },
                    new float[] {
                        110, 110, 130.81f, 110, 146.83f, 110, 130.81f, 123.47f,
                        110, 110, 164.81f, 146.83f, 130.81f, 123.47f, 110, 98,
                        87.31f, 87.31f, 110, 87.31f, 130.81f, 87.31f, 123.47f, 110,
                        98, 110, 123.47f, 130.81f, 164.81f, 146.83f, 130.81f, 123.47f
                    });

            case MusicTrack.Victory:
                // Duygusal, yavaş ve romantik kavuşma melodisi (Mirhan & Azra)
                return MakeTrack("BGM_Victory", 0.36f, Wave.Sine, Wave.Triangle,
                    new float[] {
                        349.23f, 440f, 523.25f, 0, 587.33f, 523.25f, 440f, 0,
                        392f, 440f, 466.16f, 0, 440f, 392f, 349.23f, 0,
                        523.25f, 587.33f, 698.46f, 0, 659.25f, 587.33f, 523.25f, 0,
                        440f, 523.25f, 466.16f, 440f, 392f, 0, 349.23f, 0
                    },
                    new float[] {
                        174.61f, 0, 220f, 261.63f, 146.83f, 0, 220f, 0,
                        116.54f, 0, 174.61f, 233.08f, 130.81f, 0, 196f, 0,
                        174.61f, 0, 261.63f, 349.23f, 146.83f, 0, 220f, 0,
                        116.54f, 174.61f, 130.81f, 196f, 174.61f, 0, 174.61f, 0
                    });

            default:
                // Orman Harabeleri (Klasik D minör zindan)
                return MakeTrack("BGM_Forest", 0.20f, Wave.Triangle, Wave.Square,
                    new float[] {
                        293.66f, 0, 349.23f, 329.63f, 293.66f, 0, 220.00f, 261.63f,
                        293.66f, 349.23f, 440.00f, 0, 392.00f, 349.23f, 329.63f, 0,
                        349.23f, 0, 440.00f, 392.00f, 349.23f, 0, 329.63f, 293.66f,
                        261.63f, 293.66f, 329.63f, 261.63f, 220.00f, 0, 220.00f, 0
                    },
                    new float[] {
                        146.83f, 146.83f, 0, 146.83f, 110.00f, 110.00f, 0, 130.81f,
                        146.83f, 146.83f, 0, 146.83f, 130.81f, 130.81f, 110.00f, 0,
                        116.54f, 116.54f, 0, 116.54f, 130.81f, 130.81f, 0, 130.81f,
                        110.00f, 110.00f, 130.81f, 130.81f, 110.00f, 0, 110.00f, 0
                    });
        }
    }

    static AudioClip MakeTrack(string name, float stepDur, Wave leadW, Wave bassW, float[] lead, float[] bass)
    {
        const int sr = 22050;
        int perStep = (int)(sr * stepDur);
        var data = new float[perStep * lead.Length];
        float pLead = 0f, pBass = 0f;
        for (int s = 0; s < lead.Length; s++)
        {
            float lf = lead[s], bf = bass[s];
            for (int i = 0; i < perStep; i++)
            {
                float t = (float)i / perStep;
                float env = t < 0.08f ? t / 0.08f : (1f - t * 0.60f);
                float v = 0f;
                if (lf > 0f) { pLead += lf / sr; v += Sample(leadW, pLead, null) * 0.44f * env; }
                if (bf > 0f) { pBass += bf / sr; v += Sample(bassW, pBass, null) * 0.22f * env; }
                data[s * perStep + i] = v;
            }
        }
        var c = AudioClip.Create(name, data.Length, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }

    static AudioClip MakeBossRoar()
    {
        const int sr = 22050;
        const float dur = 0.48f;
        int len = (int)(sr * dur);
        var data = new float[len];
        float phase = 0f;
        var rnd = new System.Random(777);
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / len;
            float freq = Mathf.Lerp(145f, 62f, t) + Mathf.Sin(t * 45f) * 14f;
            phase += freq / sr;
            float env = t < 0.1f ? t / 0.1f : Mathf.Pow(1f - t, 0.7f);
            float growl = Sample(Wave.Saw, phase, rnd) * 0.65f + Sample(Wave.Noise, phase, rnd) * 0.35f;
            data[i] = growl * env * 0.55f;
        }
        var c = AudioClip.Create("BossRoar", len, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }

    static AudioClip MakeSweep(string name, float dur, float f0, float f1, Wave w, float vol)
    {
        const int sr = 22050;
        int len = Mathf.Max(1, (int)(sr * dur));
        var data = new float[len];
        float phase = 0f;
        var rnd = new System.Random(name.GetHashCode());
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / len;
            float freq = Mathf.Lerp(f0, f1, t);
            phase += freq / sr;
            float env = (1f - t) * (1f - t);
            data[i] = Sample(w, phase, rnd) * env * vol;
        }
        var c = AudioClip.Create(name, len, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }

    static AudioClip MakeNotes(string name, float[] freqs, float noteDur, Wave w, float vol)
    {
        const int sr = 22050;
        int perNote = Mathf.Max(1, (int)(sr * noteDur));
        var data = new float[perNote * freqs.Length];
        var rnd = new System.Random(name.GetHashCode());
        for (int n = 0; n < freqs.Length; n++)
        {
            float phase = 0f;
            for (int i = 0; i < perNote; i++)
            {
                float t = (float)i / perNote;
                phase += freqs[n] / sr;
                float env = Mathf.Sin(t * Mathf.PI);
                data[n * perNote + i] = Sample(w, phase, rnd) * env * vol;
            }
        }
        var c = AudioClip.Create(name, data.Length, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }

    static float Sample(Wave w, float phase, System.Random rnd)
    {
        float p = phase - Mathf.Floor(phase);
        switch (w)
        {
            case Wave.Square: return p < 0.5f ? 1f : -1f;
            case Wave.Triangle: return 1f - 4f * Mathf.Abs(p - 0.5f);
            case Wave.Saw: return 2f * p - 1f;
            case Wave.Sine: return Mathf.Sin(p * Mathf.PI * 2f);
            default: return (float)(rnd.NextDouble() * 2.0 - 1.0);
        }
    }
}
