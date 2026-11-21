using UnityEngine;

public class ShimmerSound : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shimmerClip;

    public void PlayShimmer()
    {
        if (audioSource == null || shimmerClip == null)
            return;

        audioSource.PlayOneShot(shimmerClip, GameAudio.SfxOutput);
    }
}