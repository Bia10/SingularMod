﻿#region Revision Info

// This file is part of Singular - A community driven Honorbuddy CC
// $Author: mastergrim $
// $Date: 2012-09-22 09:33:31 +0000 (Sa, 22 Sep 2012) $
// $HeadURL: https://subversion.assembla.com/svn/singular_v3/trunk/Singular/Settings/RogueSettings.cs $
// $LastChangedBy: mastergrim $
// $LastChangedDate: 2012-09-22 09:33:31 +0000 (Sa, 22 Sep 2012) $
// $LastChangedRevision: 797 $
// $Revision: 797 $

#endregion

using System.ComponentModel;
using System.IO;
using Singular.ClassSpecific.Rogue;
using Styx.Helpers;

using DefaultValue = Styx.Helpers.DefaultValueAttribute;

namespace Singular.Settings
{
    internal class RogueSettings : Styx.Helpers.Settings
    {
        public RogueSettings()
            : base(Path.Combine(SingularSettings.SettingsPath, "Rogue.xml"))
        {
        }

        [Setting]
        [DefaultValue(LethalPoisonType.Deadly)]
        [Category("Common")]
        [DisplayName("Lethal Poison")]
        [Description("Lethal Poison")]
        public LethalPoisonType LethalPoison { get; set; }

        [Setting]
        [DefaultValue(NonLethalPoisonType.Crippling)]
        [Category("Common")]
        [DisplayName("Non Lethal Poison")]
        [Description("Non Lethal Poison")]
        public NonLethalPoisonType NonLethalPoison { get; set; }


        [Setting]
        [DefaultValue(true)]
        [Category("Common")]
        [DisplayName("Interrupt Spells")]
        [Description("Interrupt Spells")]
        public bool InterruptSpells { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Common")]
        [DisplayName("Use TotT")]
        [Description("Use TotT")]
        public bool UseTricksOfTheTrade { get; set; }


        [Setting]
        [DefaultValue(true)]
        [Category("Combat Spec")]
        [DisplayName("Use Rupture Finisher")]
        [Description("Use Rupture Finisher")]
        public bool CombatUseRuptureFinisher { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Combat Spec")]
        [DisplayName("Use Expose Armor")]
        [Description("Use Expose Armor")]
        public bool UseExposeArmor { get; set; }
    }
}