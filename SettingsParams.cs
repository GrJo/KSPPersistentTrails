using System.Reflection;

namespace PersistentTrails
{
    public class PersistentTrails_SettingsParms : GameParameters.CustomParameterNode

    {
        public override string Title { get { return "PersistentTrails Options"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override bool HasPresets { get { return false; } }
        public override string Section { get { return "PersistentTrails"; } }
        public override string DisplaySection { get { return "PersistentTrails"; } }
        public override int SectionOrder { get { return 1; } }
        
        [GameParameters.CustomParameterUI("Use Stock Application Launcher Icon", toolTip = "If on, the Stock Application launcher will be used,\nif off will use Blizzy Toolbar if installed")]
        public bool UseAppLToolbar = true;

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            if (member.Name == "UseAppLToolbar")
            {
                if (!ToolbarManager.ToolbarAvailable)
                {
                    UseAppLToolbar = true;
                    return false;
                }
            }
            return true; //otherwise return true
        }
    }
}
