using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScreen : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";

    void Awake()
    {
        var play = transform.Find("Play");
        if (play == null)
            return;

        var button = play.GetComponent<Button>();
        if (button == null || button.onClick.GetPersistentEventCount() > 0)
            return;

        button.onClick.AddListener(ContinueToClicker);
    }

    // Play button loads SampleScene.
    public void ContinueToClicker()
    {
        if (string.IsNullOrEmpty(gameSceneName))
            gameSceneName = "SampleScene";

        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogError($"TitleScreen could not open '{gameSceneName}'. Add it to the build.", this);
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    public void PlayGame()
    {
        ContinueToClicker();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}