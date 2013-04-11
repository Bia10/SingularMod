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
using Styx.WoWInternals.WoWObjects;

namespace Singular.ClassSpecific.Monk
{
    public class Brewmaster
    {
		private static LocalPlayer Me { get { return StyxWoW.Me; } }
		private static MonkSettings MonkSettings { get { return SingularSettings.Instance.Monk; } }

        [Behavior(BehaviorType.Pull | BehaviorType.Combat, WoWClass.Monk, WoWSpec.MonkBrewmaster, WoWContext.All)]
        public static Composite CreateBrewmasterMonkInstanceCombat()
        {
			return new PrioritySelector(
				new Decorator( ret => SingularSettings.Instance.AFKMode,
                    new PrioritySelector(
				        Safers.EnsureTarget(),
				        Movement.CreateMoveToLosBehavior(),
				        Movement.CreateFaceTargetBehavior(),
                        BaseRotation(),
				        Movement.CreateMoveToMeleeBehavior(true)
                    )
	          	),
				BaseRotation()
			);
        }

		public static Composite BaseRotation()
		{
			return new Decorator( ret => !Me.IsChanneling && !Me.Mounted,
            	new PrioritySelector(
					Helpers.Common.CreateInterruptSpellCast(ret => Me.CurrentTarget),
					//cd, cc & buff
					Spell.BuffSelf("Stance of the Sturdy Ox"),
					Spell.CastOnGround("Summon Black Ox Statue", ret => Me.CurrentTarget.Location, ret => !Me.HasAura("Sanctuary of the Ox")),
					Spell.BuffSelf("Fortifying Brew", ctx => Me.HealthPercent <= 40),
					Spell.BuffSelf("Guard", ctx => Me.HasAura("Power Guard")),
					Spell.Cast("Elusive Brew", ctx => Me.HasAura("Elusive Brew") && Me.Auras["Elusive Brew"].StackCount >= 9),
					Spell.Cast("Invoke Xuen, the White Tiger", ret => Unit.IsBoss(Me.CurrentTarget)),
					Spell.Cast("Paralysis", ret => Unit.NearbyUnfriendlyUnits.FirstOrDefault(u => u.Distance.Between(15, 20) && Me.IsFacing(u) && u.IsCasting && u != Me.CurrentTarget)),

					//rotation
					Spell.Cast("Keg Smash", ctx => Me.MaxChi - Me.CurrentChi >= 2),// &&                    Clusters.GetCluster(Me, Unit.NearbyUnfriendlyUnits, ClusterType.Radius, 8).Any(u => !u.HasAura("Weakened Blows"))),
					Spell.CastOnGround("Dizzying Haze", ctx => TankManager.Instance.NeedToTaunt.FirstOrDefault().Location , ctx => TankManager.Instance.NeedToTaunt.Any()/* && SingularSettings.Instance.Monk.DizzyTaunt*/, false),
					Spell.Cast("Rushing Jade Wind", ret => Me.CurrentChi >= 2 && Me.IsSafelyFacing(Me.CurrentTarget)),

					// AOE
					new Decorator(ctx => Unit.NearbyUnfriendlyUnits.Count(u => u.DistanceSqr <= 8 * 8) >= 3,
		            	new PrioritySelector(
							Spell.Cast("Breath of Fire", ctx => Clusters.GetCluster(Me, Unit.NearbyUnfriendlyUnits, ClusterType.Cone, 8).Count(u => u.HasAura("Dizzying Haze") && !u.HasAura("Breath of Fire")) >= 3),
							Spell.Cast("Leg Sweep", ctx => TalentManager.IsSelected((int)Common.Talents.LegSweep) && SingularSettings.Instance.Monk.AOEStun)
					)),
					
					Spell.Cast("Blackout Kick", ctx => Me.CurrentChi >= 2),
					Spell.Cast("Tiger Palm", ret => !Me.HasAura("Tiger Power")),
					Spell.BuffSelf("Purifying Brew", ctx => Me.CurrentChi >= 1 && Me.HasAura("Moderate Stagger") || Me.HasAura("Heavy Stagger")),
					
					Spell.Cast("Keg Smash", ctx => Me.CurrentChi <= 3 && Me.CurrentEnergy >= 40),
					Spell.Cast("Chi Wave"),
					
					Spell.Cast("Expel Harm", ctx => Me.HealthPercent < 90 && Me.MaxChi - Me.CurrentChi >= 1 && Unit.NearbyUnfriendlyUnits.Any(u => u.DistanceSqr <= 10 * 10)),
					Spell.Cast("Jab", ctx => Me.MaxChi - Me.CurrentChi >= 1),
					
					// filler
					Spell.Cast("Tiger Palm", ret => !SpellManager.HasSpell("Blackout Kick") || SpellManager.HasSpell("Brewmaster Training")),
					Item.UseEquippedTrinket(TrinketUsage.OnCooldownInCombat)
				)
             );
		}
    }
}