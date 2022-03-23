using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager am;

    public AudioClip die;
    public AudioClip eatApple;
    public AudioClip grow;
    public AudioClip shock;
    
    public AudioSource soundSource;
    public AudioSource musicSource;

    void Awake()
    {
        if (am == null)
        {
            am = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (am != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        GameManager gm = GameManager.gm;
        PlayerController.RegisterOnEat(PlayEatAppleSound);
        PlayerController.RegisterOnDie(PlayDieSound);
        PlayerController.RegisterOnGrow(PlayGrowSound);
        Fish.RegisterOnShock(PlayShockSound);
    }

    void PlayEatAppleSound(Fish _fish)
    {
        PlaySound(eatApple);
    }

    void PlayDieSound()
    {
        PlaySound(die);
    }

    void PlayGrowSound(float _float)
    {
        PlaySound(grow);
    }

    void PlayShockSound(Fish _fish)
    {
        PlaySound(shock);
    }

    void PlaySound(AudioClip _sound)
    {
        if (soundSource.enabled)
        {
            soundSource.PlayOneShot(_sound);
        }
    }
}
