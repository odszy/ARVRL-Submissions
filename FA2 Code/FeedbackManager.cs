using UnityEngine;

// Aural + haptic channels from the FA1 "virtual world" design.
[RequireComponent(typeof(AudioSource))]
public class FeedbackManager : MonoBehaviour
{
    public AudioClip perfectClip, goodClip, missClip;
    AudioSource src;

    void Awake() => src = GetComponent<AudioSource>();

    public void Play(int points, bool inside)
    {
        AudioClip c = points >= 80 ? perfectClip : (inside ? goodClip : missClip);
        if (c != null) src.PlayOneShot(c);
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();   // simple haptic pulse (fixed length on most devices)
#endif
    }
}
