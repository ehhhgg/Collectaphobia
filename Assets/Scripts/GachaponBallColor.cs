using UnityEngine;

public class GachaponBallColor : MonoBehaviour
{
    [SerializeField] private Renderer ballRenderer;
    [SerializeField] private int coloredSlot = 1;       // slot number of the lambert3 material
    [SerializeField] private Material[] colorMaterials; // the 6 colored materials

    private void Awake()
    {
        Material[] mats = ballRenderer.sharedMaterials;
        mats[coloredSlot] = colorMaterials[Random.Range(0, colorMaterials.Length)];
        ballRenderer.sharedMaterials = mats;
    }
}