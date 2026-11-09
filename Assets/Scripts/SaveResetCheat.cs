using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class SaveResetCheat : MonoBehaviour
{
    static readonly Key[] Sequence =
    {
        Key.P, Key.H, Key.O, Key.B, Key.I, Key.A
    };

    [SerializeField] SaveSystem save;
    [SerializeField] ClickerHud hud;
    [SerializeField] float keyTimeout = 1.5f;
    [SerializeField] int touchFingers = 3;
    [SerializeField] float touchHoldSeconds = 2f;

    int _index;
    float _lastKeyTime;
    float _touchStart = -1f;
    float _cooldownUntil;

    void Awake()
    {
        if (save == null)
            save = GetComponent<SaveSystem>();
        if (hud == null)
            hud = GetComponentInChildren<ClickerHud>(true);
    }

    void Update()
    {
        PollKeyboard();
        PollTouchHold();
    }

    void PollKeyboard()
    {
        var kb = Keyboard.current;
        if (kb == null)
            return;

        if (_index > 0 && Time.unscaledTime - _lastKeyTime > keyTimeout)
            _index = 0;

        var want = Control(kb, Sequence[_index]);
        if (want != null && want.wasPressedThisFrame)
        {
            _index++;
            _lastKeyTime = Time.unscaledTime;
            if (_index >= Sequence.Length)
            {
                _index = 0;
                Wipe();
            }

            return;
        }

        for (var i = 0; i < Sequence.Length; i++)
        {
            var control = Control(kb, Sequence[i]);
            if (control != null && control.wasPressedThisFrame)
            {
                _index = 0;
                return;
            }
        }
    }

    static KeyControl Control(Keyboard kb, Key key)
    {
        var index = (int)key - 1;
        if (index < 0 || index >= kb.allKeys.Count)
            return null;
        return kb[key];
    }

    void PollTouchHold()
    {
        if (touchFingers <= 0 || touchHoldSeconds <= 0f)
            return;

        var ts = Touchscreen.current;
        if (ts == null)
            return;

        var held = 0;
        var touches = ts.touches;
        for (var i = 0; i < touches.Count; i++)
        {
            if (touches[i].press.isPressed)
                held++;
        }

        if (held < touchFingers)
        {
            _touchStart = -1f;
            return;
        }

        if (_touchStart < 0f)
            _touchStart = Time.unscaledTime;
        else if (Time.unscaledTime - _touchStart >= touchHoldSeconds)
        {
            _touchStart = -1f;
            Wipe();
        }
    }

    void Wipe()
    {
        if (Time.unscaledTime < _cooldownUntil)
            return;

        _cooldownUntil = Time.unscaledTime + 2f;
        if (save != null)
            save.ResetProgress();
        if (hud != null)
            hud.ShowToast("Save cleared", new Color(0.86f, 0.74f, 0.42f), 2.2f);
    }
}
