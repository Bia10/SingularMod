using System;
using System.Linq;

using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;

using Styx;

using Styx.CommonBot;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;


namespace Singular.ClassSpecific.Warlock
{
    public class Destruction
    {


        #region Normal Rotation

        [Behavior(BehaviorType.Pull, WoWClass.Warlock, WoWSpec.WarlockDestruction, WoWContext.All)]
        public static Composite CreateWarlockDestructionNormalPull()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Spell.WaitForCast(true),
                Helpers.Common.CreateAutoAttack(true),
                //Spell.Cast("Soul Fire"),
                Spell.Buff("Curse of the Elements", true),
                Movement.CreateMoveToTargetBehavior(true, 35f)
                );
        }
        [Behavior(BehaviorType.Combat, WoWClass.Warlock, WoWSpec.WarlockDestruction, WoWContext.All)]
        public static Composite CreateWarlockDestructionNormalCombat()
        {
            return new PrioritySelector(
                //Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Spell.WaitForCast(true),
                //Helpers.Common.CreateAutoAttack(true),
                Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),
                //new Decorator(ret => Settings.SingularSettings.Instance.Warlock.UseAOE && Unit.UnfriendlyUnitsNearTarget(8f).Count() >= 2, DotCleave()),
                new Decorator(ret => Settings.SingularSettings.Instance.Warlock.UseAOE && Unit.UnfriendlyUnitsNearTarget(8f).Count() >= 5, BigAOEGroup()),
                Spell.Cast("Dark Soul: Instability", ret => StyxWoW.Me.CurrentTarget.IsBoss),
                Spell.Cast("Immolate", ret => !StyxWoW.Me.CurrentTarget.HasAura("Immolate") && !NeedDelayCast(Immolate)),
                Spell.CastOnGround("Rain of Fire", ret => StyxWoW.Me.CurrentTarget.Location),
                Spell.Cast("Conflagrate", ret => BackdraftStacks < 1 && StyxWoW.Me.GetCurrentPower(WoWPowerType.BurningEmbers) < Convert.ToInt32(1) * 10),
                Spell.Cast("Chaos Bolt",ret => BackdraftStacks < 3 || StyxWoW.Me.HasAura("Havoc")),

                Spell.Cast("Incinerate", ret => BackdraftStacks >= 1 && StyxWoW.Me.GetCurrentPower(WoWPowerType.BurningEmbers) < Convert.ToInt32(1) * 10 || !SpellManager.CanCast("Chaos Bolt") && !StyxWoW.Me.HasAura("Havoc") && StyxWoW.Me.GetCurrentPower(WoWPowerType.BurningEmbers) < Convert.ToInt32(1) * 10),
                Spell.Cast("Shadowburn", ret => StyxWoW.Me.CurrentTarget.HealthPercent < 20),
                Spell.Cast("Havoc", ret => Unit.UnfriendlyUnitsNearTarget(8f).First(u => u != StyxWoW.Me.CurrentTarget && StyxWoW.Me.GetCurrentPower(WoWPowerType.BurningEmbers) >= Convert.ToInt32(1) * 10)),
                Movement.CreateMoveToTargetBehavior(true, 35f)
                );
        }

        private static Composite DotCleave()
        {
            return new PrioritySelector
            (
                Spell.Cast("Immolate", ret => Unit.UnfriendlyUnitsNearTarget(8f).FirstOrDefault(u => !u.HasMyAura("Immolate"))),
                Spell.Cast("Havoc", ret => Unit.UnfriendlyUnitsNearTarget(8f).First(u => u != StyxWoW.Me.CurrentTarget && SpellManager.CanCast("Chaos Bolt")))
            );
        }

        private static Composite BigAOEGroup()
        {
            return new PrioritySelector
            (
                Spell.CastOnGround("Rain of Fire", ret => StyxWoW.Me.CurrentTarget.Location, ret => !NeedDelayCast(RainOfFire)),
                Spell.Cast("Fire and Brimstone")
            );
        }

        #endregion
        static double BackdraftStacks
        {
            get
            {
                var c = MyAura(StyxWoW.Me, "Backdraft");
                return c == null ? 0 : c.StackCount;
            }
        }



        private static WoWAura MyAura(WoWUnit Who, String What)
        {
            return Who.GetAllAuras().FirstOrDefault(p => p.CreatorGuid == StyxWoW.Me.Guid && p.Name == What);
        }

        private static bool NeedDelayCast(TimeSpan timeToDelay)
        {
            if (dt == DateTime.MinValue)
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
        static DateTime dt = DateTime.MinValue;
        static TimeSpan RainOfFire = new TimeSpan(0, 0, 0, 8);
        static TimeSpan Immolate = new TimeSpan(0, 0, 0, 2);
    }
}
