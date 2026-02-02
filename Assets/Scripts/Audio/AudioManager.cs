using UnityEngine;
using UnityEngine.Video;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Ambient")]
    public AudioClip ambientLoop;
    public VideoClip ambientVideo;
    public bool useVideoDirectAudio = true;
    [Range(0f, 1f)]
    public float ambientVolume = 0.5f;

    [Header("Conversation")]
    public AudioClip conversationLoop;

    [Header("Default SFX")]
    public AudioClip defaultSfx;
    public AudioClip errorSfx;
    public AudioClip confettiSfx;

    [Header("SFX")]
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    private AudioSource ambientSource;
    private AudioSource sfxSource;
    private VideoPlayer videoPlayer;
    private bool isConversationActive;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoBootstrap()
    {
        EnsureExists();
    }

    public static AudioManager EnsureExists()
    {
        if (Instance != null)
            return Instance;

        var prefab = Resources.Load<AudioManager>("Prefabs/AudioManager");
        if (prefab != null)
            return Instantiate(prefab);

        var go = new GameObject("AudioManager");
        return go.AddComponent<AudioManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSources();
        EnsureVideoPlayer();
        PlayAmbientIfReady();
    }

    public void PlayOneShot(AudioClip clip)
    {
        if (clip == null)
            return;

        EnsureSources();
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlayAmbient(AudioClip clip)
    {
        if (clip == null)
            return;

        EnsureSources();
        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();
        if (ambientSource.clip == clip && ambientSource.isPlaying)
            return;

        isConversationActive = false;
        ambientSource.clip = clip;
        ambientSource.volume = ambientVolume;
        ambientSource.loop = true;
        ambientSource.Play();
    }

    public void PlayAmbientVideo(VideoClip clip)
    {
        if (clip == null)
            return;

        EnsureSources();
        EnsureVideoPlayer();

        if (videoPlayer.clip == clip && videoPlayer.isPlaying)
            return;

        ambientSource.Stop();
        ambientSource.clip = null;
        ambientSource.volume = ambientVolume;

        isConversationActive = false;
        ConfigureVideoAudioOutput();
        videoPlayer.clip = clip;
        videoPlayer.isLooping = true;
        videoPlayer.Prepare();
    }

    public void PlayConversationLoop(AudioClip clipOverride = null)
    {
        var clip = clipOverride ?? conversationLoop;
        if (clip == null)
            return;

        EnsureSources();
        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();
        if (ambientSource.clip == clip && ambientSource.isPlaying)
        {
            isConversationActive = true;
            return;
        }

        isConversationActive = true;
        ambientSource.clip = clip;
        ambientSource.volume = ambientVolume;
        ambientSource.loop = true;
        ambientSource.Play();
    }

    public void ResumeAmbient()
    {
        if (!isConversationActive)
            return;

        isConversationActive = false;
        if (ambientLoop != null)
        {
            PlayAmbient(ambientLoop);
            return;
        }

        if (ambientVideo != null)
            PlayAmbientVideo(ambientVideo);
    }

    private void PlayAmbientIfReady()
    {
        if (ambientLoop != null)
        {
            PlayAmbient(ambientLoop);
            return;
        }

        if (ambientVideo != null)
            PlayAmbientVideo(ambientVideo);
    }

    private void EnsureSources()
    {
        if (ambientSource == null)
        {
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
            ambientSource.spatialBlend = 0f;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
        }
    }

    private void EnsureVideoPlayer()
    {
        if (ambientSource == null)
            EnsureSources();

        if (videoPlayer == null)
            videoPlayer = gameObject.GetComponent<VideoPlayer>();
        if (videoPlayer == null)
            videoPlayer = gameObject.AddComponent<VideoPlayer>();

        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = true;
        videoPlayer.controlledAudioTrackCount = 1;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.waitForFirstFrame = false;
        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.errorReceived -= OnVideoError;
        videoPlayer.errorReceived += OnVideoError;
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        if (source == null)
            return;

        source.Play();
    }

    private void ConfigureVideoAudioOutput()
    {
        if (videoPlayer == null)
            return;

        if (useVideoDirectAudio)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            videoPlayer.SetDirectAudioMute(0, false);
            videoPlayer.SetDirectAudioVolume(0, ambientVolume);
        }
        else
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, ambientSource);
        }
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning("AudioManager VideoPlayer error: " + message);
    }
}
