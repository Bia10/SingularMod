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

        [Behavior(BehaviorType.Pull | BehaviorType.Combat, WoWClass.Monk, WoWSpec.MonkWindwalker, WoWContext.All)]
        public static Composite CreateWindwalkerMonkCombat()
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
				new Decorator(ret => MonkSettings.PVP,
		              new PrioritySelector(PVPRotation())
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
						//CD & defense
                    Spell.Cast("Invoke Xuen, the White Tiger", ret => Me.CurrentTarget.IsBoss),
                    Spell.Cast("Tigereye Brew", ret => Me.HasAura("Tigereye Brew", MonkSettings.TigerEyeStacks)),
					Spell.Cast("Energizing Brew", ret => Me.CurrentEnergy < 30),
					Spell.Cast("Fortifying Brew", ret => Me.HealthPercent <= 35),
					Spell.Cast("Touch of Karma", ret => Me.HealthPercent <= 45),

					//dps rotation
					Spell.Cast("Touch of Death", ret => Me.HasAura("Death Note")),
					//Spell.Cast("Expel Harm", ret => Me.CurrentEnergy >= 40 && Me.HealthPercent <= 85),
					Spell.Cast("Chi Wave"),
					Spell.Cast("Tiger Palm", ret => Me.CurrentChi > 0 && (!Me.HasAura("Tiger Power") || Me.GetAuraTimeLeft("Tiger Power", true).TotalSeconds < 3) || Me.HasAura("Combo Breaker: Tiger Palm")),						
					Spell.Cast("Rising Sun Kick", ret => Me.CurrentChi >= 2),
                    Spell.Cast("Fists of Fury", ret => Me.CurrentEnergy <= 30 && Spell.GetSpellCooldown("Rising Sun Kick").Seconds >= 3 && !Me.HasAura("Combo Breaker: Blackout Kick") && !Me.IsMoving && Me.HasAura("Tiger Power") && Me.CurrentChi >= 3 && Me.CurrentTarget.HasAura("Rising Sun Kick")),
					
					new Decorator( ret => !Spell.IsGlobalCooldown() && Unit.NearbyUnfriendlyUnits.Count(u => u.Distance <= 8) >= SingularSettings.Instance.AOENumber,
					 new PrioritySelector
					 (
                        Spell.Cast("Rising Sun Kick", ret => Me.CurrentChi >= 2),
						Spell.Cast("Spinning Crane Kick", ret => Me.CurrentEnergy >= 40)
						)
					 ),

					Spell.Cast("Blackout Kick", ret => Spell.GetSpellCooldown("Rising Sun Kick").Seconds >= 1 && Me.CurrentChi >= 2 && Me.HasAura("Tiger Power") || Spell.GetSpellCooldown("Rising Sun Kick").Seconds >= 1 && Me.HasAura("Combo Breaker: Blackout Kick") && Me.HasAura("Tiger Power")),
					
					Spell.Cast("Jab", ret => Me.CurrentChi <= 2 && Me.CurrentEnergy >= 40)
					)
				);
		}

		public static Composite PVPRotation()
		{
			return new Decorator(
				ret => !Spell.IsGlobalCooldown() && !StyxWoW.Me.Mounted,
				new PrioritySelector(
				Spell.WaitForCast(true),
				//cc & interrupt stuff
				Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),
				Spell.Cast("Paralysis", ret => Unit.NearbyUnFriendlyPlayers.FirstOrDefault(u => u.Distance.Between(8, 20) && Me.IsFacing(u) && u.Guid != Me.CurrentTarget.Guid && MonkSettings.Paralysis)),
				Spell.Cast("Quaking Palm", ret => Unit.NearbyUnFriendlyPlayers.FirstOrDefault(u => u.IsWithinMeleeRange && Me.IsFacing(u) && !u.HasAura("Paralysis") && u != Me.CurrentTarget)),
				Spell.Cast("Spear Hand Strike", ret => Unit.NearbyUnFriendlyPlayers.FirstOrDefault(u => u.IsWithinMeleeRange && Me.IsFacing(u) && u.IsCastingHealingSpell)),
				
				//CD & defense
				Spell.Cast("Invoke Xuen, the White Tiger", ret => Me.CurrentTarget.IsPlayer),
				Spell.Cast("Tigereye Brew", ret => Me.HasAura("Tigereye Brew", 10)),
				Spell.Cast("Energizing Brew", ret => Me.CurrentEnergy < 30),
				Spell.Cast("Fortifying Brew", ret => Me.HealthPercent <= 35),
				Spell.Cast("Touch of Karma", ret => Me.CurrentTarget.IsPlayer && Me.HealthPercent <= 70),
				
				//drop healing sphere
				Spell.CastOnGround("Healing Sphere", ret => Me.Location, ret => Me.CurrentEnergy >= 40 && Me.HealthPercent <= 30 && MonkSettings.MoveToSpheres),
				//Spell.CastOnGround("Healing Sphere", ret => Unit.NearbyFriendlyPlayers.FirstOrDefault(u => u.Guid != Me.Guid && u.HealthPercent <= 50).Location, ret => Me.CurrentEnergy >= 40 && MonkSettings.MoveToSpheres),
				
				//dps rotation
				Spell.Cast("Touch of Death", ret => Me.CurrentTarget.IsPlayer && Me.CurrentTarget.HealthPercent < 10),
				Spell.Cast("Expel Harm", ret => Me.CurrentEnergy >= 40 && Me.HealthPercent <= 85),
				Spell.Cast("Chi Wave", ret => Me.CurrentTarget.IsPlayer && Me.HealthPercent <= 80),
				//Spell.Cast("Dampen Harm"),
				Spell.Cast("Spinning Fire Blossom", ret => Me.CurrentTarget.IsPlayer && Me.CurrentTarget.Distance > 15 && Me.IsSafelyFacing(Me.CurrentTarget)),
				Spell.Cast("Disable", ret => Me.CurrentTarget.IsPlayer && !Me.CurrentTarget.HasMyAura("Disable")),
				Spell.Cast("Leg Sweep", ret => Me.CurrentTarget.IsWithinMeleeRange && Me.CurrentTarget.IsPlayer),
				Spell.Cast("Ring of Peace", ret => Me.CurrentTarget.IsPlayer && Me.CurrentTarget.IsWithinMeleeRange),
				Spell.Cast("Grapple Weapon", ret => Me.CurrentEnergy >= 20 && Me.CurrentTarget.IsPlayer),
				Spell.Cast("Tiger Palm", ret => Me.CurrentChi > 0 && (!Me.HasAura("Tiger Power") || Me.GetAuraTimeLeft("Tiger Power", true).TotalSeconds < 4) || Me.HasAura("Combo Breaker: Tiger Palm")),						
				Spell.Cast("Rising Sun Kick", ret => Me.CurrentChi >= 2 && Spell.GetSpellCooldown("Rising Sun Kick").Seconds == 0),
				Spell.Cast("Fists of Fury", ret => Me.CurrentEnergy <= 60 && Spell.GetSpellCooldown("Rising Sun Kick").Seconds >= 1 && !Me.HasAura("Combo Breaker: Blackout Kick") && !Me.IsMoving && Me.HasAura("Tiger Power") && Me.CurrentChi >= 3 && Me.CurrentTarget.HasAura("Rising Sun Kick")),
				Spell.Cast("Blackout Kick", ret => Spell.GetSpellCooldown("Rising Sun Kick").Seconds >= 1 && Me.CurrentChi >= 2 && Me.HasAura("Tiger Power") || Spell.GetSpellCooldown("Rising Sun Kick").Seconds >= 1 && Me.HasAura("Combo Breaker: Blackout Kick") && Me.HasAura("Tiger Power")),
				
				Spell.Cast("Jab", ret => Me.CurrentChi <= 2 && Me.CurrentEnergy >= 40)
				)
				);
		}
    }
}