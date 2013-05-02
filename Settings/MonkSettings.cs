#region Revision Info

// This file is part of Singular - A community driven Honorbuddy CC
// $Author: Laria $
// $Date: 2011-05-03 18:16:12 +0300 (Sal, 03 May 2011) $
// $HeadURL: http://svn.apocdev.com/singular/trunk/Singular/Settings/MonkSettings.cs $
// $LastChangedBy: Laria$
// $LastChangedDate: 2011-05-03 18:16:12 +0300 (Sal, 03 May 2011) $
// $LastChangedRevision: 307 $
// $Revision: 307 $

#endregion

using System.ComponentModel;
using System.IO;
using Singular.ClassSpecific.Rogue;
using Styx.Helpers;

namespace Singular.Settings
{
    internal class MonkSettings : Styx.Helpers.Settings
    {
        public MonkSettings()
            : base(Path.Combine(SingularSettings.SettingsPath, "Monk.xml")) { }

        #region Common

        [Setting]
        [Styx.Helpers.DefaultValue(true)]
        [Category("Spheres")]
        [DisplayName("Move to Spheres")]
        [Description("Allow moving to spheres for Chi and Health")]
        public bool MoveToSpheres { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(15)]
        [Category("Spheres")]
        [DisplayName("Max Range at Rest")]
        [Description("Max distance willing to move when resting")]
        public int SphereDistanceAtRest { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(5)]
        [Category("Spheres")]
        [DisplayName("Max Range in Combat")]
        [Description("Max distance willing to move during combat")]
        public int SphereDistanceInCombat { get; set; }


        [Setting]
        [Styx.Helpers.DefaultValue(60)]
        [Category("Common")]
        [DisplayName("Fortifying Brew Percent")]
        [Description("Fortifying Brew is used when health percent is at or below this value")]
        public float FortifyingBrewPercent { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(70)]
        [Category("Common")]
        [DisplayName("Chi Wave Percent")]
        [Description("Chi Wave is used when health percent is at or below this value")]
        public float ChiWavePercent { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(true)]
        [Category("Common")]
        [DisplayName("AOE Stun")]
        public bool AOEStun { get; set; }

		[Setting]
		[Styx.Helpers.DefaultValue(true)]
		[Category("Common")]
		[DisplayName("Paralysis")]
		public bool Paralysis { get; set; }

		[Setting]
		[Styx.Helpers.DefaultValue(20)]
		[Category("Common")]
		[DisplayName("TigerEyeStacks")]
		public int TigerEyeStacks { get; set; }

		[Setting]
		[Styx.Helpers.DefaultValue(false)]
		[Category("Common")]
		[DisplayName("PVP")]
		public bool PVP { get; set; }

        #endregion


    }
}