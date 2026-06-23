using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using OpenWorld;

namespace UI.Panels
{
    public class PauseController : MonoBehaviour
    {
        private const string ChineseOption = "中文";
        private const string EnglishOption = "English";

        private UIDocument pauseUIDocument;
        private bool isPaused = false;

        private VisualElement root;
        private DropdownField languageDropdown;
        private Button resumeButton;
        private Button exitButton;

        void Start()
        {
            pauseUIDocument = GetComponent<UIDocument>();
            if (pauseUIDocument != null) {
                pauseUIDocument.rootVisualElement.style.display = DisplayStyle.None;
                InitializeUI();
            }
        }

        private void SetupLanguageDropdown()
        {
            if (languageDropdown == null) return;
            languageDropdown.choices = new List<string> { ChineseOption, EnglishOption };
            languageDropdown.value = I18nSystem.CurrentLanguage == Language.Chinese ? ChineseOption : EnglishOption;
            languageDropdown.RegisterValueChangedCallback(evt => {
                I18nSystem.SetLanguage(evt.newValue == ChineseOption ? Language.Chinese : Language.English);
            });
        }

        private void OnLanguageChanged()
        {
            if (languageDropdown != null) {
                string desired = I18nSystem.CurrentLanguage == Language.Chinese ? ChineseOption : EnglishOption;
                if (languageDropdown.value != desired) languageDropdown.SetValueWithoutNotify(desired);
            }
            I18nSystem.LocalizeTree(root);
        }

        private void InitializeUI()
        {
            root = pauseUIDocument.rootVisualElement;
            languageDropdown = root.Q<DropdownField>("language-dropdown");
            resumeButton = root.Q<Button>("resume-button");
            exitButton = root.Q<Button>("exit-button");

            if (resumeButton != null) resumeButton.clicked += ResumeGame;
            if (exitButton != null) exitButton.clicked += () => {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            };

            SetupLanguageDropdown();
            I18nSystem.LocalizeTree(root);

            I18nSystem.OnLanguageChanged -= OnLanguageChanged;
            I18nSystem.OnLanguageChanged += OnLanguageChanged;
        }

        private void OnDestroy()
        {
            I18nSystem.OnLanguageChanged -= OnLanguageChanged;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isPaused) ResumeGame();
                else PauseGame();
            }
        }

        public void PauseGame()
        {
            isPaused = true;
            Time.timeScale = 0;
            if (pauseUIDocument != null) {
                pauseUIDocument.rootVisualElement.style.display = DisplayStyle.Flex;
                // Add tiny animation class if needed
                pauseUIDocument.rootVisualElement.ToggleInClassList("pause-menu-visible");
            }
        }

        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1;
            if (pauseUIDocument != null) {
                pauseUIDocument.rootVisualElement.style.display = DisplayStyle.None;
                pauseUIDocument.rootVisualElement.RemoveFromClassList("pause-menu-visible");
            }
        }
    }
}
