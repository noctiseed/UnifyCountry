using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UnifyCountry.UI
{
    public sealed partial class PrototypeBattleUi
    {
        private const string MainMenuSceneName = "SCN_MainMenu";

        private void BuildExitMenuButton(Transform parent)
        {
            var button = CreateTopMenuButton(parent, "菜单");
            SetRect(button.GetComponent<RectTransform>(), new Vector2(0.91f, 0.915f), new Vector2(0.975f, 0.965f), Vector2.zero, Vector2.zero);
            button.interactable = !isResolvingTurn;
            button.onClick.AddListener(() => ShowGameMenu(parent));
        }

        private Button CreateTopMenuButton(Transform parent, string label)
        {
            var image = CreateImage(parent, label, new Color(0.96f, 0.84f, 0.58f, 0.92f));
            image.sprite = GetRoundedButtonSprite();
            image.type = Image.Type.Sliced;

            var button = image.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = new Color(0.96f, 0.84f, 0.58f, 0.92f),
                highlightedColor = new Color(1f, 0.9f, 0.66f, 1f),
                pressedColor = new Color(0.76f, 0.56f, 0.3f, 1f),
                selectedColor = new Color(0.96f, 0.84f, 0.58f, 0.92f),
                disabledColor = new Color(0.56f, 0.5f, 0.42f, 0.65f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };

            var outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.28f, 0.18f, 0.09f, 0.55f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var text = CreateText(image.transform, label, 18, TextAnchor.MiddleCenter, new Color(0.2f, 0.12f, 0.06f));
            text.fontStyle = FontStyle.Bold;
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return button;
        }

        private void ShowGameMenu(Transform parent)
        {
            CloseExistingGameMenu(parent);

            var modalRoot = new GameObject("Game Menu Modal", typeof(RectTransform));
            modalRoot.transform.SetParent(parent, false);
            var modalRect = modalRoot.GetComponent<RectTransform>();
            SetRect(modalRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var dimmer = CreateImage(modalRoot.transform, "Dim Background", new Color(0f, 0f, 0f, 0.52f));
            SetRect(dimmer.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panel = CreateImage(modalRoot.transform, "Game Menu Panel", new Color(0.96f, 0.88f, 0.66f));
            panel.sprite = GetRoundedButtonSprite();
            panel.type = Image.Type.Sliced;
            SetRect(panel.rectTransform, new Vector2(0.38f, 0.22f), new Vector2(0.62f, 0.78f), Vector2.zero, Vector2.zero);

            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.22f, 0.16f, 0.1f);
            outline.effectDistance = new Vector2(4f, -4f);

            var title = CreateText(panel.transform, "菜单", 34, TextAnchor.MiddleCenter, new Color(0.18f, 0.12f, 0.08f));
            SetRect(title.rectTransform, new Vector2(0.12f, 0.84f), new Vector2(0.88f, 0.96f), Vector2.zero, Vector2.zero);

            var saveButton = CreateButton(panel.transform, "保存游戏");
            SetRect(saveButton.GetComponent<RectTransform>(), new Vector2(0.17f, 0.66f), new Vector2(0.83f, 0.77f), Vector2.zero, Vector2.zero);
            saveButton.interactable = false;

            var loadButton = CreateButton(panel.transform, "加载存档");
            SetRect(loadButton.GetComponent<RectTransform>(), new Vector2(0.17f, 0.5f), new Vector2(0.83f, 0.61f), Vector2.zero, Vector2.zero);
            loadButton.interactable = false;

            var homeButton = CreateButton(panel.transform, "回到主页");
            SetRect(homeButton.GetComponent<RectTransform>(), new Vector2(0.17f, 0.34f), new Vector2(0.83f, 0.45f), Vector2.zero, Vector2.zero);
            homeButton.onClick.AddListener(ReturnToMainMenu);

            var quitButton = CreateButton(panel.transform, "退出游戏");
            SetRect(quitButton.GetComponent<RectTransform>(), new Vector2(0.17f, 0.18f), new Vector2(0.83f, 0.29f), Vector2.zero, Vector2.zero);
            quitButton.interactable = false;

            var closeButton = CreateButton(panel.transform, "关闭");
            SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.31f, 0.045f), new Vector2(0.69f, 0.13f), Vector2.zero, Vector2.zero);
            closeButton.onClick.AddListener(() =>
            {
                if (Application.isPlaying)
                    Destroy(modalRoot);
                else
                    DestroyImmediate(modalRoot);
            });
        }

        private void ToggleGameMenuFromKeyboard()
        {
            if (isResolvingTurn)
                return;

            var canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
                return;

            if (CloseExistingGameMenu(canvas.transform))
                return;

            ShowGameMenu(canvas.transform);
        }

        private void ReturnToMainMenu()
        {
            StopAllCoroutines();
            SceneManager.LoadScene(MainMenuSceneName);
        }

        private static bool CloseExistingGameMenu(Transform parent)
        {
            if (parent == null)
                return false;

            var closed = false;
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child == null || child.name != "Game Menu Modal")
                    continue;

                if (Application.isPlaying)
                    Object.Destroy(child.gameObject);
                else
                    Object.DestroyImmediate(child.gameObject);

                closed = true;
            }

            return closed;
        }
    }
}
