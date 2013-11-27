/**
 * Window.cs
 * 
 * Thunder Aerospace Corporation's library for the Kerbal Space Program, by Taranis Elsu
 * 
 * (C) Copyright 2013, Taranis Elsu
 * 
 * Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 * 
 * This code is licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 3.0)
 * creative commons license. See <http://creativecommons.org/licenses/by-nc-sa/3.0/legalcode>
 * for full details.
 * 
 * Attribution — You are free to modify this code, so long as you mention that the resulting
 * work is based upon or adapted from this code.
 * 
 * Non-commercial - You may not use this work for commercial purposes.
 * 
 * Share Alike — If you alter, transform, or build upon this work, you may distribute the
 * resulting work only under the same or similar license to the CC BY-NC-SA 3.0 license.
 * 
 * Note that Thunder Aerospace Corporation is a ficticious entity created for entertainment
 * purposes. It is in no way meant to represent a real entity. Any similarity to a real entity
 * is purely coincidental.
 */

using System;
using UnityEngine;
using PersistentTrails;

namespace Tac
{
    abstract class Window<T>
    {
        private string windowTitle;
        private int windowId;
        private string configNodeName;
        protected Rect windowPos;
        private bool windowMouseDown;
        private bool visible;
        protected PartModule myPartModule;

        private bool windowResizableX = false;
        private bool windowResizableY = true;
        
        protected GUIStyle closeButtonStyle;
        private GUIStyle resizeStyle;
        private GUIContent resizeContent;

        protected Window(string windowTitle, PartModule p = null)
        {
            this.myPartModule = p;

            this.windowTitle = windowTitle;
            this.windowId = windowTitle.GetHashCode() + new System.Random().Next(65536);

            configNodeName = windowTitle.Replace(" ", "");

            windowPos = new Rect(60, 60, 60, 60);
            windowMouseDown = false;
            visible = false;

            // Seems to break compile on monodevelop?
            //var texture = Utilities.LoadImage<T>(IOUtils.GetFilePathFor(typeof(T), "resize.png"));
            //resizeContent = (texture != null) ? new GUIContent(texture, "Drag to resize the window.") : new GUIContent("R", "Drag to resize the window.");
            resizeContent = new GUIContent("R", "Drag to resize the window.");
            Debug.Log("Window created: " + windowTitle);
        }

        public bool IsVisible()
        {
            return visible;
        }

        protected string GetConfigNodeName()
        {
            return configNodeName;
        }

        public virtual void SetVisible(bool newValue)
        {
            if (newValue)
            {
                if (!visible)
                {
                    RenderingManager.AddToPostDrawQueue(3, new Callback(DrawWindow));
                }
            }
            else
            {
                if (visible)
                {
                    RenderingManager.RemoveFromPostDrawQueue(3, new Callback(DrawWindow));
                }
            }

            this.visible = newValue;
        }

        public void SetResizeX(bool newValue)
        {
            windowResizableX = newValue;
        }

        public void SetResizeY(bool newValue)
        {
            windowResizableY = newValue;
        }

        public void ToggleVisible()
        {
            Debug.Log("Window.toggleVisible");
            SetVisible(!visible);
        }

        public void SetSize(int width, int height)
        {
            windowPos.width = width;
            windowPos.height = height;
        }

        public virtual void Load(ConfigNode config)
        {
            if (config.HasNode(configNodeName))
            {
                ConfigNode windowConfig = config.GetNode(configNodeName);

                windowPos.x = GUIResources.GetValue(windowConfig, "x", windowPos.x);
                windowPos.y = GUIResources.GetValue(windowConfig, "y", windowPos.y);
                windowPos.width = GUIResources.GetValue(windowConfig, "width", windowPos.width);
                windowPos.height = GUIResources.GetValue(windowConfig, "height", windowPos.height);

                bool newValue = GUIResources.GetValue(windowConfig, "visible", visible);
                //SetVisible(newValue);
            }
        }

        public virtual void Save(ConfigNode config)
        {
            ConfigNode windowConfig;
            if (config.HasNode(configNodeName))
            {
                windowConfig = config.GetNode(configNodeName);
            }
            else
            {
                windowConfig = new ConfigNode(configNodeName);
                config.AddNode(windowConfig);
            }

            windowConfig.AddValue("visible", visible);
            windowConfig.AddValue("x", windowPos.x);
            windowConfig.AddValue("y", windowPos.y);
            windowConfig.AddValue("width", windowPos.width);
            windowConfig.AddValue("height", windowPos.height);
        }

        protected bool allowedToDraw()
        {
            //Debug.Log("Window::AllowedToDraw?");
            if (this.myPartModule == null || this.myPartModule.vessel == null || this.myPartModule.vessel == FlightGlobals.ActiveVessel)
            {
                return true;                
            }
            else
            {
                return false;
            }
        }

        protected virtual void DrawWindow()
        {
            if (visible && allowedToDraw())
            {
                bool paused = false;
                if (HighLogic.LoadedSceneIsFlight)
                {
                    try
                    {
                        paused = PauseMenu.isOpen || FlightResultsDialog.isDisplaying;
                    }
                    catch (Exception)
                    {
                        // ignore the error and assume the pause menu is not open
                    }
                }

                if (!paused)
                {
                    GUI.skin = HighLogic.Skin;
                    ConfigureStyles();
                    windowPos = GUIResources.EnsureVisible(windowPos);
                    windowPos = GUILayout.Window(windowId, windowPos, PreDrawWindowContents, windowTitle, GUILayout.ExpandWidth(windowResizableX),
                        GUILayout.ExpandHeight(windowResizableY), GUILayout.MinWidth(windowPos.width), GUILayout.MinHeight(windowPos.height));
                }
            }
        }

        protected virtual void ConfigureStyles()
        {
            if (closeButtonStyle == null)
            {
                closeButtonStyle = new GUIStyle(GUI.skin.button);
                closeButtonStyle.padding = new RectOffset(5, 5, 3, 0);
                closeButtonStyle.margin = new RectOffset(1, 1, 1, 1);
                closeButtonStyle.stretchWidth = false;
                closeButtonStyle.stretchHeight = false;
                closeButtonStyle.alignment = TextAnchor.MiddleCenter;

                resizeStyle = new GUIStyle(GUI.skin.button);
                resizeStyle.alignment = TextAnchor.MiddleCenter;
                resizeStyle.padding = new RectOffset(1, 1, 1, 1);
            }
        }

        private void PreDrawWindowContents(int windowId)
        {
            DrawWindowContents(windowId);

            if (GUI.Button(new Rect(windowPos.width - 24, 4, 20, 20), "X", closeButtonStyle))
            {
                SetVisible(false);
            }

            var resizeRect = new Rect(windowPos.width - 16, windowPos.height - 16, 16, 16);
            GUI.Label(resizeRect, resizeContent, resizeStyle);

            HandleWindowEvents(resizeRect);

            GUI.DragWindow();
        }

        protected abstract void DrawWindowContents(int windowId);

        private void HandleWindowEvents(Rect resizeRect)
        {
            var theEvent = Event.current;
            if (theEvent != null)
            {
                if (theEvent.type == EventType.MouseDown && !windowMouseDown && theEvent.button == 0 && resizeRect.Contains(theEvent.mousePosition))
                {
                    windowMouseDown = true;
                    theEvent.Use();
                }
                else if (theEvent.type == EventType.MouseDrag && windowMouseDown && theEvent.button == 0)
                {
                    if (windowResizableX)
                    {
                        windowPos.width += theEvent.delta.x;
                    }
                    if (windowResizableY)
                    {
                        windowPos.height += theEvent.delta.y;
                    }
                    theEvent.Use();
                }
                else if (theEvent.type == EventType.MouseUp && windowMouseDown && theEvent.button == 0)
                {
                    windowMouseDown = false;
                    theEvent.Use();
                }
            }
        }
    }
}
