using System;
using System.Linq;

using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;
using Styx;

using Styx.CommonBot;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;


namespace Singular.ClassSpecific.Warlock
{
    public class Destruction
    {
        private static LocalPlayer Me { get { return StyxWoW.Me; } }
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
                Helpers.Common.CreateInterruptSpellCast(ret => Me.CurrentTarget),
                Spell.Cast("Dark Soul: Instability", ret => Me.CurrentTarget.IsBoss),
                Spell.PreventDoubleCast("Immolate", 2, ret => !Me.CurrentTarget.HasAura("Immolate")),
                new Decorator(ret => SingularSettings.Instance.Warlock.UseAOE && Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 4,
                        Spell.Cast("Fire and Brimstone", ret => CurrentBurningEmbers >= 1)
                        //Spell.CastOnGround(104232, ret => Me.CurrentTarget.Location, ret => !Me.CurrentTarget.HasAura("Rain of Fire"))
                    ),
                Spell.Cast("Havoc", ret => Unit.UnfriendlyUnitsNearTarget(8f).FirstOrDefault(u => u.Guid != Me.CurrentTarget.Guid && CurrentBurningEmbers > 1.5)),
                Spell.PreventDoubleCast("Chaos Bolt", 0.5, ret => CurrentBurningEmbers > 1 && BackdraftStacks < 3),
                Spell.CastOnGround(104232, ret => Me.CurrentTarget.Location, ret => !Me.CurrentTarget.HasAura("Rain of Fire") && SingularSettings.Instance.Warlock.UserRoF),
                Spell.PreventDoubleCast("Conflagrate", 0.5, ret => BackdraftStacks < 1),          
                Spell.PreventDoubleCast("Incinerate", 0.5, ret => !Me.HasAura("Havoc", 3)),
                Spell.Cast("Shadowburn", ret => Me.CurrentTarget.HealthPercent < 20),
                
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
        private static Double CurrentBurningEmbers { get { return (double)Me.GetPowerInfo(WoWPowerType.BurningEmbers).Current / 10; } }
    }
}
