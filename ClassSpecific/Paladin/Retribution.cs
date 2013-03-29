using System.Linq;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;
using Styx;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.CommonBot;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Rest = Singular.Helpers.Rest;

namespace Singular.ClassSpecific.Paladin
{
    public class Retribution
    {
		private static LocalPlayer Me { get { return StyxWoW.Me; } }

        #region Heal
        [Behavior(BehaviorType.Heal, WoWClass.Paladin, WoWSpec.PaladinRetribution)]
        public static Composite CreatePaladinRetributionHeal()
        {
            return new PrioritySelector(
                Spell.Heal("Word of Glory", ret => StyxWoW.Me,
                           ret =>
                           Me.HealthPercent <= SingularSettings.Instance.Paladin.WordOfGloryHealth &&
                           Me.CurrentHolyPower == 3),
                Spell.Heal("Holy Light", ret => StyxWoW.Me,
                           ret =>
                           !SpellManager.HasSpell("Flash of Light") &&
                           Me.HealthPercent <= SingularSettings.Instance.Paladin.RetributionHealHealth),
                Spell.Heal("Flash of Light", ret => StyxWoW.Me,
                           ret => Me.HealthPercent <= SingularSettings.Instance.Paladin.RetributionHealHealth));
        }
        #endregion

        [Behavior(BehaviorType.Heal | BehaviorType.Combat, WoWClass.Paladin, WoWSpec.PaladinRetribution, WoWContext.All)]
        public static Composite CreatePaladinRetributionInstancePullAndCombat()
        {
            return new PrioritySelector
				(
                    //Safers.EnsureTarget(),
                    //Helpers.Common.CreateAutoAttack(true),
                    Helpers.Common.CreateInterruptSpellCast(ret => Me.CurrentTarget),
					
					//Remove all movement debuff
                    Spell.BuffSelf("Hand of Freedom",
                        ret => !Me.Auras.Values.Any(a => a.Name.Contains("Hand of") && a.CreatorGuid == Me.Guid) &&
                                Me.HasAuraWithMechanic(WoWSpellMechanic.Dazed,
                                                               WoWSpellMechanic.Disoriented,
                                                               WoWSpellMechanic.Frozen,
                                                               WoWSpellMechanic.Incapacitated,
                                                               WoWSpellMechanic.Rooted,
                                                               WoWSpellMechanic.Slowed,
                                                               WoWSpellMechanic.Snared)),

					//Self protection
					Spell.BuffSelf("Sacred Shield", ret => !Me.HasAura("Sacred Shield")),
                    Spell.BuffSelf("Divine Shield", ret => Me.HealthPercent <= 20 && !Me.HasAura("Forbearance")),
                    Spell.BuffSelf("Divine Protection", ret => Me.HealthPercent <= 80),

					//Seal dancing
                    Common.CreatePaladinSealBehavior(),

					//CDs
					Spell.Cast("Guardian of Ancient Kings", ret => Me.CurrentTarget.IsBoss()),
					Spell.BuffSelf("Avenging Wrath", ret => Unit.IsBoss(Me.CurrentTarget)),
					Spell.Cast("Execution Sentence", ret => Unit.IsBoss(Me.CurrentTarget)),
					Item.UseEquippedTrinket(TrinketUsage.OnCooldown),

					//Damage buff
					Spell.Cast("Inquisition", ret => Me.CurrentHolyPower >= 3 && !Me.HasAura("Inquisition")),

					//Base skill
					Spell.Cast("Hammer of Wrath"),
					//Devine Storm if more than 2 targets, Templar's Verdict if less than 2 targets
					Spell.Cast("Divine Storm", ret => Me.CurrentHolyPower >= 3 && Unit.NearbyUnfriendlyUnits.Count(u => u.Distance <= 8) >= 2),
					Spell.Cast("Templar's Verdict", ret => Me.CurrentHolyPower >= 3 && Unit.NearbyUnfriendlyUnits.Count(u => u.Distance <= 8) < 2),

					Spell.Cast("Exorcism"),
					//Hammer of the Righteous if more than 4 targets, Crusader Strike if less than 4 targets
					Spell.Cast("Hammer of the Righteous", ret => Unit.NearbyUnfriendlyUnits.Count(u => u.Distance <= 8) >= 4),
					Spell.Cast("Crusader Strike", ret => Unit.NearbyUnfriendlyUnits.Count(u => u.Distance <= 8) <= 3),

					Spell.Cast("Judgment")
                );
        }
    }
}
