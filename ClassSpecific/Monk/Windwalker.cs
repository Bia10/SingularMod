using System;
using System.Linq;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Styx;
using Styx.CommonBot;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;
using Rest = Singular.Helpers.Rest;
using Singular.Settings;
using Styx.WoWInternals;

//Storm, Earth, and Fire (137639)

namespace Singular.ClassSpecific.Monk
{
    public class Windwalker
    {
        private static LocalPlayer Me { get { return StyxWoW.Me; } }
        private static MonkSettings MonkSettings { get { return SingularSettings.Instance.Monk; } }

        [Behavior(BehaviorType.All, WoWClass.Monk, WoWSpec.MonkWindwalker, WoWContext.All)]
        public static Composite CreateWindwalkerMonkCombat()
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
			return new Decorator(
				ret => !Spell.IsGlobalCooldown() && !StyxWoW.Me.Mounted,
				new PrioritySelector(
					Spell.WaitForCast(true),
					//cc & interrupt stuff
					Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),
					Spell.Cast("Paralysis", ret => Unit.NearbyUnFriendlyPlayers.FirstOrDefault(u => u.Distance.Between(8, 20) && Me.IsFacing(u) && u != Me.CurrentTarget && MonkSettings.Paralysis)),
					Spell.Cast("Quaking Palm", ret => Unit.NearbyUnFriendlyPlayers.FirstOrDefault(u => u.IsWithinMeleeRange && Me.IsFacing(u) && !u.HasAura("Paralysis") && u != Me.CurrentTarget)),
					Spell.Cast("Spear Hand Strike", ret => Unit.NearbyUnFriendlyPlayers.FirstOrDefault(u => u.IsWithinMeleeRange && Me.IsFacing(u) && u.IsCastingHealingSpell)),

					//CD & defense
					Spell.Cast("Invoke Xuen, the White Tiger", ret => Me.CurrentTarget.IsPlayer || Me.CurrentTarget.IsBoss),
					Spell.Cast("Tigereye Brew", ret => Me.HasAura("Tigereye Brew", 10) || Me.HealthPercent <= 40 && Me.HasAura("Tigereye Brew") && Me.HasAura("Healing Elixirs")),
					Spell.Cast("Energizing Brew", ret => Me.CurrentEnergy < 40),
					Spell.Cast("Fortifying Brew", ret => Me.HealthPercent <= 35),
					Spell.Cast("Touch of Karma", ret => !Me.CurrentTarget.IsBoss && Me.HealthPercent <= 75 || Me.CurrentTarget.IsBoss && Me.HealthPercent <= 50),

					//dps rotation
					Spell.Cast("Touch of Death", ret => Me.HasAura("Death Note") || Me.CurrentTarget.IsPlayer && Me.CurrentTarget.HealthPercent < 10),
					Spell.Cast("Expel Harm", ret => Me.CurrentEnergy >= 40 && Me.HealthPercent <= 85),
					Spell.Cast("Chi Wave", ret => !Me.CurrentTarget.IsPlayer || Me.CurrentTarget.IsPlayer && Me.HealthPercent <= 75),
					//Spell.Cast("Dampen Harm"),
					Spell.Cast("Spinning Fire Blossom", ret => !Me.CurrentTarget.IsBoss && Me.CurrentTarget.Distance > 10 && Me.IsSafelyFacing(Me.CurrentTarget)),
					Spell.Cast("Disable", ret => !Me.CurrentTarget.HasMyAura("Disable")),
					Spell.Cast("Leg Sweep", ret => Me.CurrentTarget.IsWithinMeleeRange && MonkSettings.AOEStun),
					Spell.Cast("Ring of Peace", ret => Me.CurrentTarget.IsPlayer && Me.CurrentTarget.IsWithinMeleeRange),
					Spell.Cast("Grapple Weapon", ret => Me.CurrentEnergy >= 20 && Me.CurrentTarget.IsPlayer && !Me.HasAura("Ring of Peace")),
					Spell.Cast("Tiger Palm", ret => Me.CurrentChi > 0 && (!Me.HasAura("Tiger Power") || Me.GetAuraTimeLeft("Tiger Power", true).TotalSeconds < 4) || Me.HasAura("Combo Breaker: Tiger Palm")),						
					Spell.Cast("Rising Sun Kick", ret => Me.CurrentChi >= 2 && Spell.GetSpellCooldown("Rising Sun Kick").Seconds == 0),
					Spell.Cast("Fists of Fury", ret => Me.CurrentEnergy <= 60 && Spell.GetSpellCooldown("Rising Sun Kick").Seconds >= 2 && !Me.HasAura("Combo Breaker: Blackout Kick") && !Me.IsMoving && Me.HasAura("Tiger Power") && Me.CurrentChi >= 3),
					
					new Decorator( ret => !Spell.IsGlobalCooldown() && Unit.NearbyUnfriendlyUnits.Count(u => u.Distance <= 8) >= 4 && MonkSettings.UseAOE,
					 new PrioritySelector
					 (
						Spell.Cast("Expel Harm", ret => Me.CurrentEnergy >= 40 && Spell.GetSpellCooldown("Rising Sun Kick").Seconds == 0),
						Spell.Cast("Spinning Crane Kick", ret => Me.CurrentEnergy >= 40)
						)
					 ),

					Spell.Cast("Blackout Kick", ret => Spell.GetSpellCooldown("Rising Sun Kick").Seconds >= 2 && Me.CurrentChi >= 2 && Me.HasAura("Tiger Power") || Spell.GetSpellCooldown("Rising Sun Kick").Seconds >= 2 && Me.HasAura("Combo Breaker: Blackout Kick") && Me.HasAura("Tiger Power")),
					
					Spell.Cast("Jab", ret => Me.CurrentChi <= 2 && Me.CurrentEnergy >= 40)
					)
				);
		}
    }
}