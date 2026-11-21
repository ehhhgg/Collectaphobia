using UnityEngine;

public class LeverSound : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip leverClip;
    [SerializeField] private int clicksPerSound = 4;

    private int clickCount;

    public void RegisterClick()
    {
        clickCount++;

        if (clickCount >= clicksPerSound)
        {
            clickCount = 0;
            audioSource.PlayOneShot(leverClip);
        }
    }
}