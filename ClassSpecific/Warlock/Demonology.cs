using System;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;

using Styx.CommonBot;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;
using Styx;
using System.Linq;

namespace Singular.ClassSpecific.Warlock
{
    public class Demonology
    {
		private static LocalPlayer Me { get { return StyxWoW.Me; } }
        #region Normal Rotation

        [Behavior(BehaviorType.Pull, WoWClass.Warlock, WoWSpec.WarlockDemonology, WoWContext.All)]
        public static Composite CreateWarlockDemonologyNormalPull()
        {
            return new PrioritySelector(
                //Safers.EnsureTarget(),
                Spell.WaitForCast(true),
                //Helpers.Common.CreateAutoAttack(true),
                Spell.Cast("Corruption"),
				Spell.Cast("Hand of Gul'dan")
                );
        }

        [Behavior(BehaviorType.Combat, WoWClass.Warlock, WoWSpec.WarlockDemonology, WoWContext.All)]
        public static Composite CreateWarlockDemonologyNormalCombat()
        {
            return new PrioritySelector
            (
                //Safers.EnsureTarget(),
                Spell.WaitForCast(true),
                //Helpers.Common.CreateAutoAttack(true),
				Item.UseEquippedTrinket(TrinketUsage.OnCooldown),
				Spell.Cast("Lifeblood"),
                //Helpers.Common.CreateInterruptSpellCast(ret => Me.CurrentTarget),
                //new Decorator(ret => Me.IsMoving, new Styx.TreeSharp.Action(r => forcecast("Shadow Bolt"))),
                new Decorator(ret => Settings.SingularSettings.Instance.Warlock.UseAOE && Unit.UnfriendlyUnitsNearTarget(8f).Count() >= 2, DotCleave()),
                //new Decorator( ret => Unit.UnfriendlyUnitsNearTarget(8f).Count() <= 3, SingleTarget()),
                new Decorator(ret => Settings.SingularSettings.Instance.Warlock.UseAOE && Unit.UnfriendlyUnitsNearTarget(8f).Count() >= 8, BigAOEGroup()),
                SingleTarget()
            );
        }

        private static Composite DotCleave()
        {
            return new PrioritySelector
            (
                new Decorator(ret => !WithinMetamorphosisPhase, Spell.Cast("Corruption", ret => Unit.UnfriendlyUnitsNearTarget(8f).FirstOrDefault( u => !u.HasMyAura("Corruption"))))
            );
        }

        private static Composite BigAOEGroup()
        {
            return new PrioritySelector
            (
                Spell.Cast("Corruption", ret => !Me.CurrentTarget.HasMyAura("Corruption") && !WithinMetamorphosisPhase || !Me.CurrentTarget.HasMyAura("Doom") && WithinMetamorphosisPhase),
                Spell.Cast("Hand of Gul'dan"),
                //DotCleave(),
                Spell.Cast("Harvest Life")
            );
        }

        private static Composite SingleTarget()
        {
            return new PrioritySelector
			(
                
                new Decorator
                (
                    ret => Me.GetCurrentPower(WoWPowerType.DemonicFury) >= 950 &&
                    CorruptionTime >= 3 && (Unit.IsBoss(Me.CurrentTarget)),
                    new PrioritySelector
                    (
                        Spell.BuffSelf("Dark Soul: Knowledge"),
                        Spell.BuffSelf("Metamorphosis", ret => !WithinMetamorphosisPhase)
                    )
                ),
                // Build demonic fury
                Spell.Cast("Corruption", ret => CorruptionTime <= 3 && !WithinMetamorphosisPhase),
				Spell.Cast("Hand of Gul'dan", ret => ShadowflameTime <= 3 && !WithinMetamorphosisPhase),  
				Spell.Cast("Soul Fire", ret => Me.HasAura("Molten Core", 2)),
				Spell.Cast("Shadow Bolt", ret =>  !WithinMetamorphosisPhase),
                Spell.Cast("Felstorm", ret => Me.GotAlivePet),
                //DotCleave(),
                Spell.Cast("Grimoire: Felguard"),
                Spell.Cast("Summon Doomguard", ret => Unit.IsBoss(Me.CurrentTarget)),
                Spell.Cast("Soul Fire", ret => Me.HasAura("Molten Core") && !WithinMetamorphosisPhase && !NeedDelayCast(SoulFireTime)),
                //Maintain Doom on target
           		new Decorator
               	(
                    ret => DoomTime <= 3 && (Unit.IsBoss(Me.CurrentTarget)),
                    new PrioritySelector
                    (
                        Spell.BuffSelf("Metamorphosis", ret => !WithinMetamorphosisPhase),
                        Spell.Cast("Corruption"),
                        new Decorator(ret => Me.GetCurrentPower(WoWPowerType.DemonicFury) <= 750 && !Me.HasAura("Dark Soul: Knowledge") && Me.CurrentTarget.HealthPercent > 20, new Styx.TreeSharp.Action(r => forcecast("Metamorphosis")))
                    )
            	),

                //Metamorphosis phase
                new Decorator
                (
                    ret => WithinMetamorphosisPhase,
                    new PrioritySelector
                    (
                        Spell.Cast("Corruption", ret => DoomTime <= 4),
                        Spell.Cast("Shadow Bolt"),
                        Spell.Cast("Hand of Gul'dan", ret => Unit.UnfriendlyUnitsNearTarget(6f).Count() >= 3),
                        new Decorator(ret => Me.GetCurrentPower(WoWPowerType.DemonicFury) <= 750 && !Me.HasAura("Dark Soul: Knowledge") && Me.CurrentTarget.HealthPercent > 20, new Styx.TreeSharp.Action(r => forcecast("Metamorphosis")))
                    )
                )
            );
        }

        #endregion





        static double DoomTime
        {
			get
			{
				var c = MyAura(StyxWoW.Me.CurrentTarget, "Doom");
				return c == null ? 0 : c.TimeLeft.TotalSeconds;
			}
        }

		static double CorruptionTime
		{
			get
			{
				var c = MyAura(StyxWoW.Me.CurrentTarget, "Corruption");
				return c == null ? 0 : c.TimeLeft.TotalSeconds;
			}
		}

		static double ShadowflameTime
		{
			get
			{
				var c = MyAura(StyxWoW.Me.CurrentTarget, "Shadowflame");
				return c == null ? 0 : c.TimeLeft.TotalSeconds;
			}
		}

        private static WoWAura MyAura(WoWUnit Who, String What)
        {
            return Who.GetAllAuras().FirstOrDefault(p => p.CreatorGuid == Me.Guid && p.Name == What);
        }

        private static void forcecast(string what)
        {
            Logger.Write("Force casting " + what);
            Lua.DoString("RunMacroText(\"/cast " + what + "\")");
        }

        private static bool NeedDelayCast(TimeSpan timeToDelay)
		{
            if (dt==DateTime.MinValue)
            {
                //Logger.Write("Init dt");
                dt = DateTime.Now;
            }

			TimeSpan sp = DateTime.Now - dt;
            //Logger.Write("SP " + sp);
            if (sp >= timeToDelay) 
			{
                //Logger.Write("No delay");
                dt = DateTime.Now;
				return false;
			}
            return true;
		}

		static TimeSpan SoulFireTime = new TimeSpan(0, 0, 0, 2);
        static TimeSpan HandOfGuldanTime = new TimeSpan(0, 0, 0, 0, 800);
        static DateTime dt = DateTime.MinValue;
		static bool WithinDarkSoulPhase { get { return Me.HasAura("Dark Soul: Knowledge"); } }
		static bool WithinMetamorphosisPhase { get { return Me.HasAura("Metamorphosis"); } }
    }
}