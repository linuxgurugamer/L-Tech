﻿/*
 * L-Tech Scientific Industries Continued
 * Copyright © 2015-2016, Arne Peirs (Olympic1)
 * Copyright © 2016, linuxgurugamer
 * 
 * Kerbal Space Program is Copyright © 2011-2016 Squad. See http://kerbalspaceprogram.com/.
 * This project is in no way associated with nor endorsed by Squad.
 * 
 * This file is part of Olympic1's L-Tech (Continued). Original author of L-Tech is 'ludsoe' on the KSP Forums.
 * This file was not part of the original L-Tech but was written by Arne Peirs.
 * Copyright © 2015-2016, Arne Peirs (Olympic1)
 * 
 * Continues to be licensed under the MIT License.
 * See <https://opensource.org/licenses/MIT> for full details.
 */

using KSP.UI.Screens;
using LtScience.APIClients;
using LtScience.InternalObjects;
using LtScience.Windows;
using System;
using UnityEngine;

namespace LtScience
{
    [KSPAddon(KSPAddon.Startup.FlightAndKSC, false)]
    public class LtAddon : MonoBehaviour
    {
        #region Properties

        // GUI styles
        internal static GUIStyle WindowStyle;
        internal static GUIStyle ButtonStyle;
        internal static GUIStyle ButtonToggledStyle;
        internal static GUIStyle ToggleStyleHeader;
        internal static GUIStyle LabelStyle;
        internal static GUIStyle LabelTabHeader;
        internal static GUIStyle LabelStyleHardRule;
        internal static GUIStyle ScrollStyle;
        internal static GUIStyle ToolTipStyle;

        // Toolbar integration
        private static IButton _blizzyButton;
        private static ApplicationLauncherButton _stockButton;

        // Toolbar icons
        private const string BlizzyOff = "LTech/Plugins/LT_blizzy_off";
        private const string BlizzyOn = "LTech/Plugins/LT_blizzy_on";
        private const string StockOff = "LTech/Plugins/LT_stock_off";
        private const string StockOn = "LTech/Plugins/LT_stock_on";

        // Camera UI toggle
        internal static bool ShowUi = true;

        // Makes instance available via reflection
        private static LtAddon Instance
        {
            get;
            set;
        }

        #endregion

        #region Constructor

        public LtAddon()
        {
            if (Instance == null)
                Instance = this;
        }

        #endregion

        #region Event Handlers

        private static void DummyVoid()
        { }

        internal void Awake()
        {
            try
            {
                if (HighLogic.LoadedScene != GameScenes.SPACECENTER && HighLogic.LoadedScene != GameScenes.FLIGHT)
                    return;

                DontDestroyOnLoad(this);
                LtSettings.LoadSettings();

                // Added support for Blizzy toolbar and hot switching between Stock and Blizzy
                if (LtSettings.enableBlizzyToolbar)
                {
                    // Let't try to use Blizzy's toolbar
                    if (ActivateBlizzyToolBar())
                        return;

                    // We failed to activate the toolbar, so revert to Stock
                    GameEvents.onGUIApplicationLauncherReady.Add(OnGuiAppLauncherReady);
                    GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGuiAppLauncherDestroyed);
                }
                else
                {
                    // Use Stock toolbar
                    GameEvents.onGUIApplicationLauncherReady.Add(OnGuiAppLauncherReady);
                    GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGuiAppLauncherDestroyed);
                }
            }
            catch (Exception ex)
            {
                Util.LogMessage("LTAddon.Awake. Error: " + ex, Util.LogType.Error);
            }
        }

        internal void Start()
        {
            try
            {
                if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
                    LtSettings.SaveSettings();

                // Instantiate Event Handlers
                GameEvents.onGameSceneSwitchRequested.Add(OnGameSceneSwitchRequested);

                // If we are not in flight, the rest does not get done!
                if (HighLogic.LoadedScene != GameScenes.FLIGHT)
                    return;

                GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequested);
                GameEvents.onShowUI.Add(OnShowUi);
                GameEvents.onHideUI.Add(OnHideUi);
            }
            catch (Exception ex)
            {
                Util.LogMessage("LTAddon.Start. Error: " + ex, Util.LogType.Error);
            }
        }

        internal void OnDestroy()
        {
            try
            {
                if (LtSettings.loaded)
                    LtSettings.SaveSettings();

                GameEvents.onGameSceneSwitchRequested.Remove(OnGameSceneSwitchRequested);

                GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequested);
                GameEvents.onShowUI.Remove(OnShowUi);
                GameEvents.onHideUI.Remove(OnHideUi);

                // Handle toolbars
                if (_blizzyButton == null)
                {
                    if (_stockButton != null)
                    {
                        ApplicationLauncher.Instance.RemoveModApplication(_stockButton);
                        _stockButton = null;
                    }

                    if (_stockButton == null)
                    {
                        // Remove the Stock toolbar button
                        GameEvents.onGUIApplicationLauncherReady.Remove(OnGuiAppLauncherReady);
                    }
                }
                else
                {
                    _blizzyButton?.Destroy();
                }
            }
            catch (Exception ex)
            {
                Util.LogMessage("LTAddon.OnDestroy. Error: " + ex, Util.LogType.Error);
            }
        }

        internal void OnGUI()
        {
            try
            {
                GUI.skin = HighLogic.Skin;

                SetupGuiStyles();
                Display();
                LtToolTips.ShowToolTips();
            }
            catch (Exception ex)
            {
                Util.LogMessage("LTAddon.OnGUI. Error: " + ex, Util.LogType.Error);
            }
        }

        internal void Update()
        {
            try
            {
                if (HighLogic.LoadedScene == GameScenes.SPACECENTER && HighLogic.LoadedSceneIsFlight)
                    CheckForToolbarTypeToggle();
            }
            catch (Exception ex)
            {
                Util.LogMessage($"LTAddon.Update (repeating error). Error: {ex.Message} \r\n\r\n{ex.StackTrace}", Util.LogType.Error);
            }
        }

        // Save settings on scene changes
        private void OnGameSceneLoadRequested(GameScenes requestedScene)
        {
            LtSettings.SaveSettings();
        }

        private void OnGameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> sceneData)
        {
            WindowSettings.showWindow = WindowSkyLab.showWindow = false;
        }

        // Camera UI toggle handlers
        private void OnShowUi()
        {
            ShowUi = true;
        }

        private void OnHideUi()
        {
            ShowUi = false;
        }

        // Stock vs Blizzy toolbar switch handler
        private void CheckForToolbarTypeToggle()
        {
            if (LtSettings.enableBlizzyToolbar && !LtSettings.prevEnableBlizzyToolbar)
            {
                // Let't try to use Blizzy's toolbar
                if (!ActivateBlizzyToolBar())
                {
                    // We failed to activate the toolbar, so revert to Stock
                    GameEvents.onGUIApplicationLauncherReady.Add(OnGuiAppLauncherReady);
                    GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGuiAppLauncherDestroyed);
                    LtSettings.enableBlizzyToolbar = LtSettings.prevEnableBlizzyToolbar;
                }
                else
                {
                    // Use Blizzy toolbar
                    OnGuiAppLauncherDestroyed();
                    GameEvents.onGUIApplicationLauncherReady.Remove(OnGuiAppLauncherReady);
                    GameEvents.onGUIApplicationLauncherDestroyed.Remove(OnGuiAppLauncherDestroyed);
                    LtSettings.prevEnableBlizzyToolbar = LtSettings.enableBlizzyToolbar;

                    if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedSceneIsFlight)
                        _blizzyButton.Visible = true;
                }
            }
            else if (!LtSettings.enableBlizzyToolbar && LtSettings.prevEnableBlizzyToolbar)
            {
                // Use Stock toolbar
                if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedSceneIsFlight)
                    _blizzyButton.Visible = false;

                GameEvents.onGUIApplicationLauncherReady.Add(OnGuiAppLauncherReady);
                GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGuiAppLauncherDestroyed);
                OnGuiAppLauncherReady();
                LtSettings.prevEnableBlizzyToolbar = LtSettings.enableBlizzyToolbar;
            }
        }

        // Stock toolbar startup and cleanup
        private void OnGuiAppLauncherReady()
        {
            try
            {
                // Setup Settings Button
                if ((HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedSceneIsFlight) && _stockButton == null && !LtSettings.enableBlizzyToolbar)
                {
                    _stockButton = ApplicationLauncher.Instance.AddModApplication(
                        OnToolbarButtonToggle,
                        OnToolbarButtonToggle,
                        DummyVoid, DummyVoid,
                        DummyVoid, DummyVoid,
                        ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT,
                        GameDatabase.Instance.GetTexture(StockOff, false));

                    if (WindowSettings.showWindow)
                        _stockButton.SetTexture(GameDatabase.Instance.GetTexture(WindowSettings.showWindow ? StockOn : StockOff, false));
                }
            }
            catch (Exception ex)
            {
                Util.LogMessage("LTAddon.OnGuiAppLauncherReady. Error: " + ex, Util.LogType.Error);
            }
        }

        private void OnGuiAppLauncherDestroyed()
        {
            try
            {
                if (_stockButton == null)
                    return;

                ApplicationLauncher.Instance.RemoveModApplication(_stockButton);
                _stockButton = null;
            }
            catch (Exception ex)
            {
                Util.LogMessage("LTAddon.OnGuiAppLauncherDestroyed. Error: " + ex, Util.LogType.Error);
            }
        }

        // Toolbar button click handler
        internal static void OnToolbarButtonToggle()
        {
            try
            {
                if (HighLogic.LoadedScene != GameScenes.SPACECENTER && HighLogic.LoadedScene != GameScenes.FLIGHT)
                    return;

                WindowSettings.showWindow = !WindowSettings.showWindow;

                if (LtSettings.enableBlizzyToolbar)
                    _blizzyButton.TexturePath = WindowSettings.showWindow ? BlizzyOn : BlizzyOff;
                else
                    _stockButton.SetTexture(GameDatabase.Instance.GetTexture(WindowSettings.showWindow ? StockOn : StockOff, false));
            }
            catch (Exception ex)
            {
                Util.LogMessage("LTAddon.OnToolbarButtonToggle. Error: " + ex, Util.LogType.Error);
            }
        }

        #endregion

        #region GUI Methods

        private void Display()
        {
            string step = "";
            try
            {
                step = "0 - Start";
                if ((HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedSceneIsFlight) && ShowUi)
                {
                    if (WindowSettings.showWindow)
                    {
                        step = "1 - Show Settings";
                        WindowSettings.position = GUILayout.Window(5234629, WindowSettings.position, WindowSettings.Display, WindowSettings.title, GUILayout.MinHeight(20));
                    }
                }
            }
            catch (Exception ex)
            {
                Util.LogMessage($"LTAddon.Display at or near step: {step}. Error: {ex.Message} \r\n\r\n{ex.StackTrace}", Util.LogType.Error);
            }
        }

        internal static void RepositionWindows()
        {
            RepositionWindow(ref WindowSettings.position);
            RepositionWindow(ref WindowSkyLab.position);
        }

        internal static void RepositionWindow(ref Rect position)
        {
            if (position.x < 0)
                position.x = 0;

            if (position.y < 0)
                position.y = 0;

            if (position.xMax > Screen.width)
                position.x = Screen.width - position.width;

            if (position.yMax > Screen.height)
                position.y = Screen.height - position.height;
        }

        private static void SetupGuiStyles()
        {
            if (WindowStyle != null)
                return;

            WindowStyle = new GUIStyle(GUI.skin.window);

            ButtonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { textColor = Color.white },
                hover = { textColor = Color.blue },
                fontSize = 12,
                padding =
                {
                    top = 0,
                    bottom = 0
                },
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip
            };

            ButtonToggledStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { textColor = Color.green },
                hover = { textColor = Color.blue },
                fontSize = 12,
                padding =
                {
                    top = 0,
                    bottom = 0
                },
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip
            };
            ButtonToggledStyle.normal.background = ButtonToggledStyle.onActive.background;

            ToggleStyleHeader = new GUIStyle(GUI.skin.toggle)
            {
                padding =
                {
                    top = 10,
                    bottom = 6
                },
                wordWrap = false,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 0, 0),
                alignment = TextAnchor.LowerLeft
            };

            LabelStyle = new GUIStyle(GUI.skin.label);

            LabelTabHeader = new GUIStyle(GUI.skin.label)
            {
                padding =
                {
                    top = 10,
                    bottom = 6
                },
                wordWrap = false,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 0, 0)
            };

            LabelStyleHardRule = new GUIStyle(GUI.skin.label)
            {
                padding =
                {
                    top = 0,
                    bottom = 6
                },
                wordWrap = false,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 0, 0),
                alignment = TextAnchor.LowerLeft
            };

            ScrollStyle = new GUIStyle(GUI.skin.box);

            if (GUI.skin != null)
                GUI.skin = null;

            ToolTipStyle = new GUIStyle(GUI.tooltip)
            {
                border = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(5, 5, 5, 5),
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Italic,
                wordWrap = false,
                normal = { textColor = Color.green },
                hover = { textColor = Color.green }
            };
            ToolTipStyle.hover.background = ToolTipStyle.normal.background;

            GUI.skin = HighLogic.Skin;
        }

        #endregion

        #region Action Methods

        private static bool ActivateBlizzyToolBar()
        {
            if (!LtSettings.enableBlizzyToolbar)
                return false;

            if (!ToolbarManager.ToolbarAvailable)
                return false;

            try
            {
                if (HighLogic.LoadedScene != GameScenes.SPACECENTER && HighLogic.LoadedScene != GameScenes.FLIGHT)
                    return true;

                _blizzyButton = ToolbarManager.Instance.add("L-Tech", "Settings");
                _blizzyButton.TexturePath = WindowSettings.showWindow ? BlizzyOn : BlizzyOff;
                _blizzyButton.ToolTip = "L-Tech Settings Window";
                _blizzyButton.Visibility = new GameScenesVisibility(GameScenes.SPACECENTER & GameScenes.FLIGHT);
                _blizzyButton.Visible = true;
                _blizzyButton.OnClick += e =>
                {
                    OnToolbarButtonToggle();
                };
                return true;
            }
            catch
            {
                // Blizzy Toolbar instantiation error - Ignore
                return false;
            }
        }

        #endregion
    }
}
