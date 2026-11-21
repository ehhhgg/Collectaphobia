using UnityEngine;

public class PopSound : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip popClip;
    [Tooltip("Play the pop every Nth ball. Set to 1 to pop on every ball.")]
    [SerializeField] private int ballsPerSound = 2;

    private int ballCount;

    public void RegisterBall()
    {
        if (audioSource == null || popClip == null)
            return;

        ballCount++;

        if (ballCount >= ballsPerSound)
        {
            ballCount = 0;
            audioSource.PlayOneShot(popClip, GameAudio.SfxOutput);
        }
    }
}