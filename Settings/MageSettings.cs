#region Revision Info

// This file is part of Singular - A community driven Honorbuddy CC
// $Author: Nesox $
// $Date: 2012-09-07 21:55:59 +0000 (Fr, 07 Sep 2012) $
// $HeadURL: https://subversion.assembla.com/svn/singular_v3/trunk/Singular/Settings/MageSettings.cs $
// $LastChangedBy: Nesox $
// $LastChangedDate: 2012-09-07 21:55:59 +0000 (Fr, 07 Sep 2012) $
// $LastChangedRevision: 684 $
// $Revision: 684 $

#endregion

using System.ComponentModel;
using System.IO;
using Styx.Helpers;

namespace Singular.Settings
{
    internal class MageSettings : Styx.Helpers.Settings
    {
        public MageSettings()
            : base(Path.Combine(SingularSettings.SettingsPath, "Mage.xml"))
        {

        }
        #region Category: Common

        [SettingAttribute]
        [Styx.Helpers.DefaultValue(true)]
        [Category("Common")]
        [DisplayName("Summon Table If In A Party")]
        [Description("Summons a food table instead of using conjured food if in a party")]
        public bool SummonTableIfInParty { get; set; }

        #endregion
    }
}