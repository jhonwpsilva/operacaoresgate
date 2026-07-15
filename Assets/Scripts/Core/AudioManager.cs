using System.Collections.Generic;
using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Gerencia toda a parte sonora: efeitos (SFX 2D e 3D posicional), trilhas (musica),
    /// som ambiente em loop, eco/reverb sutil e MUSICA DINAMICA (intensidade de combate
    /// ajusta volume/pitch). Clips vem de Resources/Audio ou de geracao procedural
    /// (ProceduralAudio). Singleton persistente criado pelo GameBootstrap.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioSource _music;
        private AudioSource _sfx;
        private AudioSource _ambient;
        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, AudioClip> _proc = new Dictionary<string, AudioClip>();
        private string _currentMusic = "";
        private float _musicBase = 0.55f;
        private float _intensidade;      // 0..1 intensidade de combate (musica dinamica)

        public float MusicVolume = 0.38f;   // musica mais baixa (arma nao fica abafada)
        public float SfxVolume   = 1.0f;   // efeitos mais altos (tiro audivel)

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true; _music.playOnAwake = false; _music.volume = MusicVolume;
            _music.spatialBlend = 0f;
            _musicBase = MusicVolume;

            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.loop = false; _sfx.playOnAwake = false; _sfx.spatialBlend = 0f;

            // eco/reverb sutil nos efeitos (sensacao de campo aberto)
            var reverb = gameObject.AddComponent<AudioReverbFilter>();
            reverb.reverbPreset = AudioReverbPreset.Arena;
            reverb.dryLevel = 0f;
            reverb.reverbLevel = -1200f; // bem discreto

            _ambient = gameObject.AddComponent<AudioSource>();
            _ambient.loop = true; _ambient.playOnAwake = false; _ambient.spatialBlend = 0f;
            _ambient.volume = 0.3f;

            // registra os SFX gerados por codigo (passos, recarga, capsula, etc.)
            ProceduralAudio.Registrar(this);
        }

        /// <summary>Registra um clip gerado por codigo, acessivel por nome.</summary>
        public void RegisterClip(string nome, AudioClip clip)
        {
            if (clip != null) _proc[nome] = clip;
        }

        private AudioClip Load(string name)
        {
            if (_proc.TryGetValue(name, out var pc) && pc != null) return pc;
            if (_clips.TryGetValue(name, out var c) && c != null) return c;
            var clip = Resources.Load<AudioClip>("Audio/" + name);
            if (clip != null) _clips[name] = clip;
            return clip;
        }

        /// <summary>Toca um efeito sonoro pontual (2D, nao interrompe outros).</summary>
        public void Play(string name, float volScale = 1f)
        {
            var clip = Load(name);
            if (clip == null) return;
            _sfx.PlayOneShot(clip, SfxVolume * volScale);
        }

        /// <summary>Toca um efeito no MUNDO (3D, com atenuacao por distancia).</summary>
        public void PlayAt(string name, Vector3 pos, float volScale = 1f)
        {
            var clip = Load(name);
            if (clip == null) return;
            var go = new GameObject("sfx3d");
            go.transform.position = pos;
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.volume = SfxVolume * volScale;
            src.spatialBlend = 1f;          // totalmente 3D
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 4f; src.maxDistance = 40f;
            src.Play();
            Destroy(go, clip.length + 0.1f);
        }

        /// <summary>Troca a trilha de fundo (em loop). Ignora se ja for a mesma.</summary>
        public void PlayMusic(string name)
        {
            if (_currentMusic == name && _music.isPlaying) return;
            var clip = Load(name);
            if (clip == null) return;
            _currentMusic = name;
            _music.clip = clip; _music.volume = MusicVolume; _music.pitch = 1f; _music.Play();
        }

        public void StopMusic() { _music.Stop(); _currentMusic = ""; }

        /// <summary>Som ambiente em loop (rumor de guerra, chuva).</summary>
        public void PlayAmbient(string name, float vol = 0.3f)
        {
            var clip = Load(name);
            if (clip == null) return;
            _ambient.clip = clip; _ambient.volume = vol; _ambient.Play();
        }
        public void StopAmbient() { if (_ambient != null) _ambient.Stop(); }

        /// <summary>
        /// Musica dinamica: quanto mais combate, mais alta e levemente acelerada fica a
        /// trilha, dando tensao. Chamado pela HUD com base nos inimigos ativos por perto.
        /// </summary>
        public void SetCombatIntensity(float alvo)
        {
            _intensidade = Mathf.Lerp(_intensidade, Mathf.Clamp01(alvo), Time.deltaTime * 1.5f);
            if (_music != null && _music.isPlaying)
            {
                _music.volume = Mathf.Lerp(_musicBase, Mathf.Min(1f, _musicBase + 0.25f), _intensidade);
                _music.pitch = Mathf.Lerp(1f, 1.06f, _intensidade);
            }
        }

        public void SetMusicVolume(float v)
        {
            MusicVolume = Mathf.Clamp01(v);
            _musicBase = MusicVolume;
            if (_music != null) _music.volume = MusicVolume;
        }

        public void SetSfxVolume(float v) { SfxVolume = Mathf.Clamp01(v); }
    }
}
