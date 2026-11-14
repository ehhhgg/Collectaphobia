using UnityEngine;
using UnityEngine.SceneManagement;

// Handoff the title screen owns. LoadingScreen opens the Title scene onto this object.
// Parent title UI under it. With no children, startup continues into the clicker.
// Call ContinueToClicker when the title screen should enter the game.
public class TitleScreen : MonoBehaviour
{
    public const string SceneName = "Title";

    [SerializeField] string clickerSceneName = "SampleScene";
    [SerializeField] bool continueUntilTitleContentExists = true;

    void Start()
    {
        if (!continueUntilTitleContentExists || transform.childCount > 0)
            return;

        Debug.Log(
            "TitleScreen has no content yet. Continuing to the clicker. Parent title UI under TitleScreen to stay on this scene.",
            this);
        ContinueToClicker();
    }

    public void ContinueToClicker()
    {
        if (!Application.CanStreamedLevelBeLoaded(clickerSceneName))
        {
            Debug.LogError($"TitleScreen could not open '{clickerSceneName}'. Add it to the build.", this);
            return;
        }

        SceneManager.LoadScene(clickerSceneName);
    }
}
