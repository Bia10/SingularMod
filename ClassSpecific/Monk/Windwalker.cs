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

        #region INSTANCES
        [Behavior(BehaviorType.Pull | BehaviorType.Combat, WoWClass.Monk, WoWSpec.MonkWindwalker, WoWContext.Instances)]
        public static Composite CreateWindwalkerMonkCombatInstances()
        {
            return new PrioritySelector(
                //Safers.EnsureTarget(),
                //Helpers.Common.CreateAutoAttack(true),

                Spell.WaitForCast(true),

                new Decorator(
                    ret => !Spell.IsGlobalCooldown() && !StyxWoW.Me.Mounted,
                    new PrioritySelector(

                        Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),
                        //Spell.Cast("Nimble Brew", ret => Me.IsCrowdControlled()),
                        Spell.Cast("Invoke Xuen, the White Tiger", ret => Me.CurrentTarget.IsBoss),
                        //Item.UseEquippedTrinket(TrinketUsage.OnCooldownInCombat),
                        Spell.Cast("Tigereye Brew", ret => Me.HasAura("Tigereye Brew", 10) || Me.HealthPercent <= 45 && Me.HasAura("Healing Exlixirs") && Me.HasAura("Tigereye Brew")),
                        Spell.Cast("Energizing Brew", ret => Me.CurrentEnergy < 40),
                        Spell.Cast("Fortifying Brew", ret => Me.HealthPercent <= 50),
                        Spell.Cast("Touch of Death", ret => TalentManager.HasGlyph("Touch of Death") && Me.HasAura("Death Note")),
                        Spell.Cast("Touch of Karma", ret => Me.HealthPercent <= 45),
                        Spell.Cast("Tiger Palm",
                            ret => Me.CurrentChi > 0
                                && (!Me.HasAura("Tiger Power") || Me.GetAuraTimeLeft("Tiger Power", true).TotalSeconds < 4) || Me.HasAura("Combo Breaker: Tiger Palm")), 

                        new Decorator
                            ( ret => !Spell.IsGlobalCooldown() && Unit.NearbyUnfriendlyUnits.Count( u => u.Distance <= 8) >= 4 && SingularSettings.Instance.Monk.UseAOE,
                            new PrioritySelector
                                (
                                Spell.Cast("Rising Sun Kick", ret => Me.CurrentChi >= 2 && Spell.GetSpellCooldown("Rising Sun Kick").Seconds == 0),
                                Spell.Cast("Expel Harm", ret => Me.CurrentEnergy >= 40 && Spell.GetSpellCooldown("Rising Sun Kick").Seconds == 0),
                                Spell.Cast("Fists of Fury", 
                                    ret => Spell.GetSpellCooldown("Rising Sun Kick").Seconds >= 2 && !Me.IsMoving && Me.GetAuraTimeLeft("Tiger Power", true).Seconds > 3 && Me.CurrentChi >= 3 && Me.CurrentEnergy <= 40),
                                Spell.Cast("Spinning Crane Kick", ret => Me.CurrentEnergy >= 40)
                                )
                            ),

                        Spell.Cast("Rising Sun Kick", ret => Me.CurrentChi >= 2 && Spell.GetSpellCooldown("Rising Sun Kick").Seconds == 0),

                        Spell.Cast("Fists of Fury",
                            ret => Spell.GetSpellCooldown("Rising Sun Kick").Seconds >= 2 && !Me.HasAura("Combo Breaker: Blackout Kick") && !Me.IsMoving && Me.GetAuraTimeLeft("Tiger Power", true).Seconds > 3 && Me.CurrentChi >= 3),


                        Spell.Cast("Blackout Kick", ret => Spell.GetSpellCooldown("Rising Sun Kick").Seconds >= 2 && Me.CurrentChi >= 2 && Me.HasAura("Tiger Power") || Spell.GetSpellCooldown("Rising Sun Kick").Seconds >= 2 && Me.HasAura("Combo Breaker: Blackout Kick") && Me.HasAura("Tiger Power")),
                        Spell.Cast("Chi Wave"),
                        Spell.Cast("Jab", ret => Me.CurrentChi <=2 && Me.CurrentEnergy >= 40)
                        //Spell.Cast("Spinning Fire Blossom", ret => Me.CurrentTarget.Distance > 10 && Me.IsFacing(Me.CurrentTarget))
                        )
                    )
                );
        }

        #endregion

        #region NORMAL
        [Behavior(BehaviorType.Pull | BehaviorType.Combat, WoWClass.Monk, WoWSpec.MonkWindwalker, WoWContext.Normal)]
        public static Composite CreateWindwalkerMonkCombatNormal()
        {
            return new PrioritySelector(
                //Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Helpers.Common.CreateAutoAttack(true),

                Spell.WaitForCast(true),

                new Decorator(
                    ret => !Spell.IsGlobalCooldown() && !StyxWoW.Me.Mounted,
                    new PrioritySelector(

                        Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),
                        new Decorator(ret => Me.IsCrowdControlled() && !SpellManager.CanCast("Nimble Brew"), 
                                Item.UseEquippedTrinket(TrinketUsage.CrowdControlled)
                            ),
                        Spell.Cast("Nimble Brew", ret => Me.IsCrowdControlled()),
                        Spell.Cast("Paralysis", ret => Unit.NearbyUnFriendlyPlayers.FirstOrDefault(u => u.Distance.Between(8, 20) && Me.IsFacing(u) && u != Me.CurrentTarget)),
                        Spell.Cast("Quaking Palm", ret => Unit.NearbyUnFriendlyPlayers.FirstOrDefault(u => u.IsWithinMeleeRange && Me.IsFacing(u) && !u.HasAura("Paralysis") && u != Me.CurrentTarget)),
                        Spell.Cast("Spear Hand Strike", ret => Unit.NearbyUnFriendlyPlayers.FirstOrDefault(u => u.IsWithinMeleeRange && Me.IsFacing(u) && u.IsCastingHealingSpell)),
                        Spell.Cast("Invoke Xuen, the White Tiger", ret => Me.CurrentTarget.IsPlayer),
                        Spell.Cast("Tigereye Brew", ret => Me.HasAura("Tigereye Brew", 10)),
                        Spell.Cast("Energizing Brew", ret => Me.CurrentEnergy < 40),
                        Spell.Cast("Fortifying Brew", ret => Me.HealthPercent <= 50),
                        Spell.Cast("Touch of Karma", ret => Me.HealthPercent <= 75),
                        Spell.Cast("Touch of Death", ret => TalentManager.HasGlyph("Touch of Death") && Me.HasAura("Death Note")),
                        Spell.Cast("Expel Harm", ret => Me.CurrentEnergy >= 40 && Me.HealthPercent <= 85),
                        Spell.Cast("Chi Wave", ret => Me.HealthPercent <= 90),
                        //Spell.Cast("Dampen Harm"),
                        Spell.Cast("Spinning Fire Blossom", ret => Me.CurrentTarget.Distance > 10 && Me.IsSafelyFacing(Me.CurrentTarget)),
                        Spell.Cast("Disable", ret => !Me.CurrentTarget.HasMyAura("Disable") && Me.CurrentTarget.IsPlayer),
                        Spell.Cast("Grapple Weapon", ret => Me.CurrentEnergy >= 20 && Me.CurrentTarget.IsPlayer),
                        Spell.Cast("Leg Sweep", ret => Me.CurrentTarget.IsWithinMeleeRange),
                        Spell.Cast("Tiger Palm",
                            ret => Me.CurrentChi > 0
                                && (!Me.HasAura("Tiger Power") || Me.GetAuraTimeLeft("Tiger Power", true).TotalSeconds < 4) || Me.HasAura("Combo Breaker: Tiger Palm")), 

                        new Decorator
                            (ret => !Spell.IsGlobalCooldown() && Unit.NearbyUnfriendlyUnits.Count(u => u.Distance <= 8) >= 4 && SingularSettings.Instance.Monk.UseAOE,
                            new PrioritySelector
                                (
                                Spell.Cast("Rising Sun Kick", ret => Me.CurrentChi >= 2 && Spell.GetSpellCooldown("Rising Sun Kick").Seconds == 0),
                                Spell.Cast("Expel Harm", ret => Me.CurrentEnergy >= 40 && Spell.GetSpellCooldown("Rising Sun Kick").Seconds == 0),
                                Spell.Cast("Fists of Fury", 
                                    ret => Spell.GetSpellCooldown("Rising Sun Kick").Seconds >= 2 && !Me.IsMoving && Me.HasAura("Tiger Power") && Me.CurrentChi >= 3),
                                Spell.Cast("Spinning Crane Kick", ret => Me.CurrentEnergy >= 40)
                                )
                            ),

                        Spell.Cast("Rising Sun Kick", ret => Me.CurrentChi >= 2 && Spell.GetSpellCooldown("Rising Sun Kick").Seconds == 0),

                        Spell.Cast("Fists of Fury", 
                            ret => Spell.GetSpellCooldown("Rising Sun Kick").Seconds >= 2 && !Me.HasAura("Combo Breaker: Blackout Kick") && !Me.IsMoving && Me.HasAura("Tiger Power") && Me.CurrentChi >= 3),


                        Spell.Cast("Blackout Kick", ret => Spell.GetSpellCooldown("Rising Sun Kick").Seconds >= 2 && Me.CurrentChi >= 2 && Me.HasAura("Tiger Power") || Spell.GetSpellCooldown("Rising Sun Kick").Seconds >= 2 && Me.HasAura("Combo Breaker: Blackout Kick") && Me.HasAura("Tiger Power")),

                        Spell.Cast("Jab", ret => Me.CurrentChi <=2 && Me.CurrentEnergy >= 40)
                        )
                    ),

                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        #endregion

        #region BATTLEGROUNDS
        [Behavior(BehaviorType.Pull | BehaviorType.Combat, WoWClass.Monk, WoWSpec.MonkWindwalker, WoWContext.Battlegrounds)]
        public static Composite CreateWindwalkerMonkCombatBattlegrounds()
        {
            return new PrioritySelector(
                //Safers.EnsureTarget(),
                //Movement.CreateMoveToLosBehavior(),
                //Movement.CreateFaceTargetBehavior(),
                Helpers.Common.CreateAutoAttack(true),
                Spell.WaitForCast(true),

                new Decorator(
                    ret => !Spell.IsGlobalCooldown() && !StyxWoW.Me.Mounted,
                    new PrioritySelector(

                        Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),
                        new Decorator(ret => Me.IsCrowdControlled() && !SpellManager.CanCast("Nimble Brew"),
                                Item.UseEquippedTrinket(TrinketUsage.CrowdControlled)
                            ),
                        Spell.Cast("Nimble Brew", ret => Me.IsCrowdControlled()),
                        Spell.Cast("Paralysis", ret => Unit.NearbyUnFriendlyPlayers.FirstOrDefault(u => u.Distance.Between(8, 20) && Me.IsFacing(u) && u != Me.CurrentTarget)),
                        Spell.Cast("Quaking Palm", ret => Unit.NearbyUnFriendlyPlayers.FirstOrDefault(u => u.IsWithinMeleeRange && Me.IsFacing(u) && !u.HasAura("Paralysis") && u != Me.CurrentTarget)),
                        Spell.Cast("Spear Hand Strike", ret => Unit.NearbyUnFriendlyPlayers.FirstOrDefault(u => u.IsWithinMeleeRange && Me.IsFacing(u) && u.IsCastingHealingSpell)),
                        Spell.Cast("Invoke Xuen, the White Tiger", ret => Me.CurrentTarget.IsPlayer),
                        Spell.Cast("Tigereye Brew", ret => Me.HasAura("Tigereye Brew", 10)),
                        Spell.Cast("Energizing Brew", ret => Me.CurrentEnergy < 40),
                        Spell.Cast("Fortifying Brew", ret => Me.HealthPercent <= 50),
                        Spell.Cast("Touch of Karma", ret => Me.HealthPercent <= 75),
                        Spell.Cast("Touch of Death", ret => TalentManager.HasGlyph("Touch of Death") && Me.HasAura("Death Note")),
                        Spell.Cast("Expel Harm", ret => Me.CurrentEnergy >= 40 && Me.HealthPercent <= 85),
                        Spell.Cast("Chi Wave"),
                        Spell.Cast("Dampen Harm"),
                        Spell.Cast("Spinning Fire Blossom", ret => Me.CurrentTarget.Distance > 10 && Me.IsSafelyFacing(Me.CurrentTarget)),
                        Spell.Cast("Disable", ret => !Me.CurrentTarget.HasMyAura("Disable")),
                        Spell.Cast("Leg Sweep", ret => Me.CurrentTarget.IsWithinMeleeRange),
                        Spell.Cast("Grapple Weapon", ret => Me.CurrentEnergy >= 20 && Me.CurrentTarget.IsPlayer),
                        Spell.Cast("Tiger Palm",
                            ret => Me.CurrentChi > 0
                                && (!Me.HasAura("Tiger Power") || Me.GetAuraTimeLeft("Tiger Power", true).TotalSeconds < 4) || Me.HasAura("Combo Breaker: Tiger Palm")),


                        Spell.Cast("Rising Sun Kick", ret => Me.CurrentChi >= 2 && Spell.GetSpellCooldown("Rising Sun Kick").Seconds == 0),

                        Spell.Cast("Fists of Fury",
                            ret => Spell.GetSpellCooldown("Rising Sun Kick").Seconds >= 2 && !Me.HasAura("Combo Breaker: Blackout Kick") && !Me.IsMoving && Me.HasAura("Tiger Power") && Me.CurrentChi >= 3),


                        Spell.Cast("Blackout Kick", ret => Spell.GetSpellCooldown("Rising Sun Kick").Seconds >= 2 && Me.CurrentChi >= 2 && Me.HasAura("Tiger Power") || Spell.GetSpellCooldown("Rising Sun Kick").Seconds >= 2 && Me.HasAura("Combo Breaker: Blackout Kick") && Me.HasAura("Tiger Power")),

                        Spell.Cast("Jab", ret => Me.CurrentChi <= 2 && Me.CurrentEnergy >= 40)
                        )
                    ),

                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        #endregion
    }
}