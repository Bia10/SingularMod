#region Revision Info

// This file is part of Singular - A community driven Honorbuddy CC
// $Author: Nesox $
// $Date: 2012-09-07 21:55:59 +0000 (Fr, 07 Sep 2012) $
// $HeadURL: https://subversion.assembla.com/svn/singular_v3/trunk/Singular/Settings/HunterSettings.cs $
// $LastChangedBy: Nesox $
// $LastChangedDate: 2012-09-07 21:55:59 +0000 (Fr, 07 Sep 2012) $
// $LastChangedRevision: 684 $
// $Revision: 684 $

#endregion

using System;
using System.ComponentModel;
using System.IO;
using Styx.Helpers;
using Styx.WoWInternals.WoWObjects;

using DefaultValue = Styx.Helpers.DefaultValueAttribute;

namespace Singular.Settings
{
    internal class HunterSettings : Styx.Helpers.Settings
    {
        public HunterSettings()
            : base(Path.Combine(SingularSettings.SettingsPath, "Hunter.xml"))
        {
        }

        #region Category: Pet

        [Setting]
        [DefaultValue("1")]
        [Category("Pet")]
        [DisplayName("Pet Slot")]
        public string PetSlot { get; set; }

        [Setting]
        [DefaultValue(70)]
        [Category("Pet")]
        [DisplayName("Mend Pet Percent")]
        public double MendPetPercent { get; set; }

        #endregion

        #region Category: Common

        [Setting]
        [DefaultValue(false)]
        [Category("Common")]
        [DisplayName("Use Disengage")]
        [Description("Will be used in battlegrounds no matter what this is set")]
        public bool UseDisengage { get; set; }

        #endregion
    }
}