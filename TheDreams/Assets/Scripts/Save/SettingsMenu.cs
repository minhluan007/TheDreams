using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    void Awake()
    {
        RectTransform panel = (RectTransform)transform;
        float width = panel.sizeDelta.x;
        float height = panel.sizeDelta.y;
        if (width < 40f)
            width = 70f;
        if (height < 12f)
            height = 20f;

        float buttonWidth = Mathf.Floor((width - 6f) * 0.5f);
        float buttonHeight = height - 4f;
        float offset = (buttonWidth + 2f) * 0.5f;

        CreateButton("SaveButton", "Save", new Vector2(-offset, 0f), new Vector2(buttonWidth, buttonHeight), GameSave.Save);
        CreateButton("LoadButton", "Load", new Vector2(offset, 0f), new Vector2(buttonWidth, buttonHeight), () => GameSave.Load());
    }

    void CreateButton(string objectName, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        Transform existing = transform.Find(objectName);
        Button button;
        if (existing == null)
        {
            GameObject buttonObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.layer = gameObject.layer;
            buttonObject.transform.SetParent(transform, false);

            RectTransform rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.16f);
            image.raycastTarget = true;

            GameObject textObject = new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.layer = gameObject.layer;
            textObject.transform.SetParent(buttonObject.transform, false);

            RectTransform textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 6f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            if (TMP_Settings.defaultFontAsset != null)
                text.font = TMP_Settings.defaultFontAsset;

            button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.fadeDuration = 0.05f;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.75f, 0.9f, 1f, 1f);
            colors.pressedColor = new Color(0.55f, 0.75f, 0.9f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
        }
        else
        {
            button = existing.GetComponent<Button>();
            if (button == null)
                button = existing.gameObject.AddComponent<Button>();
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            onClick.Invoke();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        });
    }
}
