using UnityEngine;
using UnityEngine.SceneManagement;

// Música y efectos de sonido. Vive entre escenas (DontDestroyOnLoad).
// Cualquier script puede llamar: AudioManager.Play(Sfx.GarfioLanzar);
public enum Sfx
{
    GarfioLanzar, GarfioImpacto, GarfioPared, Red, Dash, Golpe,
    Muerte, Moneda, Comprar, Click, InicioRonda, Victoria, Derrota, Runa
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public class SoundEntry
    {
        public Sfx id;
        public AudioClip[] clips;            // Si pones varios, elige uno al azar
        [Range(0f, 1f)] public float volume = 1f;
    }

    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioClip menuMusic;
    [SerializeField] AudioClip battleMusic;
    [SerializeField] string menuSceneName = "MainMenu";
    [SerializeField] SoundEntry[] sounds;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        sfxSource.playOnAwake = false;

        AudioListener.volume = PlayerPrefs.GetFloat("volumen", 0.8f);
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance != this) return;
        PlayMusic(scene.name == menuSceneName ? menuMusic : battleMusic);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    public static void Play(Sfx id)
    {
        if (Instance != null) Instance.PlayInternal(id);
    }

    void PlayInternal(Sfx id)
    {
        if (sounds == null) return;
        foreach (var s in sounds)
        {
            if (s.id != id || s.clips == null || s.clips.Length == 0) continue;
            AudioClip clip = s.clips[Random.Range(0, s.clips.Length)];
            if (clip != null) sfxSource.PlayOneShot(clip, s.volume);
            return;
        }
    }

    public static void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("volumen", value);
    }
}
