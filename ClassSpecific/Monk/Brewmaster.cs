using System;
using System.Linq;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;
using Styx;
using Styx.Common.Helpers;
using Styx.TreeSharp;
using System.Collections.Generic;
using Styx.CommonBot;
using Action = Styx.TreeSharp.Action;

namespace Singular.ClassSpecific.Monk
{
    public class Brewmaster
    {
		private static LocalPlayer Me { get { return StyxWoW.Me; } }
		private static MonkSettings MonkSettings { get { return SingularSettings.Instance.Monk; } }

        [Behavior(BehaviorType.All, WoWClass.Monk, WoWSpec.MonkBrewmaster, WoWContext.All)]
        public static Composite CreateBrewmasterMonkInstanceCombat()
        {
			return new PrioritySelector(
				new Decorator( SingularSettings.Instance.AFKMode,
				              Safers.EnsureTarget(),
				              Movement.CreateMoveToLosBehavior(),
				              Movement.CreateFaceTargetBehavior(),
				              Movement.CreateMoveToTargetBehavior(true)
	          	),
				BaseRotation()
			);
        }

		public static Composite BaseRotation()
		{
			return new Decorator( ret => !StyxWoW.Me.IsChanneling && !StyxWoW.Me.Mounted,
            	new PrioritySelector(
					Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),
					//cd, cc & buff
					Spell.BuffSelf("Stance of the Sturdy Ox"),
					Spell.CastOnGround("Summon Black Ox Statue", ret => StyxWoW.Me.CurrentTarget.Location, ret => !StyxWoW.Me.HasAura("Sanctuary of the Ox")),
					Spell.BuffSelf("Fortifying Brew", ctx => StyxWoW.Me.HealthPercent <= 40),
					Spell.BuffSelf("Guard", ctx => StyxWoW.Me.HasAura("Power Guard")),
					Spell.Cast("Elusive Brew", ctx => StyxWoW.Me.HasAura("Elusive Brew") && StyxWoW.Me.Auras["Elusive Brew"].StackCount >= 9),
					Spell.Cast("Invoke Xuen, the White Tiger", ret => Unit.IsBoss(StyxWoW.Me.CurrentTarget)),
					Spell.Cast("Paralysis", ret => Unit.NearbyUnfriendlyUnits.FirstOrDefault(u => u.Distance.Between(15, 20) && StyxWoW.Me.IsFacing(u) && u.IsCasting && u != StyxWoW.Me.CurrentTarget)),

					//rotation
					Spell.Cast("Keg Smash", ctx => StyxWoW.Me.MaxChi - StyxWoW.Me.CurrentChi >= 2),// &&                    Clusters.GetCluster(StyxWoW.Me, Unit.NearbyUnfriendlyUnits, ClusterType.Radius, 8).Any(u => !u.HasAura("Weakened Blows"))),
					Spell.CastOnGround("Dizzying Haze", ctx => TankManager.Instance.NeedToTaunt.FirstOrDefault().Location , ctx => TankManager.Instance.NeedToTaunt.Any()/* && SingularSettings.Instance.Monk.DizzyTaunt*/, false),
					Spell.Cast("Rushing Jade Wind", ret => StyxWoW.Me.CurrentChi >= 2 && StyxWoW.Me.IsSafelyFacing(StyxWoW.Me.CurrentTarget)),

					// AOE
					new Decorator(ctx => Unit.NearbyUnfriendlyUnits.Count(u => u.DistanceSqr <= 8 * 8) >= 3,
		            	new PrioritySelector(
							Spell.Cast("Breath of Fire", ctx => Clusters.GetCluster(StyxWoW.Me, Unit.NearbyUnfriendlyUnits, ClusterType.Cone, 8).Count(u => u.HasAura("Dizzying Haze") && !u.HasAura("Breath of Fire")) >= 3),
							Spell.Cast("Leg Sweep", ctx => TalentManager.IsSelected((int)Common.Talents.LegSweep) && SingularSettings.Instance.Monk.AOEStun)
					)),
					
					Spell.Cast("Blackout Kick", ctx => StyxWoW.Me.CurrentChi >= 2),
					Spell.Cast("Tiger Palm", ret => !StyxWoW.Me.HasAura("Tiger Power")),
					Spell.BuffSelf("Purifying Brew", ctx => StyxWoW.Me.CurrentChi >= 1 && StyxWoW.Me.HasAura("Moderate Stagger") || StyxWoW.Me.HasAura("Heavy Stagger")),
					
					Spell.Cast("Keg Smash", ctx => StyxWoW.Me.CurrentChi <= 3 && StyxWoW.Me.CurrentEnergy >= 40),
					Spell.Cast("Chi Wave"),
					
					Spell.Cast("Expel Harm", ctx => StyxWoW.Me.HealthPercent < 90 && StyxWoW.Me.MaxChi - StyxWoW.Me.CurrentChi >= 1 && Unit.NearbyUnfriendlyUnits.Any(u => u.DistanceSqr <= 10 * 10)),
					Spell.Cast("Jab", ctx => StyxWoW.Me.MaxChi - StyxWoW.Me.CurrentChi >= 1),
					
					// filler
					Spell.Cast("Tiger Palm", ret => !SpellManager.HasSpell("Blackout Kick") || SpellManager.HasSpell("Brewmaster Training")),
					Item.UseEquippedTrinket(TrinketUsage.OnCooldownInCombat)
				)
             );
		}
    }
}