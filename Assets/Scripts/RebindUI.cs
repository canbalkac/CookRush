using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class RebindUI : MonoBehaviour
{
    public InputActionReference actionRef;
    [Tooltip("Composite parça adý: up, down, left, right. Boþsa tekil binding")]
    public string bindingPartName = "";
    public TMP_Text display;

    int bindingIndex = -1;

    private void Start()
    {
        ResolveBindingIndex();
        UpdateDisplay();
    }

    private void ResolveBindingIndex()
    {
        bindingIndex = -1;
        var a = actionRef?.action; if (a == null) return;

        for (int i = 0; i < a.bindings.Count; i++)
        {
            var b = a.bindings[i];
            if (!string.IsNullOrEmpty(bindingPartName))
            {
                if (b.isPartOfComposite && b.name == bindingPartName) { bindingIndex = i; break; }
            }
            else
            {
                if (!b.isComposite && !b.isPartOfComposite) { bindingIndex = i; break; }
            }
        }
    }

    public void StartRebind()
    {
        var a = actionRef?.action; if (a == null) return;
        if (bindingIndex < 0) ResolveBindingIndex();

        a.Disable();
        if (display) display.text = "Press a key";

        a.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .WithControlsExcluding("<Mouse>")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(op =>
            {
                op.Dispose();
                a.Enable();
                SaveOverrides();
                UpdateDisplay();
            })
            .OnCancel(op =>
            {
                op.Dispose();
                a.Enable();
                UpdateDisplay();
            })
            .Start();
    }

    void SaveOverrides()
    {
        var asset = actionRef.action.actionMap.asset;
        var json = asset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("rebinds", json);
        PlayerPrefs.Save();
    }

    void UpdateDisplay()
    {
        var a = actionRef?.action; if (a == null || display == null) return;
        if (bindingIndex < 0 || bindingIndex >= a.bindings.Count) return;

        var path = a.bindings[bindingIndex].effectivePath;
        display.text = InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
    }

    public void ResetBindings()
    {
        var a = actionRef?.action; if (a == null) return;
        a.RemoveAllBindingOverrides();
        SaveOverrides();
        UpdateDisplay();
    }
}
