﻿using System;
using System.Collections.Generic;
using System.Linq;
using Evade;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace SAwareness
{
    internal class Activator
    {
        public static Dictionary<Obj_AI_Hero, List<IncomingDamage>> Damages =
            new Dictionary<Obj_AI_Hero, List<IncomingDamage>>();

        public static List<Skillshot> DetectedSkillshots = new List<Skillshot>();
        private readonly List<BuffType> _buffs = new List<BuffType>();
        private float _lastItemCleanseUse;

        private const bool Debug = false;

        public Activator()
        {
            //foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
            //{
            //    if (!hero.IsEnemy)
            //    {
            //        Damages.Add(hero, new List<IncomingDamage>());
            //    }
            //}
            //Damages.Add(new Obj_AI_Hero(), new List<IncomingDamage>());
            //Game.OnGameUpdate += Game_OnGameUpdate;
            //Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
            //if (Debug)
            //    Drawing.OnDraw += Drawing_OnDraw;

            //Evade
            //SkillshotDetector.OnDetectSkillshot += OnDetectSkillshot;
            //SkillshotDetector.OnDeleteMissile += OnDeleteMissile;
        }

        ~Activator()
        {
            Game.OnGameUpdate -= Game_OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast -= Obj_AI_Hero_OnProcessSpellCast;

            SkillshotDetector.OnDetectSkillshot -= OnDetectSkillshot;
            SkillshotDetector.OnDeleteMissile -= OnDeleteMissile;
        }

        public bool IsActive()
        {
            return MainMenu.Activator.GetActive();
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Debug)
                foreach (var damage in Damages)
                {
                    Vector2 d2 = Drawing.WorldToScreen(damage.Key.ServerPosition);
                    Drawing.DrawText(d2.X, d2.Y, Color.Aquamarine, CalcMaxDamage(damage.Key).ToString());
                }
        }

        private void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!IsActive())
                return;
            UseOffensiveItems_OnProcessSpellCast(sender, args);
            GetIncomingDamage_OnProcessSpellCast(sender, args);
            UseSummonerSpells_OnProcessSpellCast(sender, args);
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (!IsActive())
                return;
            UseOffensiveItems_OnGameUpdate();
            UseMiscItems_OnGameUpdate();
            UseDefensiveItems_OnGameUpdate();
            GetIncomingDamage_OnGameUpdate();
            UseSummonerSpells_OnGameUpdate();
        }

        private void UseDefensiveItems_OnGameUpdate()
        {
            if (!MainMenu.ActivatorDefensive.GetActive())
                return;

            _buffs.Clear();
            if (
                MainMenu.ActivatorDefensiveCleanseConfig.GetMenuItem("SAwarenessActivatorDefensiveCleanseConfigStun")
                    .GetValue<bool>())
                _buffs.Add(BuffType.Stun);
            if (
                MainMenu.ActivatorDefensiveCleanseConfig.GetMenuItem("SAwarenessActivatorDefensiveCleanseConfigSilence")
                    .GetValue<bool>())
                _buffs.Add(BuffType.Silence);
            if (
                MainMenu.ActivatorDefensiveCleanseConfig.GetMenuItem("SAwarenessActivatorDefensiveCleanseConfigTaunt")
                    .GetValue<bool>())
                _buffs.Add(BuffType.Taunt);
            if (
                MainMenu.ActivatorDefensiveCleanseConfig.GetMenuItem("SAwarenessActivatorDefensiveCleanseConfigFear")
                    .GetValue<bool>())
                _buffs.Add(BuffType.Fear);
            if (
                MainMenu.ActivatorDefensiveCleanseConfig.GetMenuItem("SAwarenessActivatorDefensiveCleanseConfigCharm")
                    .GetValue<bool>())
                _buffs.Add(BuffType.Charm);
            if (
                MainMenu.ActivatorDefensiveCleanseConfig.GetMenuItem("SAwarenessActivatorDefensiveCleanseConfigBlind")
                    .GetValue<bool>())
                _buffs.Add(BuffType.Blind);
            if (
                MainMenu.ActivatorDefensiveCleanseConfig.GetMenuItem("SAwarenessActivatorDefensiveCleanseConfigDisarm")
                    .GetValue<bool>())
                _buffs.Add(BuffType.Disarm);
            if (
                MainMenu.ActivatorDefensiveCleanseConfig.GetMenuItem("SAwarenessActivatorDefensiveCleanseConfigSuppress")
                    .GetValue<bool>())
                _buffs.Add(BuffType.Suppression);
            if (
                MainMenu.ActivatorDefensiveCleanseConfig.GetMenuItem("SAwarenessActivatorDefensiveCleanseConfigSlow")
                    .GetValue<bool>())
                _buffs.Add(BuffType.Slow);
            if (
                MainMenu.ActivatorDefensiveCleanseConfig.GetMenuItem(
                    "SAwarenessActivatorDefensiveCleanseConfigCombatDehancer").GetValue<bool>())
                _buffs.Add(BuffType.CombatDehancer);
            if (
                MainMenu.ActivatorDefensiveCleanseConfig.GetMenuItem("SAwarenessActivatorDefensiveCleanseConfigSnare")
                    .GetValue<bool>())
                _buffs.Add(BuffType.Snare);
            if (
                MainMenu.ActivatorDefensiveCleanseConfig.GetMenuItem("SAwarenessActivatorDefensiveCleanseConfigPoison")
                    .GetValue<bool>())
                _buffs.Add(BuffType.Poison);

            UseSelfCleanseItems();
            UseSlowItems();
            UseShieldItems();
            UseMikaelsCrucible();
            UseZhonyaWooglet();
        }

        private void UseSelfCleanseItems()
        {
            UseQss();
            UseMs();
            UseDb();
        }

        private void UseQss()
        {
            if (!MainMenu.ActivatorDefensiveCleanseSelf.GetActive())
                return;

            List<BuffInstance> buffList = GetActiveCcBuffs(_buffs);

            if (buffList.Count() >=
                MainMenu.ActivatorDefensiveCleanseSelf.GetMenuItem("SAwarenessActivatorDefensiveCleanseSelfConfigMinSpells")
                    .GetValue<Slider>()
                    .Value &&
                MainMenu.ActivatorDefensiveCleanseSelf.GetMenuItem("SAwarenessActivatorDefensiveCleanseSelfQSS")
                    .GetValue<bool>() &&
                _lastItemCleanseUse + 1 < Game.Time)
            {
                var qss = new Items.Item(3140, 0);
                if (qss.IsReady())
                {
                    qss.Cast();
                    _lastItemCleanseUse = Game.Time;
                }
            }
        }

        private void UseMs()
        {
            if (!MainMenu.ActivatorDefensiveCleanseSelf.GetActive())
                return;

            List<BuffInstance> buffList = GetActiveCcBuffs(_buffs);

            if (buffList.Count() >=
                MainMenu.ActivatorDefensiveCleanseSelf.GetMenuItem("SAwarenessActivatorDefensiveCleanseSelfConfigMinSpells")
                    .GetValue<Slider>()
                    .Value &&
                MainMenu.ActivatorDefensiveCleanseSelf.GetMenuItem(
                    "SAwarenessActivatorDefensiveCleanseSelfMercurialScimitar").GetValue<bool>() &&
                _lastItemCleanseUse + 1 < Game.Time)
            {
                var ms = new Items.Item(3139, 0);
                if (ms.IsReady())
                {
                    foreach (BuffInstance instance in buffList)
                    {
                        Console.WriteLine(instance.Name);
                    }
                    ms.Cast();
                    _lastItemCleanseUse = Game.Time;
                }
            }
        }

        private void UseDb()
        {
            if (!MainMenu.ActivatorDefensiveCleanseSelf.GetActive())
                return;

            List<BuffInstance> buffList = GetActiveCcBuffs(_buffs);

            if (buffList.Count() >=
                MainMenu.ActivatorDefensiveCleanseSelf.GetMenuItem("SAwarenessActivatorDefensiveCleanseSelfConfigMinSpells")
                    .GetValue<Slider>()
                    .Value &&
                MainMenu.ActivatorDefensiveCleanseSelf.GetMenuItem("SAwarenessActivatorDefensiveCleanseSelfDervishBlade")
                    .GetValue<bool>() &&
                _lastItemCleanseUse + 1 < Game.Time)
            {
                var db = new Items.Item(3137, 0);
                if (db.IsReady())
                {
                    db.Cast();
                    _lastItemCleanseUse = Game.Time;
                }
            }
        }

        private void UseSlowItems()
        {
            UseRanduins();
            UseFrostQueensClaim();
        }

        private void UseRanduins()
        {
            if (!MainMenu.ActivatorDefensiveDebuffSlow.GetActive())
                return;

            Obj_AI_Hero hero = GetHighestAdEnemy();
            int count = ObjectManager.Player.ServerPosition.CountEnemiesInRange(400);
            if (hero == null || !hero.IsValid || hero.IsDead)
                return;

            if (
                MainMenu.ActivatorDefensiveDebuffSlow.GetMenuItem("SAwarenessActivatorDefensiveDebuffSlowRanduins")
                    .GetValue<bool>() &&
                MainMenu.ActivatorDefensiveDebuffSlow.GetMenuItem("SAwarenessActivatorDefensiveDebuffSlowConfigRanduins")
                    .GetValue<Slider>()
                    .Value >= count &&
                ImFleeing(hero) || IsFleeing(hero) && !ImFleeing(hero))
            {
                var randuins = new Items.Item(3143, 0);
                if (randuins.IsReady())
                {
                    randuins.Cast();
                }
            }
        }

        private void UseFrostQueensClaim()
        {
            if (!MainMenu.ActivatorDefensiveDebuffSlow.GetActive())
                return;

            Obj_AI_Hero enemy = null;
            int count = 0;
            int nCount = 0;

            foreach (Obj_AI_Hero hero1 in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero1.IsEnemy && hero1.IsVisible)
                {
                    if (hero1.ServerPosition.Distance(ObjectManager.Player.ServerPosition) < 750)
                    {
                        foreach (Obj_AI_Hero hero2 in ObjectManager.Get<Obj_AI_Hero>())
                        {
                            if (hero2.IsEnemy && hero2.IsVisible)
                            {
                                if (hero2.ServerPosition.Distance(hero1.ServerPosition) < 200)
                                {
                                    count++;
                                }
                            }
                        }
                        if (count == 0)
                        {
                            enemy = hero1;
                        }
                        else if (nCount < count)
                        {
                            nCount = count;
                            enemy = hero1;
                        }
                    }
                }
            }

            if (enemy == null || !enemy.IsValid || enemy.IsDead ||
                MainMenu.ActivatorDefensiveDebuffSlow.GetMenuItem(
                    "SAwarenessActivatorDefensiveDebuffSlowConfigFrostQueensClaim").GetValue<Slider>().Value > nCount)
                return;

            if (
                MainMenu.ActivatorDefensiveDebuffSlow.GetMenuItem("SAwarenessActivatorDefensiveDebuffSlowFrostQueensClaim")
                    .GetValue<bool>())
            {
                var fqc = new Items.Item(3092, 850);
                if (fqc.IsReady())
                {
                    fqc.Cast(enemy.ServerPosition);
                }
            }
        }

        private void UseShieldItems()
        {
            if (!MainMenu.ActivatorDefensiveShieldBoost.GetActive())
                return;

            UseLocketofIronSolari();
            UseTalismanofAscension();
            UseFaceOfTheMountain();
            UseGuardiansHorn();
        }

        private static double CheckForHit(Obj_AI_Hero hero)
        {
            List<IncomingDamage> damageList = Damages[Damages.Last().Key];
            double maxDamage = 0;
            foreach (IncomingDamage incomingDamage in damageList)
            {
                var pred = new PredictionInput();
                pred.Type = SkillshotType.SkillshotLine;
                pred.Radius = 50;
                pred.From = incomingDamage.StartPos;
                pred.RangeCheckFrom = incomingDamage.StartPos;
                pred.Range = incomingDamage.StartPos.Distance(incomingDamage.EndPos);
                pred.Collision = false;
                pred.Unit = hero;
                if (Prediction.GetPrediction(pred).Hitchance >= HitChance.Low)
                    maxDamage += incomingDamage.Dmg;
            }
            return maxDamage;
        }

        public static double CalcMaxDamage(Obj_AI_Hero hero, bool turret = true, bool minion = false)
        {
            List<IncomingDamage> damageList = Damages[hero];
            double maxDamage = 0;
            foreach (IncomingDamage incomingDamage in damageList)
            {
                if (!turret && incomingDamage.Turret)
                    continue;
                if (!minion && incomingDamage.Minion)
                    continue;
                maxDamage += incomingDamage.Dmg;
            }
            return maxDamage /* + CheckForHit(hero)*/;
        }

        private void UseLocketofIronSolari()
        {
            if (
                !MainMenu.ActivatorDefensiveShieldBoost.GetMenuItem(
                    "SAwarenessActivatorDefensiveShieldBoostLocketofIronSolari").GetValue<bool>())
                return;
            foreach (var pair in Damages)
            {
                double damage = CalcMaxDamage(pair.Key);
                Obj_AI_Hero hero = pair.Key;
                CheckForHit(hero);
                if (!hero.IsDead)
                {
                    var lis = new Items.Item(3190, 700);
                    if (hero.Health < damage && hero.ServerPosition.Distance(ObjectManager.Player.ServerPosition) < 700)
                    {
                        if (lis.IsReady())
                        {
                            lis.Cast();
                        }
                    }
                    else if (GetNegativBuff(hero) != null && Game.Time > GetNegativBuff(hero).EndTime - 0.1)
                    {
                        if (lis.IsReady())
                        {
                            lis.Cast();
                        }
                    }
                }
            }
        }

        private void UseTalismanofAscension()
        {
            if (
                !MainMenu.ActivatorDefensiveShieldBoost.GetMenuItem(
                    "SAwarenessActivatorDefensiveShieldBoostTalismanofAscension").GetValue<bool>())
                return;
            var ta = new Items.Item(3069, 0);
            Obj_AI_Hero hero = TargetSelector.GetTarget(1000, TargetSelector.DamageType.True);
            if (hero != null && hero.IsValid && !ImFleeing(hero) && IsFleeing(hero))
            {
                if ((hero.Health/hero.MaxHealth*100) <= 50)
                {
                    if (ta.IsReady())
                    {
                        ta.Cast();
                    }
                }
            }
            else if (ObjectManager.Player.ServerPosition.CountEnemiesInRange(1000) >
                     ObjectManager.Get<Obj_AI_Hero>().Where((units => units.IsAlly)).Count((units =>
                         (double)
                             Vector2.Distance(ObjectManager.Player.Position.To2D(),
                                 units.Position.To2D()) <= (double) 1000)) &&
                     ObjectManager.Player.Health != ObjectManager.Player.MaxHealth)
            {
                if (ta.IsReady())
                {
                    ta.Cast();
                }
            }
        }

        private void UseFaceOfTheMountain()
        {
            if (
                !MainMenu.ActivatorDefensiveShieldBoost.GetMenuItem(
                    "SAwarenessActivatorDefensiveShieldBoostFaceOfTheMountain").GetValue<bool>())
                return;
            foreach (var pair in Damages)
            {
                double damage = CalcMaxDamage(pair.Key);
                Obj_AI_Hero hero = pair.Key;
                if (!hero.IsDead)
                {
                    var lis = new Items.Item(3401, 700);
                    if (hero.Health < damage && hero.ServerPosition.Distance(ObjectManager.Player.ServerPosition) < 700)
                    {
                        if (lis.IsReady())
                        {
                            lis.Cast();
                        }
                    }
                    else if (GetNegativBuff(hero) != null && Game.Time > GetNegativBuff(hero).EndTime - 0.1)
                    {
                        if (lis.IsReady())
                        {
                            lis.Cast();
                        }
                    }
                }
            }
        }

        private void UseGuardiansHorn()
        {
            if (
                !MainMenu.ActivatorDefensiveShieldBoost.GetMenuItem("SAwarenessActivatorDefensiveShieldBoostGuardiansHorn")
                    .GetValue<bool>())
                return;
            if (Utility.Map.GetMap().Type != Utility.Map.MapType.HowlingAbyss)
                return;
            Obj_AI_Hero hero = TargetSelector.GetTarget(1000, TargetSelector.DamageType.True);
            if (hero != null && hero.IsValid && !ImFleeing(hero) && IsFleeing(hero))
            {
                var gh = new Items.Item(2051, 0);
                if (gh.IsReady())
                {
                    gh.Cast();
                }
            }
        }

        private void UseMikaelsCrucible()
        {
            if (!MainMenu.ActivatorDefensiveMikaelCleanse.GetActive() && _lastItemCleanseUse + 1 > Game.Time)
                return;

            var mc = new Items.Item(3222, 750);

            if (
                MainMenu.ActivatorDefensiveMikaelCleanse.GetMenuItem(
                    "SAwarenessActivatorDefensiveMikaelCleanseConfigAlly").GetValue<bool>())
            {
                foreach (Obj_AI_Hero ally in ObjectManager.Get<Obj_AI_Hero>())
                {
                    if (ally.IsEnemy)
                        return;

                    if (ally.ServerPosition.Distance(ObjectManager.Player.ServerPosition) < 750 && !ally.IsDead &&
                        !ally.HasBuff("Recall"))
                    {
                        double health = (ally.Health/ally.MaxHealth)*100;
                        List<BuffInstance> activeCc = GetActiveCcBuffs(ally, _buffs);
                        if (activeCc.Count >=
                            MainMenu.ActivatorDefensiveMikaelCleanse.GetMenuItem(
                                "SAwarenessActivatorDefensiveMikaelCleanseConfigMinSpells").GetValue<Slider>().Value)
                        {
                            if (mc.IsReady())
                            {
                                mc.Cast(ally);
                                _lastItemCleanseUse = Game.Time;
                            }
                        }
                        if (health <= MainMenu.ActivatorDefensiveMikaelCleanse.GetMenuItem(
                            "SAwarenessActivatorDefensiveMikaelCleanseConfigAllyHealth").GetValue<Slider>().Value)
                        {
                            if (mc.IsReady())
                            {
                                mc.Cast(ally);
                                _lastItemCleanseUse = Game.Time;
                            }
                        }
                    }
                }
            }
            else
            {
                if (!ObjectManager.Player.IsDead && !ObjectManager.Player.HasBuff("Recall"))
                {
                    double health = (ObjectManager.Player.Health/ObjectManager.Player.MaxHealth)*100;
                    List<BuffInstance> activeCc = GetActiveCcBuffs(_buffs);
                    if (activeCc.Count >=
                        MainMenu.ActivatorDefensiveMikaelCleanse.GetMenuItem(
                            "SAwarenessActivatorDefensiveMikaelCleanseConfigMinSpells").GetValue<Slider>().Value)
                    {
                        if (mc.IsReady())
                        {
                            mc.Cast(ObjectManager.Player);
                            _lastItemCleanseUse = Game.Time;
                        }
                    }
                    if (health <= MainMenu.ActivatorDefensiveMikaelCleanse.GetMenuItem(
                        "SAwarenessActivatorDefensiveMikaelCleanseConfigSelfHealth").GetValue<Slider>().Value)
                    {
                        if (mc.IsReady())
                        {
                            mc.Cast(ObjectManager.Player);
                            _lastItemCleanseUse = Game.Time;
                        }
                    }
                }
            }
        }

        private BuffInstance GetNegativBuff(Obj_AI_Hero hero)
        {
            foreach (BuffInstance buff in hero.Buffs)
            {
                if (buff.Name.Contains("fallenonetarget") || buff.Name.Contains("SoulShackles") ||
                    buff.Name.Contains("zedulttargetmark") || buff.Name.Contains("fizzmarinerdoombomb") ||
                    buff.Name.Contains("varusrsecondary"))
                    return buff;
            }
            return null;
        }

        public static List<BuffInstance> GetActiveCcBuffs(List<BuffType> buffs)
        {
            return GetActiveCcBuffs(ObjectManager.Player, buffs);
        }

        private static List<BuffInstance> GetActiveCcBuffs(Obj_AI_Hero hero, List<BuffType> buffs)
        {
            var nBuffs = new List<BuffInstance>();
            foreach (BuffInstance buff in hero.Buffs)
            {
                foreach (BuffType buffType in buffs)
                {
                    if (buff.Type == buffType)
                        nBuffs.Add(buff);
                }
            }
            return nBuffs;
        }

        private void UseOffensiveItems_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!MainMenu.ActivatorOffensive.GetActive())
                return;
            if (sender.NetworkId != ObjectManager.Player.NetworkId)
                return;
            if (!args.SData.Name.ToLower().Contains("attack") || args.Target.Type != GameObjectType.obj_AI_Hero)
                return;

            if (MainMenu.ActivatorOffensiveAd.GetActive())
            {
                Obj_AI_Hero target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Physical);
                if (target == null || !target.IsValid)
                    return;
                var entropy = new Items.Item(3184, 400);
                var hydra = new Items.Item(3074, 400);
                var botrk = new Items.Item(3153, 450);
                var tiamat = new Items.Item(3077, 450);
                var devinesword = new Items.Item(3131, 900);
                var youmuus = new Items.Item(3142, 900);

                if (entropy.IsReady() &&
                    MainMenu.ActivatorOffensiveAd.GetMenuItem("SAwarenessActivatorOffensiveAdEntropy").GetValue<bool>())
                {
                    entropy.Cast();
                }
                if (hydra.IsReady() &&
                    MainMenu.ActivatorOffensiveAd.GetMenuItem("SAwarenessActivatorOffensiveAdRavenousHydra")
                        .GetValue<bool>())
                {
                    hydra.Cast();
                }
                if (botrk.IsReady() &&
                    MainMenu.ActivatorOffensiveAd.GetMenuItem("SAwarenessActivatorOffensiveAdBOTRK").GetValue<bool>())
                {
                    botrk.Cast(target);
                }
                if (tiamat.IsReady() &&
                    MainMenu.ActivatorOffensiveAd.GetMenuItem("SAwarenessActivatorOffensiveAdTiamat").GetValue<bool>())
                {
                    tiamat.Cast();
                }
                if (devinesword.IsReady() &&
                    MainMenu.ActivatorOffensiveAd.GetMenuItem("SAwarenessActivatorOffensiveAdSwordOfTheDevine")
                        .GetValue<bool>())
                {
                    devinesword.Cast();
                }
                if (youmuus.IsReady() &&
                    MainMenu.ActivatorOffensiveAd.GetMenuItem("SAwarenessActivatorOffensiveAdYoumuusGhostblade")
                        .GetValue<bool>())
                {
                    youmuus.Cast();
                }
            }
        }

        private void UseOffensiveItems_OnGameUpdate()
        {
            if (!MainMenu.ActivatorOffensive.GetActive() ||
                !MainMenu.ActivatorOffensive.GetMenuItem("SAwarenessActivatorOffensiveKey").GetValue<KeyBind>().Active)
                return;
            if (MainMenu.ActivatorOffensiveAd.GetActive())
            {
                Obj_AI_Hero target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Physical);
                if (target == null || !target.IsValid)
                    return;
                var botrk = new Items.Item(3153, 450);
                if (botrk.IsReady() &&
                    MainMenu.ActivatorOffensiveAd.GetMenuItem("SAwarenessActivatorOffensiveAdBOTRK").GetValue<bool>())
                {
                    botrk.Cast(target);
                }
            }
            if (MainMenu.ActivatorOffensiveAp.GetActive())
            {
                Obj_AI_Hero target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Physical);
                if (target == null || !target.IsValid)
                    return;
                var bilgewater = new Items.Item(3144, 450);
                var hextech = new Items.Item(3146, 700);
                var blackfire = new Items.Item(3188, 750);
                var dfg = new Items.Item(3128, 750);
                var twinshadows = new Items.Item(3023, 1000);
                if (Utility.Map.GetMap().Type == Utility.Map.MapType.CrystalScar)
                    twinshadows = new Items.Item(3290, 1000);
                if (bilgewater.IsReady() &&
                    MainMenu.ActivatorOffensiveAp.GetMenuItem("SAwarenessActivatorOffensiveApBilgewaterCutlass")
                        .GetValue<bool>())
                {
                    bilgewater.Cast(target);
                }
                if (hextech.IsReady() &&
                    MainMenu.ActivatorOffensiveAp.GetMenuItem("SAwarenessActivatorOffensiveApHextechGunblade")
                        .GetValue<bool>())
                {
                    hextech.Cast(target);
                }
                if (blackfire.IsReady() &&
                    MainMenu.ActivatorOffensiveAp.GetMenuItem("SAwarenessActivatorOffensiveApBlackfireTorch").GetValue<bool>())
                {
                    blackfire.Cast(target);
                }
                if (dfg.IsReady() &&
                    MainMenu.ActivatorOffensiveAp.GetMenuItem("SAwarenessActivatorOffensiveApDFG").GetValue<bool>())
                {
                    dfg.Cast(target);
                }
                if (twinshadows.IsReady() &&
                    MainMenu.ActivatorOffensiveAp.GetMenuItem("SAwarenessActivatorOffensiveApTwinShadows").GetValue<bool>())
                {
                    twinshadows.Cast();
                }
            }
        }

        private void UseMiscItems_OnGameUpdate()
        {

        }

        public static bool IsCCd(Obj_AI_Hero hero)
        {
            var cc = new List<BuffType>
            {
                BuffType.Taunt,
                BuffType.Blind,
                BuffType.Charm,
                BuffType.Fear,
                BuffType.Polymorph,
                BuffType.Stun,
                BuffType.Silence,
                BuffType.Snare
            };

            return cc.Any(hero.HasBuffOfType);
        }

        public static SpellSlot GetIgniteSlot()
        {
            foreach (SpellDataInst spell in ObjectManager.Player.Spellbook.Spells)
            {
                if (spell.Name.ToLower().Contains("dot") && spell.State == SpellState.Ready)
                    return spell.Slot;
            }
            return SpellSlot.Unknown;
        }

        public static SpellSlot GetSmiteSlot()
        {
            foreach (SpellDataInst spell in ObjectManager.Player.Spellbook.Spells)
            {
                if (spell.Name.ToLower().Contains("smite") && spell.State == SpellState.Ready)
                    return spell.Slot;
            }
            return SpellSlot.Unknown;
        }

        public static SpellSlot GetHealSlot()
        {
            foreach (SpellDataInst spell in ObjectManager.Player.Spellbook.Spells)
            {
                if (spell.Name.ToLower().Contains("heal") && spell.State == SpellState.Ready)
                    return spell.Slot;
            }
            return SpellSlot.Unknown;
        }

        public static SpellSlot GetBarrierSlot()
        {
            foreach (SpellDataInst spell in ObjectManager.Player.Spellbook.Spells)
            {
                if (spell.Name.ToLower().Contains("barrier") && spell.State == SpellState.Ready)
                    return spell.Slot;
            }
            return SpellSlot.Unknown;
        }

        public static SpellSlot GetExhaustSlot()
        {
            foreach (SpellDataInst spell in ObjectManager.Player.Spellbook.Spells)
            {
                if (spell.Name.ToLower().Contains("exhaust") && spell.State == SpellState.Ready)
                    return spell.Slot;
            }
            return SpellSlot.Unknown;
        }

        public static SpellSlot GetCleanseSlot()
        {
            foreach (SpellDataInst spell in ObjectManager.Player.Spellbook.Spells)
            {
                if (spell.Name.ToLower().Contains("boost") && spell.State == SpellState.Ready)
                    return spell.Slot;
            }
            return SpellSlot.Unknown;
        }

        public static SpellSlot GetFlashSlot()
        {
            foreach (SpellDataInst spell in ObjectManager.Player.Spellbook.Spells)
            {
                if (spell.Name.ToLower().Contains("flash") && spell.State == SpellState.Ready)
                    return spell.Slot;
            }
            return SpellSlot.Unknown;
        }

        public static SpellSlot GetPacketSlot(SpellSlot nSpellSlot)
        {
            SpellSlot spellSlot = nSpellSlot;
            int slot = -1;
            if (spellSlot == SpellSlot.Q)
                slot = 64;
            else if (spellSlot == SpellSlot.W)
                slot = 65;
            if (slot != -1)
            {
                return (SpellSlot) slot;
            }
            return SpellSlot.Unknown;
        }

        private void UseSummonerSpells_OnGameUpdate()
        {
            if (!MainMenu.ActivatorAutoSummonerSpell.GetActive())
                return;

            UseIgnite();
            UseHealth();
            UseBarrier();
            UseExhaust_OnGameUpdate();
            UseCleanse();
        }

        private void UseSummonerSpells_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            UseExhaust_OnProcessSpellCast(sender, args);
        }

        private void UseCleanse()
        {
            if (!MainMenu.ActivatorAutoSummonerSpellCleanse.GetActive())
                return;
            SpellSlot sumCleanse = GetCleanseSlot();
            if (ObjectManager.Player.Spellbook.CanUseSpell(sumCleanse) != SpellState.Ready)
                return;
            _buffs.Clear();
            if (
                MainMenu.ActivatorAutoSummonerSpellCleanse.GetMenuItem("SAwarenessActivatorAutoSummonerSpellCleanseStun")
                    .GetValue<bool>())
                _buffs.Add(BuffType.Stun);
            if (
                MainMenu.ActivatorAutoSummonerSpellCleanse.GetMenuItem("SAwarenessActivatorAutoSummonerSpellCleanseSilence")
                    .GetValue<bool>())
                _buffs.Add(BuffType.Silence);
            if (
                MainMenu.ActivatorAutoSummonerSpellCleanse.GetMenuItem("SAwarenessActivatorAutoSummonerSpellCleanseTaunt")
                    .GetValue<bool>())
                _buffs.Add(BuffType.Taunt);
            if (
                MainMenu.ActivatorAutoSummonerSpellCleanse.GetMenuItem("SAwarenessActivatorAutoSummonerSpellCleanseFear")
                    .GetValue<bool>())
                _buffs.Add(BuffType.Fear);
            if (
                MainMenu.ActivatorAutoSummonerSpellCleanse.GetMenuItem("SAwarenessActivatorAutoSummonerSpellCleanseCharm")
                    .GetValue<bool>())
                _buffs.Add(BuffType.Charm);
            if (
                MainMenu.ActivatorAutoSummonerSpellCleanse.GetMenuItem("SAwarenessActivatorAutoSummonerSpellCleanseBlind")
                    .GetValue<bool>())
                _buffs.Add(BuffType.Blind);
            if (
                MainMenu.ActivatorAutoSummonerSpellCleanse.GetMenuItem("SAwarenessActivatorAutoSummonerSpellCleanseDisarm")
                    .GetValue<bool>())
                _buffs.Add(BuffType.Disarm);
            if (
                MainMenu.ActivatorAutoSummonerSpellCleanse.GetMenuItem("SAwarenessActivatorAutoSummonerSpellCleanseSlow")
                    .GetValue<bool>())
                _buffs.Add(BuffType.Slow);
            if (
                MainMenu.ActivatorAutoSummonerSpellCleanse.GetMenuItem(
                    "SAwarenessActivatorAutoSummonerSpellCleanseCombatDehancer").GetValue<bool>())
                _buffs.Add(BuffType.CombatDehancer);
            if (
                MainMenu.ActivatorAutoSummonerSpellCleanse.GetMenuItem("SAwarenessActivatorAutoSummonerSpellCleanseSnare")
                    .GetValue<bool>())
                _buffs.Add(BuffType.Snare);
            if (
                MainMenu.ActivatorAutoSummonerSpellCleanse.GetMenuItem("SAwarenessActivatorAutoSummonerSpellCleansePoison")
                    .GetValue<bool>())
                _buffs.Add(BuffType.Poison);

            List<BuffInstance> buffList = GetActiveCcBuffs(_buffs);

            if (buffList.Count() >=
                MainMenu.ActivatorAutoSummonerSpellCleanse.GetMenuItem(
                    "SAwarenessActivatorAutoSummonerSpellCleanseMinSpells").GetValue<Slider>().Value &&
                _lastItemCleanseUse + 1 < Game.Time)
            {
                SpellSlot spellSlot = sumCleanse;
                if (spellSlot != SpellSlot.Unknown)
                {
                    GamePacket gPacketT = Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(0, spellSlot));
                    gPacketT.Send();
                    ObjectManager.Player.Spellbook.CastSpell(sumCleanse);
                    _lastItemCleanseUse = Game.Time;
                }
            }
        }

        private void UseIgnite()
        {
            if (!MainMenu.ActivatorAutoSummonerSpellIgnite.GetActive())
                return;
            SpellSlot sumIgnite = GetIgniteSlot();
            if (ObjectManager.Player.Spellbook.CanUseSpell(sumIgnite) != SpellState.Ready)
                return;
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero != null && hero.IsEnemy && hero.ServerPosition.Distance(ObjectManager.Player.ServerPosition) < 700)
                {
                    if (sumIgnite != SpellSlot.Unknown)
                    {
                        double igniteDmg = ObjectManager.Player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite);
                        double regenpersec = (hero.FlatHPRegenMod + (hero.HPRegenRate * hero.Level));
                        double dmgafter = (igniteDmg - ((regenpersec * 5) / 2));
                        if (dmgafter > hero.Health)
                        {
                            SpellSlot spellSlot = sumIgnite;
                            if (spellSlot != SpellSlot.Unknown)
                            {
                                GamePacket gPacketT =
                                    Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(hero.NetworkId, spellSlot));
                                gPacketT.Send();
                                ObjectManager.Player.Spellbook.CastSpell(sumIgnite, hero);
                            }
                        }
                    }
                }
            }
        }

        private void UseHealth()
        {
            if (!MainMenu.ActivatorAutoSummonerSpellHeal.GetActive())
                return;

            SpellSlot sumHeal = GetHealSlot();

            if (ObjectManager.Player.Spellbook.CanUseSpell(sumHeal) != SpellState.Ready)
                return;

            if (
                MainMenu.ActivatorAutoSummonerSpellHeal.GetMenuItem("SAwarenessActivatorAutoSummonerSpellHealAllyActive")
                    .GetValue<bool>())
            {
                foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
                {
                    if (!hero.IsEnemy && !hero.IsDead &&
                        hero.ServerPosition.Distance(ObjectManager.Player.ServerPosition) < 700)
                    {
                        if (((hero.Health/hero.MaxHealth)*100) <
                            MainMenu.ActivatorAutoSummonerSpellHeal.GetMenuItem(
                                "SAwarenessActivatorAutoSummonerSpellHealPercent").GetValue<Slider>().Value)
                        {
                            SpellSlot spellSlot = sumHeal;
                            if (spellSlot != SpellSlot.Unknown)
                            {
                                GamePacket gPacketT = Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(0, spellSlot));
                                gPacketT.Send();
                                ObjectManager.Player.Spellbook.CastSpell(sumHeal);
                            }
                        }
                        foreach (var damage in Damages)
                        {
                            if (damage.Key.NetworkId != ObjectManager.Player.NetworkId)
                                continue;

                            if (CalcMaxDamage(damage.Key) >= damage.Key.Health)
                            {
                                SpellSlot spellSlot = sumHeal;
                                if (spellSlot != SpellSlot.Unknown)
                                {
                                    GamePacket gPacketT = Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(0, spellSlot));
                                    gPacketT.Send();
                                    ObjectManager.Player.Spellbook.CastSpell(sumHeal);
                                }
                            }
                        }
                    }
                }
            }
            if (((ObjectManager.Player.Health/ObjectManager.Player.MaxHealth)*100) <
                MainMenu.ActivatorAutoSummonerSpellHeal.GetMenuItem("SAwarenessActivatorAutoSummonerSpellHealPercent")
                    .GetValue<Slider>()
                    .Value)
            {
                SpellSlot spellSlot = sumHeal;
                if (spellSlot != SpellSlot.Unknown)
                {
                    GamePacket gPacketT = Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(0, spellSlot));
                    gPacketT.Send();
                    ObjectManager.Player.Spellbook.CastSpell(sumHeal);
                }
            }
            foreach (var damage in Damages)
            {
                if (damage.Key.NetworkId != ObjectManager.Player.NetworkId)
                    continue;

                if (CalcMaxDamage(damage.Key) >= damage.Key.Health)
                {
                    SpellSlot spellSlot = sumHeal;
                    if (spellSlot != SpellSlot.Unknown)
                    {
                        GamePacket gPacketT = Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(0, spellSlot));
                        gPacketT.Send();
                        ObjectManager.Player.Spellbook.CastSpell(sumHeal);
                    }
                }
            }
        }

        private void UseBarrier()
        {
            if (!MainMenu.ActivatorAutoSummonerSpellBarrier.GetActive())
                return;

            SpellSlot sumBarrier = GetBarrierSlot();

            if (ObjectManager.Player.Spellbook.CanUseSpell(sumBarrier) != SpellState.Ready)
                return;

            if (((ObjectManager.Player.Health/ObjectManager.Player.MaxHealth)*100) <
                MainMenu.ActivatorAutoSummonerSpellBarrier.GetMenuItem("SAwarenessActivatorAutoSummonerSpellBarrierPercent")
                    .GetValue<Slider>()
                    .Value)
            {
                SpellSlot spellSlot = sumBarrier;
                if (spellSlot != SpellSlot.Unknown)
                {
                    GamePacket gPacketT = Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(0, spellSlot));
                    gPacketT.Send();
                    ObjectManager.Player.Spellbook.CastSpell(sumBarrier);
                }
            }
            foreach (var damage in Damages)
            {
                if (damage.Key.NetworkId != ObjectManager.Player.NetworkId)
                    continue;

                if (CalcMaxDamage(damage.Key) >= damage.Key.Health)
                {
                    SpellSlot spellSlot = sumBarrier;
                    if (spellSlot != SpellSlot.Unknown)
                    {
                        GamePacket gPacketT = Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(0, spellSlot));
                        gPacketT.Send();
                        ObjectManager.Player.Spellbook.CastSpell(sumBarrier);
                    }
                }
            }
        }

        private void UseExhaust_OnGameUpdate()
        {
            if (!MainMenu.ActivatorAutoSummonerSpellExhaust.GetActive())
                return;

            SpellSlot sumExhaust = GetExhaustSlot();
            if (ObjectManager.Player.Spellbook.CanUseSpell(sumExhaust) != SpellState.Ready)
                return;
            Obj_AI_Hero enemy = GetHighestAdEnemy();
            if (enemy == null || !enemy.IsValid)
                return;
            int countE = ObjectManager.Player.ServerPosition.CountEnemiesInRange(750);
            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (!hero.IsMe && !hero.IsEnemy && hero.IsValid &&
                    hero.ServerPosition.Distance(ObjectManager.Player.ServerPosition) <= 900 &&
                    countE >=
                    MainMenu.ActivatorAutoSummonerSpellExhaust.GetMenuItem(
                        "SAwarenessActivatorAutoSummonerSpellExhaustMinEnemies").GetValue<Slider>().Value)
                {
                    int countA =
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(units => !units.IsEnemy)
                            .Count(
                                units =>
                                    Vector2.Distance(ObjectManager.Player.Position.To2D(), units.Position.To2D()) <= 750);
                    float healthA = hero.Health/hero.MaxHealth*100;
                    float healthE = enemy.Health/enemy.MaxHealth*100;
                    SpellSlot spellSlot = sumExhaust;
                    if (spellSlot != SpellSlot.Unknown)
                    {
                        GamePacket gPacketT =
                            Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(enemy.NetworkId, spellSlot));
                        if (
                            MainMenu.ActivatorAutoSummonerSpellExhaust.GetMenuItem(
                                "SAwarenessActivatorAutoSummonerSpellExhaustAutoCast").GetValue<KeyBind>().Active &&
                            IsFleeing(enemy) && !ImFleeing(enemy) && countA > 0)
                        {
                            gPacketT.Send();
                            ObjectManager.Player.Spellbook.CastSpell(sumExhaust, enemy);
                        }
                        else if (
                            MainMenu.ActivatorAutoSummonerSpellExhaust.GetMenuItem(
                                "SAwarenessActivatorAutoSummonerSpellExhaustAutoCast").GetValue<KeyBind>().Active &&
                            !IsFleeing(enemy) && healthA < 25)
                        {
                            gPacketT.Send();
                            ObjectManager.Player.Spellbook.CastSpell(sumExhaust, enemy);
                        }
                        else if (!IsFleeing(enemy) &&
                                 healthA <=
                                 MainMenu.ActivatorAutoSummonerSpellExhaust.GetMenuItem(
                                     "SAwarenessActivatorAutoSummonerSpellExhaustAllyPercent")
                                     .GetValue<Slider>()
                                     .Value)
                        {
                            gPacketT.Send();
                            ObjectManager.Player.Spellbook.CastSpell(sumExhaust, enemy);
                        }
                        else if (!ImFleeing(enemy) && countA > 0 && IsFleeing(enemy) && healthE >= 10 &&
                                 healthE <=
                                 MainMenu.ActivatorAutoSummonerSpellExhaust.GetMenuItem(
                                     "SAwarenessActivatorAutoSummonerSpellExhaustSelfPercent")
                                     .GetValue<Slider>()
                                     .Value)
                        {
                            gPacketT.Send();
                            ObjectManager.Player.Spellbook.CastSpell(sumExhaust, enemy);
                        }
                    }
                }
            }
        }

        private void UseExhaust_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!MainMenu.ActivatorAutoSummonerSpellExhaust.GetActive() ||
                !MainMenu.ActivatorAutoSummonerSpellExhaust.GetMenuItem(
                    "SAwarenessActivatorAutoSummonerSpellExhaustUseUltSpells").GetValue<bool>())
                return;

            if (sender.IsEnemy)
            {
                SpellSlot sumExhaust = GetExhaustSlot();
                if (ObjectManager.Player.Spellbook.CanUseSpell(sumExhaust) != SpellState.Ready)
                    return;
                if (sumExhaust != SpellSlot.Unknown)
                {
                    if (args.SData.Name.Contains("InfernalGuardian") || //Annie
                        args.SData.Name.Contains("BrandWildfire") || //Brand
                        args.SData.Name.Contains("CaitlynAceintheHole") || //Caitlyn
                        args.SData.Name.Contains("DravenRCast") || //Draven
                        args.SData.Name.Contains("EzrealTrueshotBarrage") || //Ezreal
                        args.SData.Name.Contains("Crowstorm") || //Fiddle
                        args.SData.Name.Contains("FioraDance") || //Fiora
                        args.SData.Name.Contains("FizzMarinerDoom") || //Fizz
                        args.SData.Name.Contains("GragasR") || //Gragas
                        args.SData.Name.Contains("GravesChargeShot") || //Graves
                        args.SData.Name.Contains("JinxR") || //Jinx
                        args.SData.Name.Contains("KatarinaR") || //Katarina
                        args.SData.Name.Contains("KennenShurikenStorm") || //Kennen
                        args.SData.Name.Contains("LissandraR") || //Lissandra
                        args.SData.Name.Contains("LuxMaliceCannon") || //Lux
                        args.SData.Name.Contains("AlZaharNetherGrasp") || //Malzahar
                        args.SData.Name.Contains("MissFortuneBulletTime") || //Miss Fortune
                        args.SData.Name.Contains("OrianaDetonateCommand") || //Orianna
                        args.SData.Name.Contains("RivenFengShuiEngine") || //Riven
                        args.SData.Name.Contains("SyndraR") || //Syndra
                        args.SData.Name.Contains("TalonShadowAssault") || //Talon
                        args.SData.Name.Contains("BusterShot") || //Tristana
                        args.SData.Name.Contains("FullAutomatic") || //Twitch
                        args.SData.Name.Contains("VeigarPrimordialBurst") || //Veigar
                        args.SData.Name.Contains("VelkozR") || //Vel Koz
                        args.SData.Name.Contains("ViktorChaosStorm") || //Viktor
                        args.SData.Name.Contains("MonkeyKingSpinToWin") || //Wukong
                        args.SData.Name.Contains("XerathLocusOfPower2") || //Xerath
                        args.SData.Name.Contains("YasuoRKnockUpComboW") || //Yasuo
                        args.SData.Name.Contains("ZiggsR") || //Ziggs
                        args.SData.Name.Contains("ZyraBrambleZone")) //Zyra
                    {
                        if (sender.ServerPosition.Distance(ObjectManager.Player.ServerPosition) <= 750)
                        {
                            Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(sender.NetworkId, sumExhaust)).Send();
                            ObjectManager.Player.Spellbook.CastSpell(sumExhaust, sender);
                        }
                    }

                    if (args.SData.Name.Contains("SoulShackles") || //Morgana
                        args.SData.Name.Contains("KarthusFallenOne") || //Karthus
                        args.SData.Name.Contains("VladimirHemoplague")) //Vladimir
                    {
                        Utility.DelayAction.Add(2500, () =>
                        {
                            if (sender.ServerPosition.Distance(ObjectManager.Player.ServerPosition) <= 750)
                            {
                                Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(sender.NetworkId, sumExhaust)).Send();
                                ObjectManager.Player.Spellbook.CastSpell(sumExhaust, sender);
                            }
                        });
                    }

                    if (args.SData.Name.Contains("AbsoluteZero") || //Nunu
                        args.SData.Name.Contains("ZedUlt")) //Zed
                    {
                        Utility.DelayAction.Add(500, () =>
                        {
                            if (sender.ServerPosition.Distance(ObjectManager.Player.ServerPosition) <= 750)
                            {
                                Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(sender.NetworkId, sumExhaust)).Send();
                                ObjectManager.Player.Spellbook.CastSpell(sumExhaust, sender);
                            }
                        });
                    }
                }
            }
        }

        private Obj_AI_Hero GetHighestAdEnemy()
        {
            Obj_AI_Hero highestAd = null;
            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.IsEnemy)
                {
                    if (hero.IsValidTarget() && hero.Distance(ObjectManager.Player.ServerPosition) <= 650)
                    {
                        if (highestAd == null)
                        {
                            highestAd = hero;
                        }
                        else if (highestAd.BaseAttackDamage + highestAd.FlatPhysicalDamageMod <
                                 hero.BaseAttackDamage + hero.FlatPhysicalDamageMod)
                        {
                            highestAd = hero;
                        }
                    }
                }
            }
            return highestAd;
        }

        private bool IsFleeing(Obj_AI_Hero hero)
        {
            if (hero.IsValid &&
                hero.ServerPosition.Distance(ObjectManager.Player.Position) >
                hero.Position.Distance(ObjectManager.Player.Position))
            {
                return true;
            }
            return false;
        }

        private bool ImFleeing(Obj_AI_Hero hero)
        {
            if (hero.IsValid &&
                hero.Position.Distance(ObjectManager.Player.ServerPosition) >
                hero.Position.Distance(ObjectManager.Player.Position))
            {
                return true;
            }
            return false;
        }

        private void UseZhonyaWooglet()
        {
            if (!MainMenu.ActivatorDefensiveWoogletZhonya.GetActive())
                return;

            Items.Item item = null;

            if (MainMenu.ActivatorDefensiveWoogletZhonya.GetMenuItem(
                "SAwarenessActivatorDefensiveWoogletZhonyaWooglet").GetValue<bool>())
                item = new Items.Item(3090, 0); //Items.Wooglets_Witchcap;//
            if (item == null && MainMenu.ActivatorDefensiveWoogletZhonya.GetMenuItem(
                "SAwarenessActivatorDefensiveWoogletZhonyaZhonya").GetValue<bool>())
                item = new Items.Item(3157, 0); //Items.Zhonyas_Hourglass;//

            if (item == null)
                return;

            foreach (var damage in Damages)
            {
                if(damage.Key.NetworkId != ObjectManager.Player.NetworkId)
                    continue;

                if (CalcMaxDamage(damage.Key) > damage.Key.Health)
                {
                    if (item.IsReady())
                    {
                        item.Cast();
                    }
                }
            }
        }

        private static void OnDetectSkillshot(Skillshot skillshot)
        {
            bool alreadyAdded = false;

            foreach (Skillshot item in DetectedSkillshots)
            {
                if (item.SpellData.SpellName == skillshot.SpellData.SpellName &&
                    (item.Unit.NetworkId == skillshot.Unit.NetworkId &&
                     (skillshot.Direction).AngleBetween(item.Direction) < 5 &&
                     (skillshot.Start.Distance(item.Start) < 100 || skillshot.SpellData.FromObjects.Length == 0)))
                {
                    alreadyAdded = true;
                }
            }

            //Check if the skillshot is from an ally.
            if (skillshot.Unit.Team == ObjectManager.Player.Team)
            {
                return;
            }

            //Check if the skillshot is too far away.
            if (skillshot.Start.Distance(ObjectManager.Player.ServerPosition.To2D()) >
                (skillshot.SpellData.Range + skillshot.SpellData.Radius + 1000)*1.5)
            {
                return;
            }

            //Add the skillshot to the detected skillshot list.
            if (!alreadyAdded)
            {
                //Multiple skillshots like twisted fate Q.
                if (skillshot.DetectionType == DetectionType.ProcessSpell)
                {
                    if (skillshot.SpellData.MultipleNumber != -1)
                    {
                        var originalDirection = skillshot.Direction;

                        for (int i = -(skillshot.SpellData.MultipleNumber - 1)/2;
                            i <= (skillshot.SpellData.MultipleNumber - 1)/2;
                            i++)
                        {
                            var end = skillshot.Start +
                                      skillshot.SpellData.Range*
                                      originalDirection.Rotated(skillshot.SpellData.MultipleAngle*i);
                            var skillshotToAdd = new Skillshot(
                                skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start, end,
                                skillshot.Unit);

                            DetectedSkillshots.Add(skillshotToAdd);
                        }
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "UFSlash")
                    {
                        skillshot.SpellData.MissileSpeed = 1600 + (int) skillshot.Unit.MoveSpeed;
                    }

                    if (skillshot.SpellData.Invert)
                    {
                        var newDirection = -(skillshot.End - skillshot.Start).Normalized();
                        var end = skillshot.Start + newDirection*skillshot.Start.Distance(skillshot.End);
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start, end,
                            skillshot.Unit);
                        DetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }

                    if (skillshot.SpellData.Centered)
                    {
                        var start = skillshot.Start - skillshot.Direction*skillshot.SpellData.Range;
                        var end = skillshot.Start + skillshot.Direction*skillshot.SpellData.Range;
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                            skillshot.Unit);
                        DetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "SyndraE" || skillshot.SpellData.SpellName == "syndrae5")
                    {
                        int angle = 60;
                        var edge1 =
                            (skillshot.End - skillshot.Unit.ServerPosition.To2D()).Rotated(
                                -angle/2*(float) Math.PI/180);
                        var edge2 = edge1.Rotated(angle*(float) Math.PI/180);

                        foreach (Obj_AI_Minion minion in ObjectManager.Get<Obj_AI_Minion>())
                        {
                            Vector2 v = minion.ServerPosition.To2D() - skillshot.Unit.ServerPosition.To2D();
                            if (minion.Name == "Seed" && edge1.CrossProduct(v) > 0 && v.CrossProduct(edge2) > 0 &&
                                minion.Distance(skillshot.Unit) < 800 &&
                                (minion.Team != ObjectManager.Player.Team))
                            {
                                Vector2 start = minion.ServerPosition.To2D();
                                var end = skillshot.Unit.ServerPosition.To2D()
                                    .Extend(
                                        minion.ServerPosition.To2D(),
                                        skillshot.Unit.Distance(minion) > 200 ? 1300 : 1000);

                                var skillshotToAdd = new Skillshot(
                                    skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                                    skillshot.Unit);
                                DetectedSkillshots.Add(skillshotToAdd);
                            }
                        }
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "AlZaharCalloftheVoid")
                    {
                        Vector2 start = skillshot.End - skillshot.Direction.Perpendicular()*400;
                        Vector2 end = skillshot.End + skillshot.Direction.Perpendicular() * 400;
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                            skillshot.Unit);
                        DetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "ZiggsQ")
                    {
                        var d1 = skillshot.Start.Distance(skillshot.End);
                        float d2 = d1*0.4f;
                        float d3 = d2*0.69f;


                        var bounce1SpellData = SpellDatabase.GetByName("ZiggsQBounce1");
                        var bounce2SpellData = SpellDatabase.GetByName("ZiggsQBounce2");

                        Vector2 bounce1Pos = skillshot.End + skillshot.Direction * d2;
                        Vector2 bounce2Pos = bounce1Pos + skillshot.Direction * d3;

                        bounce1SpellData.Delay =
                            (int) (skillshot.SpellData.Delay + d1*1000f/skillshot.SpellData.MissileSpeed + 500);
                        bounce2SpellData.Delay =
                            (int) (bounce1SpellData.Delay + d2*1000f/bounce1SpellData.MissileSpeed + 500);

                        var bounce1 = new Skillshot(
                            skillshot.DetectionType, bounce1SpellData, skillshot.StartTick, skillshot.End, bounce1Pos,
                            skillshot.Unit);
                        var bounce2 = new Skillshot(
                            skillshot.DetectionType, bounce2SpellData, skillshot.StartTick, bounce1Pos, bounce2Pos,
                            skillshot.Unit);

                        DetectedSkillshots.Add(bounce1);
                        DetectedSkillshots.Add(bounce2);
                    }

                    if (skillshot.SpellData.SpellName == "ZiggsR")
                    {
                        skillshot.SpellData.Delay =
                            (int)(1500 + 1500*skillshot.End.Distance(skillshot.Start)/skillshot.SpellData.Range);
                    }

                    if (skillshot.SpellData.SpellName == "JarvanIVDragonStrike")
                    {
                        var endPos = new Vector2();

                        foreach (Skillshot s in DetectedSkillshots)
                        {
                            if (s.Unit.NetworkId == skillshot.Unit.NetworkId && s.SpellData.Slot == SpellSlot.E)
                            {
                                endPos = s.End;
                            }
                        }

                        foreach (Obj_AI_Minion m in ObjectManager.Get<Obj_AI_Minion>())
                        {
                            if (m.BaseSkinName == "jarvanivstandard" && m.Team == skillshot.Unit.Team &&
                                skillshot.IsDanger(m.Position.To2D()))
                            {
                                endPos = m.Position.To2D();
                            }
                        }

                        if (!endPos.IsValid())
                        {
                            return;
                        }

                        skillshot.End = endPos + 200*(endPos - skillshot.Start).Normalized();
                        skillshot.Direction = (skillshot.End - skillshot.Start).Normalized();
                    }
                }

                if (skillshot.SpellData.SpellName == "OriannasQ")
                {
                    var endCSpellData = SpellDatabase.GetByName("OriannaQend");

                    var skillshotToAdd = new Skillshot(
                        skillshot.DetectionType, endCSpellData, skillshot.StartTick, skillshot.Start, skillshot.End,
                        skillshot.Unit);

                    DetectedSkillshots.Add(skillshotToAdd);
                }


                //Dont allow fow detection.
                if (skillshot.SpellData.DisableFowDetection && skillshot.DetectionType == DetectionType.RecvPacket)
                {
                    return;
                }
#if DEBUG
                Console.WriteLine(Environment.TickCount + "Adding new skillshot: " + skillshot.SpellData.SpellName);
#endif

                DetectedSkillshots.Add(skillshot);
            }
        }

        private static void OnDeleteMissile(Skillshot skillshot, Obj_SpellMissile missile)
        {
            if (skillshot.SpellData.SpellName == "VelkozQ")
            {
                var spellData = SpellDatabase.GetByName("VelkozQSplit");
                var direction = skillshot.Direction.Perpendicular();
                if (DetectedSkillshots.Count(s => s.SpellData.SpellName == "VelkozQSplit") == 0)
                {
                    for (int i = -1; i <= 1; i = i + 2)
                    {
                        var skillshotToAdd = new Skillshot(
                            DetectionType.ProcessSpell, spellData, Environment.TickCount, missile.Position.To2D(),
                            missile.Position.To2D() + i*direction*spellData.Range, skillshot.Unit);
                        DetectedSkillshots.Add(skillshotToAdd);
                    }
                }
            }
        }

        private void GetIncomingDamage_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            foreach (var damage in Damages)
            {
                foreach (IncomingDamage incomingDamage in damage.Value.ToArray())
                {
                    if (incomingDamage.TimeHit < Game.Time)
                        damage.Value.Remove(incomingDamage);
                }
                if (sender.NetworkId == damage.Key.NetworkId)
                    continue;
                //if (args.Target.Type == GameObjectType.obj_LampBulb || args.Target.Type == GameObjectType.Unknown)
                //    //No target, find it later
                //{
                //    try
                //    {
                //        double spellDamage = sender.GetSpellDamage((Obj_AI_Base) args.Target, args.SData.Name);
                //        if (spellDamage != 0.0f)
                //            Damages[Damages.Last().Key].Add(new IncomingDamage(args.SData.Name, sender, args.Start,
                //                args.End, spellDamage,
                //                IncomingDamage.CalcTimeHit(args.TimeCast, sender, damage.Key, args.End)));
                //    }
                //    catch (InvalidOperationException)
                //    {
                //        //Cannot find spell
                //    }
                //    catch (InvalidCastException)
                //    {
                //        //TODO Need a workaround to get the spelldamage for args.Target
                //        return;
                //    }
                //}
                if (args.SData.Name.ToLower().Contains("attack") && args.Target.NetworkId == damage.Key.NetworkId)
                {
                    double aaDamage = sender.GetAutoAttackDamage((Obj_AI_Base) args.Target);
                    if (aaDamage != 0.0f)
                        if (sender.Type == GameObjectType.obj_AI_Minion)
                        {
                            Damages[damage.Key].Add(new IncomingDamage(args.SData.Name, sender, args.Start, args.End,
                                aaDamage, IncomingDamage.CalcTimeHit(args.TimeCast, sender, damage.Key, args.End),
                                args.Target, false, true));
                        }
                        else if (sender.Type == GameObjectType.obj_AI_Turret)
                        {
                            Damages[damage.Key].Add(new IncomingDamage(args.SData.Name, sender, args.Start, args.End,
                                aaDamage, IncomingDamage.CalcTimeHit(args.TimeCast, sender, damage.Key, args.End),
                                args.Target, true));
                        }
                        else
                        {
                            Damages[damage.Key].Add(new IncomingDamage(args.SData.Name, sender, args.Start, args.End,
                                aaDamage, IncomingDamage.CalcTimeHit(args.TimeCast, sender, damage.Key, args.End),
                                args.Target));
                        }
                    continue;
                }
                if (sender.Type == GameObjectType.obj_AI_Hero && args.Target.NetworkId == damage.Key.NetworkId)
                {
                    try
                    {
                        double spellDamage = sender.GetSpellDamage((Obj_AI_Base) args.Target, args.SData.Name);
                        if (spellDamage != 0.0f)
                            Damages[damage.Key].Add(new IncomingDamage(args.SData.Name, sender, args.Start, args.End,
                                spellDamage, IncomingDamage.CalcTimeHit(args.TimeCast, sender, damage.Key, args.End),
                                args.Target));
                    }
                    catch (InvalidOperationException)
                    {
                        //Cannot find spell
                    }
                }
                if (sender.Type == GameObjectType.obj_AI_Turret && args.Target.NetworkId == damage.Key.NetworkId)
                    Damages[damage.Key].Add(new IncomingDamage(args.SData.Name, sender, args.Start, args.End, 300,
                        IncomingDamage.CalcTimeHit(args.TimeCast, sender, damage.Key, args.End), args.Target, true));
            }
        }

        private void GetIncomingDamage_OnGameUpdate()
        {
            DetectedSkillshots.RemoveAll(skillshot => !skillshot.IsActive());
            var tempDamages =
                new Dictionary<Obj_AI_Hero, List<IncomingDamage>>(Damages);
            foreach (var damage in Damages)
            {
                Obj_AI_Hero hero = damage.Key;

                if (hero == null || !hero.IsValid)
                    continue;

                foreach (Skillshot skillshot in DetectedSkillshots)
                {
                    if (skillshot.IsAboutToHit(50, hero))
                    {
                        try
                        {
                            double spellDamage = skillshot.Unit.GetSpellDamage((Obj_AI_Base)hero,
                                skillshot.SpellData.SpellName);
                            bool exists = false;
                            foreach (IncomingDamage incomingDamage in tempDamages[hero])
                            {
                                if (incomingDamage.SpellName.Contains(skillshot.SpellData.SpellName))
                                {
                                    exists = true;
                                    break;
                                }
                            }
                            if (spellDamage != 0.0f && !exists)
                                continue;
                            tempDamages[hero].Add(new IncomingDamage(skillshot.SpellData.SpellName, skillshot.Unit,
                                skillshot.Start.To3D(), skillshot.End.To3D(), spellDamage, Game.Time + 0.05, hero));
                        }
                        catch (InvalidOperationException)
                        {
                            //Cannot find spell
                        }
                    }
                }
                tempDamages = BuffDamage(hero, tempDamages);
            }
            Damages = tempDamages;
        }

        private static Dictionary<Obj_AI_Hero, List<IncomingDamage>> BuffDamage(Obj_AI_Hero hero,
            Dictionary<Obj_AI_Hero, List<IncomingDamage>> tempDamages) //TODO: Add Ignite
        {
            foreach (BuffInstance buff in hero.Buffs)
            {
                if (buff.Type == BuffType.Poison || buff.Type == BuffType.Damage)
                {
                    foreach (Database.Spell spell in Database.GetSpellList())
                    {
                        if (string.Equals(spell.Name, buff.DisplayName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            try
                            {
                                DamageSpell damageSpell = null;
                                Obj_AI_Hero enemy = null;
                                foreach (Obj_AI_Hero champ in ObjectManager.Get<Obj_AI_Hero>())
                                {
                                    if (champ.IsEnemy)
                                    {
                                        foreach (SpellDataInst spellDataInst in champ.Spellbook.Spells)
                                        {
                                            if (string.Equals(spellDataInst.Name, spell.Name,
                                                StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                damageSpell = Damage.Spells[champ.ChampionName].FirstOrDefault(s =>
                                                {
                                                    if (s.Slot == spellDataInst.Slot)
                                                        return 0 == s.Stage;
                                                    return false;
                                                }) ??
                                                              Damage.Spells[champ.ChampionName].FirstOrDefault(
                                                                  s => s.Slot == spellDataInst.Slot);
                                                if (damageSpell != null)
                                                {
                                                    enemy = champ;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }

                                double spellDamage = enemy.GetSpellDamage(hero, spell.Name);
                                bool exists = false;
                                foreach (IncomingDamage incomingDamage in tempDamages[hero])
                                {
                                    if (incomingDamage.SpellName.Contains(spell.Name))
                                    {
                                        exists = true;
                                        break;
                                    }
                                }
                                if (spellDamage != 0.0f && !exists)
                                    tempDamages[hero].Add(new IncomingDamage(spell.Name, enemy, new Vector3(),
                                        new Vector3(), spellDamage, buff.EndTime, hero));
                            }
                            catch (InvalidOperationException)
                            {
                                //Cannot find spell
                            }
                        }
                    }
                }
            }
            return tempDamages;
        }

        public class IncomingDamage
        {
            public double Dmg;
            public Vector3 EndPos;
            public bool Minion;
            public Obj_AI_Base Source;
            public String SpellName;
            public Vector3 StartPos;
            public GameObject Target;
            public double TimeHit;
            public bool Turret;

            public IncomingDamage(String spellName, Obj_AI_Base source, Vector3 startPos, Vector3 endPos, double dmg,
                double timeHit, GameObject target = null, bool turret = false, bool minion = false)
            {
                SpellName = spellName;
                Source = source;
                StartPos = startPos;
                EndPos = endPos;
                Dmg = dmg;
                TimeHit = timeHit;
                Target = target;
                Turret = turret;
                Minion = minion;
            }

            public static double CalcTimeHit(double extraTimeForCast, Obj_AI_Base sender, Obj_AI_Base hero,
                Vector3 endPos) //TODO: Fix Time for animations etc
            {
                return Game.Time + (extraTimeForCast/1000)*(sender.ServerPosition.Distance(endPos)/1000) +
                       (hero.ServerPosition.Distance(sender.ServerPosition)/1000);
            }

            public static double CalcTimeHit(double startTime, double extraTimeForCast, Obj_AI_Base sender,
                Obj_AI_Base hero, Vector3 endPos) //TODO: Fix Time for animations etc
            {
                return startTime + (extraTimeForCast/1000)*(sender.ServerPosition.Distance(endPos)/1000) +
                       (hero.ServerPosition.Distance(sender.ServerPosition)/1000);
            }
        }

        public class FixedSummonerCast
        {
            public static GamePacket Encoded(Packet.C2S.Cast.Struct packetStruct)
            {
                var result = new GamePacket(Packet.C2S.Cast.Header);
                result.WriteInteger(packetStruct.SourceNetworkId);
                result.WriteByte(GetSpellByte(packetStruct.Slot));
                result.WriteByte(GetFixedByte(packetStruct.Slot));
                result.WriteFloat(packetStruct.FromX);
                result.WriteFloat(packetStruct.FromY);
                result.WriteFloat(packetStruct.ToX);
                result.WriteFloat(packetStruct.ToY);
                result.WriteInteger(packetStruct.TargetNetworkId);
                return result;
            }

            private static byte GetFixedByte(SpellSlot spell)
            {
                switch (spell)
                {
                    case (SpellSlot)64:
                        return 0x00;
                    case (SpellSlot)65:
                        return 0x01;
                    default:
                        return (byte)spell;
                }
            }

            private static byte GetSpellByte(SpellSlot spell)
            {
                switch (spell)
                {
                    case SpellSlot.Q:
                        return 0xE8;
                    case SpellSlot.W:
                        return 0xE8;
                    case SpellSlot.E:
                        return 0xE8;
                    case SpellSlot.R:
                        return 0xE8;
                    case SpellSlot.Item1:
                        return 0;
                    case SpellSlot.Item2:
                        return 0;
                    case SpellSlot.Item3:
                        return 0;
                    case SpellSlot.Item4:
                        return 0;
                    case SpellSlot.Item5:
                        return 0;
                    case SpellSlot.Item6:
                        return 0;
                    case SpellSlot.Trinket:
                        return 0;
                    case SpellSlot.Recall:
                        return 0;
                    case (SpellSlot)64:
                        return 0xEF;
                    case (SpellSlot)65:
                        return 0xEF;
                    case SpellSlot.Unknown:
                        return 0;
                    default:
                        return 0;
                }
            }
        }

        

    }
}