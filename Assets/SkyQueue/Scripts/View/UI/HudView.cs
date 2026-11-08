using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SkyQueue
{
    /// <summary>
    /// Gameplay HUD (level label, restart/pause, booster bar placeholder, toasts, result/pause panels). Raises events;
    /// owns no game state. All elements live inside "frames": rects sized like the reference canvas and scaled/centred
    /// onto the fitted gameplay frame (see <see cref="ScreenMap"/>), so the HUD matches the scene on any screen shape.
    /// Dimmer panels stretch over the whole screen while their content stays inside a frame.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        public event Action RestartRequested, PauseRequested, ResumeRequested, ResultConfirmed, BuyConfirmed, BuyCancelled;
        public event Action<int> BoosterPressed, SettingToggled;   // setting: 0 sound, 1 haptics, 2 motion
        public event Action IntroClosed;

        readonly List<RectTransform> frames = new List<RectTransform>();
        RectTransform canvasRect;
        HudLayout layout;
        Text levelText, coinsText, toastText, bannerText, resultTitle, resultReward, resultButtonText;
        GameObject resultPanel, pausePanel, buyPanel, introPanel;
        Text introTitle, introBody, introGift;
        readonly Text[] settingLabels = new Text[3];
        Text buyTitle, buyDescription, buyCost;
        Button buyButton;
        readonly List<Button> boosterButtons = new List<Button>(3);
        readonly List<bool> boosterLocked = new List<bool>(3);
        int boosterArmed = -1;
        Coroutine toastRoutine, bannerRoutine;
        readonly List<RectTransform> boosterRects = new List<RectTransform>(3);
        readonly List<RaycastResult> uiHits = new List<RaycastResult>(8);   // reused: no allocation per tap
        PointerEventData uiProbe;

        public static HudView Create(HudLayout layout)
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
                es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();   // not done automatically for runtime-created modules
            }

            var go = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;   // 1 canvas unit = 1 screen pixel; frames do the scaling
            UiFactory.Configure(layout.canvasSize);
            var hud = go.AddComponent<HudView>();
            hud.canvasRect = go.GetComponent<RectTransform>();
            hud.layout = layout;
            hud.Build(canvas.transform);
            return hud;
        }

        RectTransform NewFrame(Transform parent, string name)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = layout.canvasSize;
            frames.Add(rt);
            return rt;
        }

        /// <summary>Place every frame over the fitted gameplay frame (screen pixels, origin bottom-left).</summary>
        public void SetFrame(Rect framePx)
        {
            Vector2 screenCenter = new Vector2(Screen.width, Screen.height) * 0.5f;
            Vector2 offset = framePx.center - screenCenter;
            float scale = framePx.height / layout.canvasSize.y;
            for (int i = 0; i < frames.Count; i++)
            {
                frames[i].anchoredPosition = offset;
                frames[i].localScale = new Vector3(scale, scale, 1f);
            }
        }

        void Build(Transform root)
        {
            var hud = NewFrame(root, "HudFrame");
            levelText = UiFactory.Text("", hud, layout.levelLabel, layout.levelLabelSize, layout.levelFontSize, Color.white);
            coinsText = UiFactory.Text("", hud, layout.coinsLabel, layout.coinsLabelSize, layout.coinsFontSize, layout.coinsColor);
            UiFactory.Button("R", hud, layout.restartButton, layout.cornerButtonSize, layout.accent, layout.cornerFontSize, () => Raise(RestartRequested));
            UiFactory.Button("II", hud, layout.pauseButton, layout.cornerButtonSize, layout.accent, layout.cornerFontSize, () => Raise(PauseRequested));

            // Booster bar: composition matches the reference; the actions arrive in a later phase, so the buttons are visibly inactive.
            for (int i = 0; i < layout.boosterLabels.Length; i++)
            {
                var pos = new Vector2(layout.boosterXs[i], layout.boosterY);
                int index = i;
                var b = UiFactory.Button(layout.boosterLabels[i], hud, pos, layout.boosterSize, layout.boosterColor, layout.boosterFontSize, () => RaiseBooster(index));
                boosterButtons.Add(b);
                boosterLocked.Add(false);
                boosterRects.Add((RectTransform)b.transform);
                boosterIcons.Add(AddBoosterIcon(b, i));
            }

            toastText = UiFactory.Text("", hud, layout.toast, layout.toastSize, layout.toastFontSize, Color.white);
            bannerText = UiFactory.Text("", hud, layout.banner, layout.bannerSize, layout.bannerFontSize, layout.bannerColor);

            resultPanel = UiFactory.Overlay("ResultPanel", root, layout.dim);
            var rf = NewFrame(resultPanel.transform, "ResultFrame");
            resultTitle = UiFactory.Text("", rf, layout.resultTitle, layout.resultTitleSize, layout.resultTitleFontSize, Color.white);
            resultReward = UiFactory.Text("", rf, layout.resultReward, layout.resultRewardSize, layout.resultRewardFontSize, layout.coinsColor);
            var next = UiFactory.Button("Next", rf, layout.primaryButton, layout.panelButtonSize, layout.positive, layout.panelFontSize, () => Raise(ResultConfirmed));
            resultButtonText = next.GetComponentInChildren<Text>();
            resultPanel.SetActive(false);

            pausePanel = UiFactory.Overlay("PausePanel", root, layout.dim);
            var pf = NewFrame(pausePanel.transform, "PauseFrame");
            UiFactory.Text("Paused", pf, layout.pauseTitle, layout.resultTitleSize, layout.resultTitleFontSize, Color.white);
            UiFactory.Button("Resume", pf, layout.primaryButton, layout.panelButtonSize, layout.positive, layout.panelFontSize, () => Raise(ResumeRequested));
            UiFactory.Button("Restart", pf, layout.secondaryButton, layout.panelButtonSize, layout.accent, layout.panelFontSize, () => Raise(RestartRequested));
            Vector2[] settingPos = { layout.settingSound, layout.settingHaptics, layout.settingMotion };
            for (int i = 0; i < settingPos.Length; i++)
            {
                int which = i;
                var sb = UiFactory.Button("Setting" + i, pf, settingPos[i], layout.settingButtonSize, layout.settingColor, layout.settingFontSize, () => { if (SettingToggled != null) SettingToggled(which); });
                settingLabels[i] = sb.GetComponentInChildren<Text>();
                settingLabels[i].fontSize = layout.settingFontSize;
            }
            pausePanel.SetActive(false);

            buyPanel = UiFactory.Overlay("BuyPanel", root, layout.dim);
            var bf = NewFrame(buyPanel.transform, "BuyFrame");
            buyTitle = UiFactory.Text("", bf, layout.buyTitle, layout.resultTitleSize, layout.resultTitleFontSize, Color.white);
            buyDescription = UiFactory.Text("", bf, layout.buyDescription, layout.buyDescriptionSize, layout.buyDescriptionFontSize, Color.white);
            buyCost = UiFactory.Text("", bf, layout.buyCost, layout.buyDescriptionSize, layout.buyDescriptionFontSize, layout.coinsColor);
            buyButton = UiFactory.Button("Buy", bf, layout.buyButton, layout.panelButtonSize, layout.positive, layout.panelFontSize, () => Raise(BuyConfirmed));
            UiFactory.Button("Cancel", bf, layout.cancelButton, layout.panelButtonSize, layout.accent, layout.panelFontSize, () => Raise(BuyCancelled));
            buyPanel.SetActive(false);

            introPanel = UiFactory.Overlay("IntroPanel", root, layout.dim);
            var inf = NewFrame(introPanel.transform, "IntroFrame");
            introTitle = UiFactory.Text("", inf, layout.buyTitle, layout.resultTitleSize, layout.resultTitleFontSize * 3 / 4, Color.white);
            introBody = UiFactory.Text("", inf, layout.buyDescription, layout.buyDescriptionSize, layout.buyDescriptionFontSize, Color.white);
            introGift = UiFactory.Text("", inf, layout.buyCost, layout.buyDescriptionSize, layout.buyDescriptionFontSize, layout.coinsColor);
            UiFactory.Button("OK", inf, layout.introOkButton, layout.panelButtonSize, layout.positive, layout.panelFontSize, () => { introPanel.SetActive(false); Raise(IntroClosed); });
            introPanel.SetActive(false);
        }

        static void Raise(Action a) { if (a != null) a(); }

        /// <summary>
        /// True when an interactive UI graphic (a button or a blocking panel) is under a screen position, so a tap there
        /// belongs to the UI and must not also pick a plane underneath. Text and frame rects are not raycast targets.
        /// </summary>
        readonly List<Image> boosterIcons = new List<Image>(3);

        bool HasIcon(int i) { return i < boosterIcons.Count && boosterIcons[i] != null; }

        /// <summary>Puts the booster's icon on top of its button (reference look) and moves the label under it. Null when no icon is set.</summary>
        Image AddBoosterIcon(Button b, int i)
        {
            if (layout.boosterIcons == null || i >= layout.boosterIcons.Length || layout.boosterIcons[i] == null) return null;
            var rt = UiFactory.Rect("Icon", b.transform, layout.boosterIconCenter, layout.boosterIconSize);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = layout.boosterIcons[i]; img.preserveAspect = true; img.raycastTarget = false;
            var label = b.GetComponentInChildren<Text>();
            label.fontSize = layout.boosterIconFontSize;
            label.rectTransform.anchoredPosition = new Vector2(0f, -layout.boosterLabelDropPx * layout.canvasSize.x / GameConfig.RefW);
            label.transform.SetAsLastSibling();   // text over the icon's lower edge
            return img;
        }

        /// <summary>Index of the booster button under a screen position, or -1 (debug route checks).</summary>
        public int BoosterAt(Vector2 screenPosition)
        {
            for (int i = 0; i < boosterRects.Count; i++)
                if (RectTransformUtility.RectangleContainsScreenPoint(boosterRects[i], screenPosition, null)) return i;
            return -1;
        }

        public bool IsOverUi(Vector2 screenPosition)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            if (uiProbe == null || uiProbe.currentInputModule == null) uiProbe = new PointerEventData(es);
            uiProbe.position = screenPosition;
            uiHits.Clear();
            es.RaycastAll(uiProbe, uiHits);
            return uiHits.Count > 0;
        }

        public void SetLevel(int id) { levelText.text = "Level " + id; }
        public void ShowPause(bool visible) { if (visible) ShowPanel(pausePanel); else pausePanel.SetActive(false); }
        public void HideAllPanels() { resultPanel.SetActive(false); pausePanel.SetActive(false); buyPanel.SetActive(false); introPanel.SetActive(false); }

        // boosters ------------------------------------------------------------------------------------------------------
        public int BoosterButtonCount { get { return boosterButtons.Count; } }

        void RaiseBooster(int index) { if (BoosterPressed != null) BoosterPressed(index); }

        /// <summary>Shows the owned count, or a "+" (buy) marker when none are owned.</summary>
        public void SetBoosterCount(int index, int owned, int cost)
        {
            if (index < 0 || index >= boosterButtons.Count) return;
            boosterLocked[index] = false;
            PaintBooster(index);
            var t = boosterButtons[index].GetComponentInChildren<Text>();
            bool icon = HasIcon(index);
            t.text = owned > 0 ? string.Format(icon ? layout.boosterIconOwnedFormat : layout.boosterOwnedFormat, layout.boosterLabels[index], owned)
                               : string.Format(icon ? layout.boosterIconBuyFormat : layout.boosterBuyFormat, layout.boosterLabels[index], cost);
        }

        /// <summary>Marks the booster that is armed and waiting for a target (-1 = none).</summary>
        public void SetBoosterHighlight(int index)
        {
            boosterArmed = index;
            for (int i = 0; i < boosterButtons.Count; i++) PaintBooster(i);
        }

        void PaintBooster(int i)
        {
            boosterButtons[i].targetGraphic.color = boosterLocked[i] ? layout.boosterLockedColor
                                                  : i == boosterArmed ? layout.boosterArmedColor : layout.boosterColor;
            if (HasIcon(i)) boosterIcons[i].color = boosterLocked[i] ? layout.boosterIconLockedTint : Color.white;
        }

        public void ShowBuy(string title, string description, int cost, bool affordable)
        {
            buyTitle.text = title;
            buyDescription.text = description;
            buyCost.text = string.Format(affordable ? layout.buyCostFormat : layout.buyCostShortFormat, cost);
            buyButton.interactable = affordable;
            ShowPanel(buyPanel);
        }

        public void HideBuy() { buyPanel.SetActive(false); }

        /// <summary>Locked booster: shows the level it unlocks at, greyed.</summary>
        public void SetBoosterLocked(int index, int unlockLevel)
        {
            if (index < 0 || index >= boosterButtons.Count) return;
            boosterButtons[index].GetComponentInChildren<Text>().text = string.Format(HasIcon(index) ? layout.boosterIconLockedFormat : layout.boosterLockedFormat, layout.boosterLabels[index], unlockLevel);
            boosterLocked[index] = true;
            PaintBooster(index);
        }

        public string BoosterLockedMessage(int index, int unlockLevel)
        {
            string name = index >= 0 && index < layout.boosterLabels.Length ? layout.boosterLabels[index] : "Booster";
            return string.Format(layout.boosterLockedToast, name, unlockLevel);
        }

        /// <summary>First-time booster popup (unlock sequence).</summary>
        public void ShowIntro(string name, string description, int gift)
        {
            introTitle.text = string.Format(layout.introTitleFormat, name);
            introBody.text = description;
            introGift.text = gift > 0 ? string.Format(layout.introGiftFormat, gift) : "";
            ShowPanel(introPanel);
        }

        public bool IntroVisible { get { return introPanel.activeSelf; } }

        public void SetSettings(bool sound, bool haptics, bool reducedMotion)
        {
            settingLabels[0].text = string.Format(layout.soundFormat, sound ? layout.onText : layout.offText);
            settingLabels[1].text = string.Format(layout.hapticsFormat, haptics ? layout.onText : layout.offText);
            settingLabels[2].text = string.Format(layout.motionFormat, reducedMotion ? layout.reducedMotionText : layout.fullMotionText);
        }

        int shownCoins = -1;
        Coroutine coinRoutine;

        /// <summary>Rolls the counter to the new balance (instant the first time).</summary>
        public void SetCoins(int coins)
        {
            if (shownCoins < 0 || !isActiveAndEnabled) { shownCoins = coins; coinsText.text = string.Format(layout.coinsFormat, coins); return; }
            if (coinRoutine != null) StopCoroutine(coinRoutine);
            coinRoutine = StartCoroutine(RollCoins(shownCoins, coins));
        }

        IEnumerator RollCoins(int from, int to)
        {
            for (float t = 0f; t < layout.coinCountSeconds; t += Time.unscaledDeltaTime)
            {
                int v = Mathf.RoundToInt(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / layout.coinCountSeconds)));
                if (v != shownCoins) { shownCoins = v; coinsText.text = string.Format(layout.coinsFormat, v); }
                yield return null;
            }
            shownCoins = to;
            coinsText.text = string.Format(layout.coinsFormat, to);
        }

        /// <summary>Shows a popup panel with a short fade-in (unscaled time, works while the game is paused).</summary>
        void ShowPanel(GameObject panel)
        {
            panel.SetActive(true);
            var g = panel.GetComponent<CanvasGroup>();
            if (g == null) g = panel.AddComponent<CanvasGroup>();
            StartCoroutine(FadeIn(g));
        }

        IEnumerator FadeIn(CanvasGroup g)
        {
            for (float t = 0f; t < layout.panelFadeSeconds; t += Time.unscaledDeltaTime) { g.alpha = t / layout.panelFadeSeconds; yield return null; }
            g.alpha = 1f;
        }

        public void ShowResult(bool won, int level, WinReward reward)
        {
            resultTitle.text = won ? "Level " + level + " Complete!" : "No moves left";
            if (!won) resultReward.text = "";
            else if (reward.Total == 0) resultReward.text = layout.replayText;
            else
            {
                string line = reward.Base + reward.SuperHard > 0 ? string.Format(layout.rewardFormat, reward.Base + reward.SuperHard) : "";
                if (reward.Milestone > 0) line += (line.Length > 0 ? "   " : "") + string.Format(layout.milestoneFormat, reward.Milestone);
                resultReward.text = line;
            }
            resultButtonText.text = won ? "Next" : "Retry";
            ShowPanel(resultPanel);
        }

        public void Toast(string message) { Restart(ref toastRoutine, toastText, message, layout.toastSeconds); }
        public void Banner(string message) { Restart(ref bannerRoutine, bannerText, message, layout.bannerSeconds); }

        void Restart(ref Coroutine slot, Text target, string message, float duration)
        {
            if (slot != null) StopCoroutine(slot);
            slot = StartCoroutine(Fade(target, message, duration, layout.fadeSeconds));
        }

        static IEnumerator Fade(Text t, string message, float duration, float fade)
        {
            t.text = message;
            Color c = t.color;
            for (float e = 0f; e < duration; e += Time.unscaledDeltaTime)
            {
                c.a = Mathf.Clamp01((duration - e) / fade);
                t.color = c;
                yield return null;
            }
            t.text = "";
            c.a = 1f; t.color = c;
        }
    }
}
