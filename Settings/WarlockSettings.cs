#region Revision Info

// This file is part of Singular - A community driven Honorbuddy CC
// $Author: bobby53 $
// $Date: 2012-11-01 22:40:01 +0000 (Do, 01 Nov 2012) $
// $HeadURL: https://subversion.assembla.com/svn/singular_v3/trunk/Singular/Settings/WarlockSettings.cs $
// $LastChangedBy: bobby53 $
// $LastChangedDate: 2012-11-01 22:40:01 +0000 (Do, 01 Nov 2012) $
// $LastChangedRevision: 1058 $
// $Revision: 1058 $

#endregion

using System.ComponentModel;
using System.IO;
using Styx.Helpers;

using DefaultValue = Styx.Helpers.DefaultValueAttribute;
using Styx;
using Styx.CommonBot;


namespace Singular.Settings
{
    public enum WarlockMinion
    {
        None        = 0,
        Auto        = 1,
        Imp         = 688,
        Voidwalker  = 697,
        Succubus    = 712,
        Felhunter   = 691,
        Felguard    = 30146
    }



    internal class WarlockSettings : Styx.Helpers.Settings
    {

        public WarlockSettings()
            : base(Path.Combine(SingularSettings.SettingsPath, "Warlock.xml"))
        {
        }

        [Setting]
        [DefaultValue(WarlockMinion.Auto)]
        [Category("General")]
        [DisplayName("Minion")]
        [Description("The minion to use. Auto will auto select.  Voidwalker used if choice not available.")]
        public WarlockMinion Minion { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("General")]
        [DisplayName("Use AOE")]
        [Description("Enable/Disable Warlock's AOE.")]
        public bool UseAOE { get; set; }


#region Setting Helpers

        /// <summary>
        /// determines the best WarlockMinion value to use.  Attempts to use 
        /// user setting first, but if choice not available yet will choose Imp 
        /// for instances and Voidwalker for everything else.  
        /// </summary>
        /// <returns>WarlockMinion to use</returns>
        public static WarlockMinion GetBestMinion()
        {
            WarlockMinion bestMinion = SingularSettings.Instance.Warlock.Minion;
            if (bestMinion != WarlockMinion.None)
            {
                if (StyxWoW.Me.Specialization == WoWSpec.None)
                    return WarlockMinion.Imp;

                if (bestMinion == WarlockMinion.Auto)
                {
                    if (StyxWoW.Me.Specialization == WoWSpec.WarlockDemonology)
                        bestMinion = WarlockMinion.Felguard;
                    else if (SingularRoutine.CurrentWoWContext == WoWContext.Battlegrounds)
                        bestMinion = WarlockMinion.Succubus;
                    else if (SingularRoutine.CurrentWoWContext == WoWContext.Instances)
                        bestMinion = WarlockMinion.Felhunter;
                    else
                        bestMinion = WarlockMinion.Voidwalker;
                }

                SpellFindResults sfr;
                if (!SpellManager.FindSpell((int)bestMinion, out sfr))
                {
                    // default: use Imp in instances to be sure no Taunt
                    bestMinion = SingularRoutine.CurrentWoWContext == WoWContext.Instances ? WarlockMinion.Imp : WarlockMinion.Voidwalker;
                }
            }

            return bestMinion;
        }

#endregion

    }
}