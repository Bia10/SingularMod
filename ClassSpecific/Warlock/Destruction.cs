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

        [Behavior(BehaviorType.All, WoWClass.Warlock, WoWSpec.WarlockDestruction, WoWContext.All)]
        public static Composite CreateWarlockDestructionCombat()
        {
            return new PrioritySelector(
                new Decorator(ret => SingularSettings.Instance.AFKMode,
				    new PrioritySelector( 
					    Safers.EnsureTarget(),
					    Movement.CreateMoveToLosBehavior(),
					    Movement.CreateFaceTargetBehavior(),
	                    Movement.CreateMoveToTargetBehavior(true, 35f)
				    )
                ),
				BaseRotation()
			);
        }

		public static Composite BaseRotation()
		{
            return new Decorator( ret => !Spell.IsGlobalCooldown() && !StyxWoW.Me.Mounted,
			    new PrioritySelector(
				    Spell.WaitForCast(true),
				    Helpers.Common.CreateInterruptSpellCast(ret => Me.CurrentTarget),
				    Spell.Cast("Dark Soul: Instability", ret => Me.CurrentTarget.IsBoss),
				    Spell.PreventDoubleCast("Immolate", 2, ret => !Me.CurrentTarget.HasAura("Immolate")),
				    new Decorator(ret => SingularSettings.Instance.Warlock.UseAOE && Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 6,
                        new PrioritySelector(
			                  Spell.Cast("Fire and Brimstone", ret => CurrentBurningEmbers >= 1),
			                  Spell.PreventDoubleCast("Immolate", 2, ret => !Me.CurrentTarget.HasAura("Immolate"))
			                  //Spell.CastOnGround(104232, ret => Me.CurrentTarget.Location, ret => !Me.CurrentTarget.HasAura("Rain of Fire"))
			                  )),
				    Spell.Cast("Havoc", ret => Unit.UnfriendlyUnitsNearTarget(8f).FirstOrDefault(u => u.Guid != Me.CurrentTarget.Guid && CurrentBurningEmbers >= 1)),
				    Spell.PreventDoubleCast("Chaos Bolt", 1, ret => CurrentBurningEmbers >= 1 && BackdraftStacks < 3 && Me.CurrentTarget.HasAura("Immolate") && Me.CurrentTarget.HealthPercent > 20),
				    Spell.CastOnGround(104232, ret => Me.CurrentTarget.Location, ret => !Me.CurrentTarget.HasAura("Rain of Fire") && Me.CurrentTarget.Distance.Between(1, 35) && SingularSettings.Instance.Warlock.UserRoF),
				    Spell.PreventDoubleCast("Conflagrate", 1, ret => BackdraftStacks < 1 && Me.CurrentTarget.HasAura("Immolate")),
				    Spell.PreventDoubleCast("Incinerate", 1, ret => !Me.HasAura("Havoc", 3) && Me.CurrentTarget.HasAura("Immolate")),
				    Spell.Cast("Shadowburn", ret => Me.CurrentTarget.HealthPercent < 20)
                )
			);
		}

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

        private static Double CurrentBurningEmbers { get { return (double)Me.GetPowerInfo(WoWPowerType.BurningEmbers).Current / 10; } }
    }
}
