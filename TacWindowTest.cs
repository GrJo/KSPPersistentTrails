using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

using Tac;
using KSP.IO;
using UnityEngine;

// A sample usage of TacLib to create a window that remembers state and position ?per-ship? and features a "Show on startup" option

// To use: Compile to create the DLL and add the following to end of any part, before the final }
/*
MODULE
{
	name = TacWindowTest
	debug = True
}
*/

public class TacWindowTest : PartModule
{
    // Set debug according to setting in part.cfg
    [KSPField]
    public bool debug = false;

    private MyWindow mainWindow;
    
    // Fired first - this is at KSP load-time (When the loading bar hits a part with this mod)
    public override void OnAwake()
    {
        base.OnAwake();
        if (debug) Debug.Log("[TWT] OnAwake fired");
    }

    // Fires at multiple times, but mainly when scene loads - node contains scene ConfigNode data (all craft in savegame)
    // IMPORTANT! This also fires at KSP load-time. DO NOT try and start the GUI here.
    public override void OnLoad(ConfigNode node)
    {
        base.OnLoad(node);

        if (debug) Debug.Log("[TWT] OnLoad fired");
        // Only fire Load when we are loading a scene, not loading KSP.
        if (HighLogic.LoadedSceneIsFlight)
        {
            // Load settings for mainWindow
            // Second parameter is used to limit this window to only show for the vessel that created it
            // mainWindow = new MyWindow("My Window", this.vessel);
            mainWindow = new MyWindow("My Window", this);
            
            mainWindow.Load(node);
        }
    }

    // Fired when scene containing part Saves (Ends)
    public override void OnSave(ConfigNode node)
    {
        base.OnSave(node);
        // OnSave seems to fire in the Editor when you place the part or when you load a craft containing it
        // Code bombs if mainwindow.Save called at this point
        if (HighLogic.LoadedSceneIsFlight)
        {
            if (debug) Debug.Log("[TWT] OnSave fired");
            mainWindow.Save(node);
        }
    }

    // Fired once, when a scene starts containing the part
    public void Start()
    {
        if (debug) Debug.Log("[TWT] Start fired");
        mainWindow.SetResizeX(false);           // Disallow horizontal resizing
        mainWindow.SetVisible(mainWindow.uistatus.ShowOnStartup);
        // mainwindow is now passed all info it need to set up, so fire Start()
        //MyWindow.LogHandler myLogger = new MyWindow.LogHandler(Logger);
        mainWindow.testHandler = new MyWindow.TestHandler(DoSomething);
    }

    //public delegate void BuildHandler(string message);

    // Fires ?every frame? while the GUI is active
    public void OnGUI()
    {
    }
    
    // =====================================================================================================================================================
    // Flight UI and Action Group Hooks

    [KSPEvent(guiActive = true, guiName = "Show Main Menu", active = true)]
    public void ShowMainMenu()
    {
        mainWindow.SetVisible(true);
    }

    [KSPEvent(guiActive = true, guiName = "Hide Main Menu", active = false)]
    public void HideMainMenu()
    {
        mainWindow.SetVisible(false);
    }

    [KSPAction("Show Main Menu")]
    public void ShowMainMenuAction(KSPActionParam param)
    {
        ShowMainMenu();
    }

    [KSPAction("Hide Main Menu")]
    public void HideMainMenuAction(KSPActionParam param)
    {
        HideMainMenu();
    }

    public override void OnUpdate()
    {
        Events["ShowMainMenu"].active = !mainWindow.IsVisible();
        Events["HideMainMenu"].active = mainWindow.IsVisible();
    }

    // =====================================================================================================================================================
    static void DoSomething(string s)
    {
        Debug.Log("[TWT]: The Window said: " + s);
    }


}

/**
 * Instructions for use:
 *  (1) Create an instance of this class somewhere, preferrably where it will not be deleted/garbage collected.
 *  (2) Call SetVisible(true) to start showing it.
 *  (3) Call Load/Save if you want it to remember its position and size between runs.
 */
class MyWindow : Window<MyWindow>
{
    public TestHandler testHandler;

    // Use this class to store the current state of the UI
    public class UIStatus
    {
        public bool ShowOnStartup = true;   // A bit of an exception - this value is stored beteen sessions in config files.

        public Vector2 ContentScroller;         // Demo variable - can be removed. Holds the current position of the content scroller
        public bool ShowScroller = false;       // Demo variable - can be removed. Holds state of Show Scroller toggle
        public bool ShowSecondWindow = false;   // Demo variable - can be removed. Holds state of Show Second Window toggle
    }
    public UIStatus uistatus = new UIStatus();

    public SecondWindow secondWindow;

    public MyWindow(string name, PartModule p = null)
        : base(name, p)
    {
        // Force default size
        windowPos = new Rect(60, 60, 400, 400);
    }

    public delegate void TestHandler(string message);

    public void Test(TestHandler myHandler)
    {
        if (myHandler != null)
        {
            myHandler("Test Pressed!");
        }
    }

    // Called when UI is starting
    public void Start()
    {
        
    }

    protected override void DrawWindow()
    {
        //Example feature - prevent the window from being shown if the vessel is not prelaunch or landed.
        if (FlightGlobals.fetch && FlightGlobals.fetch.activeVessel != null)
        {
            var situation = FlightGlobals.fetch.activeVessel.situation;
            if (situation == Vessel.Situations.PRELAUNCH || situation == Vessel.Situations.LANDED)
            {
                base.DrawWindow();
            }
        }
    }

    protected override void ConfigureStyles()
    {
        base.ConfigureStyles();
        // Initialize your styles here (optional)
    }

    // Called every time the GUI paints (Often!)
    // All the code in here is example code and can be discarded.
    protected override void DrawWindowContents(int windowId)
    {
        // UI is defined here...
        GUILayout.BeginVertical();
        // Stuff here will be a "Header" and always visible
        GUILayout.Box("Hello World");

        // An example of how the UI works.
        // Bearing in mind DrawWindowContents executes constantly over and over...
        // uistatus.ShowScroller is a boolean that defines whether the scroller shows or not
        // By calling GUILayout.Toggle and passing it the current value of uistatus.ShowScroller, we create a toggle UI item that is in synch with the current state
        // GUILayout.Toggle returns the current state of the toggle - so setting uistatus.ShowScroller to the return value keeps it in synch with the UI Toggle item
        // ... the toggle will change state.
        uistatus.ShowScroller = GUILayout.Toggle(uistatus.ShowScroller, "Show scroller containing resource list");
        // If the toggle is in the ON state
        if (uistatus.ShowScroller)
        {
            // Begin a vertical scroller of unfixed height to hold a block of content.
            uistatus.ContentScroller = GUILayout.BeginScrollView(uistatus.ContentScroller, alwaysShowHorizontal: false, alwaysShowVertical: true);

            // The "Body" of the scroller
            GUILayout.Box("Craft access to all known resources:");
            foreach (PartResourceDefinition def in PartResourceLibrary.Instance.resourceDefinitions)
            {
                GUILayout.Box(def.name + ": " + Utilities.GetConnectedResources(this.myPartModule.part, def.name)[0].amount.ToString());
            }

            GUILayout.Box("And one that isn't - NonExistantResource: " + Utilities.GetConnectedResources(this.myPartModule.part, "NonExistantResource")[0].amount.ToString());
            // End the scroller
            GUILayout.EndScrollView();
        }

        // Another technique for toggles - use this method to execute code only when the state changes
        if (GUILayout.Toggle(uistatus.ShowSecondWindow, "Show Second Window"))
        {
            if (!uistatus.ShowSecondWindow)
            {
                uistatus.ShowSecondWindow = true;
                // Open window
                secondWindow.SetVisible(true);
                //secondWindow.LimitToVessel(this.GetVessel());
            }
        }
        else
        {
            if (uistatus.ShowSecondWindow)
            {
                uistatus.ShowSecondWindow = false;
                // Close window
                // ToDo: save window postion here?
                secondWindow.SetVisible(false);
            }
        }

        if (GUILayout.Button("Test"))
        {
            // How do I get this button to call DoSomething() ?
            Test(testHandler);
        }
        // Stuff below the scroller behaves like a "Footer"
        uistatus.ShowOnStartup = GUILayout.Toggle(uistatus.ShowOnStartup, "Show on StartUp");
        GUILayout.EndVertical();
    }

    public override void Load(ConfigNode node)
    {
        // Load base settings from global
        var configFilename = IOUtils.GetFilePathFor(this.GetType(), "TacWindowTest.cfg");
        ConfigNode config = ConfigNode.Load(configFilename);

        // Merge with per-ship settings
        if (config != null) config.CopyTo(node);
 
        // Apply settings
        base.Load(node);

        // Set uistatus.ShowOnStartup according to setting
        if (node.HasNode(GetConfigNodeName()))
        {
            var tmp = node.GetNode(GetConfigNodeName());

            uistatus.ShowOnStartup = Utilities.GetValue(tmp, "showonstartup", uistatus.ShowOnStartup);
        }
        //secondWindow = new SecondWindow("Second Window", this.GetVessel());
        secondWindow = new SecondWindow("Second Window", this.myPartModule);

    }

    public override void Save(ConfigNode node)
    {
        // Start with fresh node
        var configFilename = IOUtils.GetFilePathFor(this.GetType(), "TacWindowTest.cfg");
        ConfigNode config = new ConfigNode();

        // Add Window information to node
        base.Save(config);

        // Add custom info to the WINDOW settings
        config.GetNode(GetConfigNodeName()).AddValue("showonstartup", uistatus.ShowOnStartup);

        // Save global settings
        config.Save(configFilename);

        // Save Per-Ship settings
        config.CopyTo(node);
    }
}

// Derive a second window from the base class - we do not need all those extra extensions for this
class SecondWindow : Window<SecondWindow>
{
    public SecondWindow(string name, PartModule p = null)
        : base(name, p)
    {
        // Force default size
        windowPos = new Rect(60, 60, 400, 400);
    }

    protected override void DrawWindowContents(int windowId)
    {
        GUILayout.BeginVertical();
        GUILayout.Box("Second Window");
        GUILayout.EndVertical();
    }
}