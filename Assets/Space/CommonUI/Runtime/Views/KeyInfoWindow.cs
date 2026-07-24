using System.Collections;
using System.Text;
using SpaceGame.CommonUI.Input;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceGame.CommonUI.Views
{
    [DisallowMultipleComponent]
    public sealed class KeyInfoWindow : ModalWindowBase
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private KeyInfoRowView rowPrefab;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text emptyText;

        private Coroutine refreshRoutine;
        private LayoutElement summaryLayout;

        public void ConfigureView(
            RectTransform rowsRoot,
            KeyInfoRowView keyRowPrefab,
            Button close,
            TMP_Text noBindingsText)
        {
            content = rowsRoot;
            rowPrefab = keyRowPrefab;
            closeButton = close;
            emptyText = noBindingsText;
        }

        protected override void OnInitialized()
        {
            closeButton.onClick.AddListener(RequestClose);
            Context.BindingCatalog.BindingsChanged += RefreshRows;
            summaryLayout =
                emptyText.GetComponent<LayoutElement>() ??
                emptyText.gameObject.AddComponent<LayoutElement>();
            emptyText.alignment = TextAlignmentOptions.TopLeft;
            emptyText.overflowMode = TextOverflowModes.Overflow;
            emptyText.richText = true;
            emptyText.gameObject.SetActive(true);
            RefreshRows();
        }

        protected override void OnOpened()
        {
            RefreshRows();
        }

        protected override void OnDestroy()
        {
            if (Context?.BindingCatalog != null)
            {
                Context.BindingCatalog.BindingsChanged -= RefreshRows;
            }

            closeButton?.onClick.RemoveListener(RequestClose);
            base.OnDestroy();
        }

        private void RefreshRows()
        {
            var builder = new StringBuilder();
            foreach (InputBindingDefinition definition in
                     Context.BindingCatalog.Bindings)
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(EscapeRichText(definition.DisplayName));
                builder.Append("<pos=72%><color=#2EAEEA>");
                builder.Append(EscapeRichText(definition.GetDisplayString()));
                builder.Append("</color>");
            }

            int count = Context.BindingCatalog.Bindings.Count;
            emptyText.SetText(
                count == 0
                    ? "표시할 단축키가 없습니다."
                    : builder.ToString());
            summaryLayout.preferredHeight = Mathf.Max(80f, count * 36f);
            emptyText.SetLayoutDirty();
            emptyText.SetVerticesDirty();

            if (refreshRoutine != null)
            {
                StopCoroutine(refreshRoutine);
            }

            if (isActiveAndEnabled)
            {
                refreshRoutine = StartCoroutine(RefreshAfterLayout());
            }
        }

        private void RebuildLayout()
        {
            if (content == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            content.anchoredPosition =
                new Vector2(content.anchoredPosition.x, 0f);
        }

        private IEnumerator RefreshAfterLayout()
        {
            yield return null;
            RebuildLayout();
            emptyText.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();
            refreshRoutine = null;
        }

        private static string EscapeRichText(string value)
        {
            return (value ?? string.Empty)
                .Replace("<", "‹")
                .Replace(">", "›");
        }
    }
}
