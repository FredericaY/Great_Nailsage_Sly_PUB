using System;
using System.Collections.Generic;
using Game.Core.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI
{
    [DisallowMultipleComponent]
    public class PauseMenuController : MonoBehaviour
    {
        private enum PageId
        {
            Setting = 0,
            Journal = 1,
            Charm = 2
        }

        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private List<Canvas> canvasesToDisableOnOpen = new();

        [Header("References")]
        [SerializeField] private Canvas pauseMenuCanvas;
        [SerializeField] private RectTransform borderRoot;
        [SerializeField] private RectTransform blackBordersRoot;
        [SerializeField] private RectTransform centerOverlayRoot;
        [SerializeField] private RectTransform headerRoot;
        [SerializeField] private RectTransform footerRoot;

        [Header("Presentation")]
        [SerializeField] private float pageSlideSpeed = 12f;

        private GameInputRouter _inputRouter;
        private InputRebindController _rebindController;
        private RectTransform _contentViewportRoot;
        private RectTransform _pagesRoot;
        private RectTransform _settingPageRoot;
        private Text _pageTitleText;
        private Text _leftPageHintText;
        private Text _rightPageHintText;
        private Text _hintText;
        private PauseMenuSettingsPage _settingsPage;
        private PauseMenuJournalPage _journalPage;
        private PauseMenuCharmPage _charmPage;
        private PauseMenuPageBase[] _pages;
        private bool _isOpen;
        private int _currentPageIndex;
        private float _targetPagesX;
        private float _pageWidth;

        private void Awake()
        {
            EnsureDependencies();
            BindReferences();
            SetPauseVisible(false, true);
        }

        private void Update()
        {
            if (_inputRouter == null)
                return;

            if (!_isOpen)
            {
                if (_inputRouter.PausePressedThisFrame)
                    OpenPauseMenu();
                return;
            }

            UpdatePageAnimation();
            PauseMenuPageBase currentPage = GetCurrentPage();
            currentPage?.Tick(Time.unscaledDeltaTime);

            if (currentPage != null && currentPage.IsBusy)
            {
                currentPage.HandleInput();
                return;
            }

            if (_inputRouter.PausePressedThisFrame || _inputRouter.UICancelPressed)
            {
                ClosePauseMenu();
                return;
            }

            if (_inputRouter.UIPageLeftPressed)
                MovePage(-1);
            else if (_inputRouter.UIPageRightPressed)
                MovePage(1);

            currentPage?.HandleInput();
        }

        private void OnDisable()
        {
            if (_isOpen)
            {
                Time.timeScale = 1f;
                SetExternalCanvasesEnabled(true);
            }
        }

        private void EnsureDependencies()
        {
            if (pauseMenuCanvas == null)
                pauseMenuCanvas = FindPauseCanvas();

            _inputRouter = FindObjectOfType<GameInputRouter>();
            if (_inputRouter == null)
                _inputRouter = gameObject.AddComponent<GameInputRouter>();

            _rebindController = FindObjectOfType<InputRebindController>();
            if (_rebindController == null)
                _rebindController = gameObject.AddComponent<InputRebindController>();

            if (inputActions != null)
            {
                _inputRouter.Configure(inputActions);
                _rebindController.Configure(inputActions);
            }
        }

        private void BindReferences()
        {
            if (pauseMenuCanvas == null)
                return;

            if (borderRoot == null)
                borderRoot = FindRect(pauseMenuCanvas.transform, "Border");
            if (blackBordersRoot == null)
                blackBordersRoot = FindRect(pauseMenuCanvas.transform, "BlackBorders");
            if (centerOverlayRoot == null)
                centerOverlayRoot = FindRect(pauseMenuCanvas.transform, "CenterOverlay");
            if (headerRoot == null)
                headerRoot = FindRect(pauseMenuCanvas.transform, "Header");
            if (footerRoot == null)
                footerRoot = FindRect(pauseMenuCanvas.transform, "Footer");

            if (_contentViewportRoot == null)
                _contentViewportRoot = FindRect(pauseMenuCanvas.transform, "ContentViewport");
            if (_pagesRoot == null)
                _pagesRoot = FindRect(pauseMenuCanvas.transform, "PagesRoot");
            if (_settingPageRoot == null)
                _settingPageRoot = FindRect(pauseMenuCanvas.transform, "SettingPage");
            if (_pageTitleText == null)
                _pageTitleText = FindText(pauseMenuCanvas.transform, "PageTitle");
            if (_leftPageHintText == null)
                _leftPageHintText = FindText(pauseMenuCanvas.transform, "LeftPageHint");
            if (_rightPageHintText == null)
                _rightPageHintText = FindText(pauseMenuCanvas.transform, "RightPageHint");
            if (_hintText == null)
                _hintText = FindText(pauseMenuCanvas.transform, "HintText");
            if (_settingsPage == null && _settingPageRoot != null)
                _settingsPage = _settingPageRoot.GetComponent<PauseMenuSettingsPage>();
            if (_journalPage == null)
            {
                RectTransform journalRoot = FindRect(pauseMenuCanvas.transform, "JournalPage");
                if (journalRoot != null)
                    _journalPage = journalRoot.GetComponent<PauseMenuJournalPage>();
            }
            if (_charmPage == null)
            {
                RectTransform charmRoot = FindRect(pauseMenuCanvas.transform, "CharmPage");
                if (charmRoot != null)
                    _charmPage = charmRoot.GetComponent<PauseMenuCharmPage>();
            }

            if (_settingsPage != null)
                _settingsPage.Configure(_inputRouter, _rebindController, _hintText);
            if (_journalPage != null)
                _journalPage.Configure();
            if (_charmPage != null)
                _charmPage.Configure(_inputRouter, _hintText);

            _pages = new PauseMenuPageBase[] { _settingsPage, _journalPage, _charmPage };
            _pageWidth = _settingPageRoot != null ? _settingPageRoot.rect.width : 0f;
            if (_pageTitleText != null && _pages[0] != null)
                _pageTitleText.text = _pages[0].PageTitle;
            RefreshPagePrompts();
        }

        private void OpenPauseMenu()
        {
            BindReferences();
            if (_inputRouter == null)
                return;

            _isOpen = true;
            Time.timeScale = 0f;
            _inputRouter.EnterUIMode(false);
            SetExternalCanvasesEnabled(false);
            if (_pageTitleText != null && _pages != null && _currentPageIndex >= 0 && _currentPageIndex < _pages.Length && _pages[_currentPageIndex] != null)
                _pageTitleText.text = _pages[_currentPageIndex].PageTitle;
            RefreshPagePrompts();
            SetPauseVisible(true, true);
            for (int i = 0; i < _pages.Length; i++)
                _pages[i]?.OnMenuOpened();
        }

        private void ClosePauseMenu()
        {
            _isOpen = false;
            for (int i = 0; i < _pages.Length; i++)
                _pages[i]?.OnMenuClosed();

            Time.timeScale = 1f;
            if (_inputRouter != null)
                _inputRouter.EnterGameplayMode();
            SetExternalCanvasesEnabled(true);
            SetPauseVisible(false, true);
        }

        private void MovePage(int delta)
        {
            const int pageCount = 3;
            _currentPageIndex = (_currentPageIndex + delta + pageCount) % pageCount;
            _targetPagesX = -_currentPageIndex * _pageWidth;
            if (_pageTitleText != null && _pages[_currentPageIndex] != null)
                _pageTitleText.text = _pages[_currentPageIndex].PageTitle;
            RefreshPagePrompts();
        }

        private void UpdatePageAnimation()
        {
            if (_pagesRoot == null)
                return;

            Vector2 pos = _pagesRoot.anchoredPosition;
            pos.x = Mathf.Lerp(pos.x, _targetPagesX, 1f - Mathf.Exp(-pageSlideSpeed * Time.unscaledDeltaTime));
            _pagesRoot.anchoredPosition = pos;
            RefreshPagePrompts();
        }

        private PauseMenuPageBase GetCurrentPage()
        {
            if (_pages == null || _currentPageIndex < 0 || _currentPageIndex >= _pages.Length)
                return null;
            return _pages[_currentPageIndex];
        }

        private void SetPauseVisible(bool visible, bool instant)
        {
            if (pauseMenuCanvas == null)
                return;

            SetRootVisible(borderRoot, visible);
            SetRootVisible(blackBordersRoot, visible);
            SetRootVisible(centerOverlayRoot, visible);
            SetRootVisible(headerRoot, visible);
            SetRootVisible(footerRoot, visible);
            SetRootVisible(_contentViewportRoot, visible);

            if (visible && instant && _pagesRoot != null)
                _pagesRoot.anchoredPosition = new Vector2(_targetPagesX, _pagesRoot.anchoredPosition.y);
        }

        private void SetExternalCanvasesEnabled(bool enabled)
        {
            if (canvasesToDisableOnOpen == null)
                return;

            for (int i = 0; i < canvasesToDisableOnOpen.Count; i++)
            {
                Canvas targetCanvas = canvasesToDisableOnOpen[i];
                if (targetCanvas != null)
                    targetCanvas.enabled = enabled;
            }
        }

        private void RefreshPagePrompts()
        {
            if (_leftPageHintText == null || _rightPageHintText == null || _inputRouter == null)
                return;

            string leftPrompt = _inputRouter.LastInputSource == GameInputRouter.InputPromptSource.Gamepad ? "LB" : "Q";
            string rightPrompt = _inputRouter.LastInputSource == GameInputRouter.InputPromptSource.Gamepad ? "RB" : "E";
            PageId leftPage = GetWrappedPage(-1);
            PageId rightPage = GetWrappedPage(1);
            _leftPageHintText.text = leftPrompt + "  " + GetPageLabel(leftPage);
            _rightPageHintText.text = GetPageLabel(rightPage) + "  " + rightPrompt;
        }

        private PageId GetWrappedPage(int delta)
        {
            const int pageCount = 3;
            int index = (_currentPageIndex + delta + pageCount) % pageCount;
            return (PageId)index;
        }

        private static string GetPageLabel(PageId page)
        {
            switch (page)
            {
                case PageId.Setting:
                    return "SETTING";
                case PageId.Journal:
                    return "JOURNAL";
                case PageId.Charm:
                    return "CHARM";
                default:
                    return string.Empty;
            }
        }

        private static RectTransform FindRect(Transform root, string name)
        {
            Transform child = FindDeepChild(root, name);
            return child != null ? child as RectTransform : null;
        }

        private static Text FindText(Transform root, string name)
        {
            Transform child = FindDeepChild(root, name);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root == null)
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (string.Equals(child.name, name, StringComparison.Ordinal))
                    return child;

                Transform nested = FindDeepChild(child, name);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private Canvas FindPauseCanvas()
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && string.Equals(canvases[i].gameObject.name, "PauseMenu", StringComparison.OrdinalIgnoreCase))
                    return canvases[i];
            }

            return FindObjectOfType<Canvas>(true);
        }

        private static void SetRootVisible(Component root, bool visible)
        {
            if (root != null)
                root.gameObject.SetActive(visible);
        }
    }
}
