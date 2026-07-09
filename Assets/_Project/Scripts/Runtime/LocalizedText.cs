using UnityEngine;
using UnityEngine.UI;

namespace ColorGateRush
{
    [RequireComponent(typeof(Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private LocalizationKey key;

        private Text _text;

        // Configures this text element to follow a localization key.
        public void Configure(LocalizationKey localizationKey)
        {
            key = localizationKey;
            Refresh();
        }

        // Subscribes to language changes when this UI label is active.
        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += Refresh;
            Refresh();
        }

        // Unsubscribes from language changes to avoid stale UI callbacks.
        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= Refresh;
        }

        // Applies the current translation to the attached uGUI Text component.
        private void Refresh()
        {
            if (_text == null)
            {
                _text = GetComponent<Text>();
            }

            if (_text != null)
            {
                _text.text = LocalizationManager.T(key);
            }
        }
    }
}
