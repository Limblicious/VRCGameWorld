using UdonSharp;
using UnityEngine;

public class AudioRouter : UdonSharpBehaviour
{
    [Tooltip("Prewired one-shot sources")]
    public AudioSource[] sources;

    public void Play(int i)
    {
        if (i < 0 || i >= sources.Length) return;
        AudioSource s = sources[i];
        if (s != null) s.Play();
    }

    public void PlayAt(int i, Vector3 pos)
    {
        if (i < 0 || i >= sources.Length) return;
        AudioSource s = sources[i];
        if (s == null) return;
        s.transform.position = pos;
        s.Play();
    }

    // TODO: Wire sources in inspector.
}