#region
using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp.Common;
using LeagueSharp;
using SharpDX;
using Color = System.Drawing.Color;
#endregion

namespace kPoppy2
{
    internal class Program
    {
        private const string Champion = "Poppy";
        private static readonly List<Spell> Spellist = new List<Spell>();
        private static Spell _q, _w, _e, _r;
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;
        private static Orbwalking.Orbwalker _orbwalker;
        private static Menu _config;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != Champion)
                return;

            _q = new Spell(SpellSlot.Q, 250f);
            _w = new Spell(SpellSlot.W, _q.Range);
            _e = new Spell(SpellSlot.E, 525f);
            _r = new Spell(SpellSlot.R, 600f);

            Spellist.AddRange(new[] { _q, _w, _e, _r });

            // Menu
            _config = new Menu(Player.ChampionName, Player.ChampionName, true);
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            _config.AddSubMenu(targetSelectorMenu);

            _config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            _orbwalker = new Orbwalking.Orbwalker(_config.SubMenu("Orbwalking"));

            // Combo mode
            _config.AddSubMenu(new Menu("Combo", "Combo"));
            _config.SubMenu("Combo").AddItem(new MenuItem("ComboQ", "Use Q").SetValue(true));
            _config.SubMenu("Combo").AddItem(new MenuItem("ComboE", "Use Smart E (STUN)").SetValue(true));
            _config.SubMenu("Combo").AddItem(new MenuItem("ComboR", "Use R").SetValue(true));
            _config.SubMenu("Combo")
                .AddItem(new MenuItem("ComboActive", "Combo KEY").SetValue(new KeyBind(32, KeyBindType.Press)));

            // Harass Mode
            _config.AddSubMenu(new Menu("Harass", "Harass"));
            _config.SubMenu("Harass").AddItem(new MenuItem("HarassQ", "Use Q").SetValue(true));
            _config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarrasActive", "Harass Key").SetValue(new KeyBind("X".ToCharArray()[0],
                        KeyBindType.Press)));

            // KS Mode
            _config.AddSubMenu(new Menu("KSMode", "KSMode"));
            _config.SubMenu("KSMode").AddItem(new MenuItem("KSActive", "Use E+Q to KS").SetValue(true));


            // Jungle Mode
            _config.AddSubMenu(new Menu("Jungle", "Jungle"));
            _config.SubMenu("Jungle")
                .AddItem(
                    new MenuItem("JActive", "Jungle Farm Key").SetValue(new KeyBind("C".ToCharArray()[0],
                        KeyBindType.Press)));

            _config.AddSubMenu(new Menu("Drawning", "Drawning"));

            _config.AddToMainMenu();



            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;

            Game.PrintChat(Champion + " Loaded!");

        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            Utility.DrawCircle(Player.Position, _r.Range, Color.Blue);
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;
            _orbwalker.SetAttacks(true);
            _orbwalker.SetMovement(true);
            // calls of combos etc~
            if (_config.Item("JActive").GetValue<KeyBind>().Active)
            {
                JungleFarm();
            }
            if (_config.Item("HarrasActive").GetValue<KeyBind>().Active)
            {
                Harass();
            }
            var useRks = _config.Item("KSActive").GetValue<bool>() && _e.IsReady();
            if (_config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            if (useRks)
            {
                Killsteal();
            }
        }

        private static void JungleFarm()
        {
            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _q.Range,
                MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count > 0)
            {
                _orbwalker.SetMovement(true);
                _orbwalker.SetAttacks(true);
                if (_w.IsReady())
                {
                    _w.Cast();
                }
                if (_e.IsReady())
                {
                    _e.CastOnUnit(mobs[0]);
                }
                if (_q.IsReady())
                {
                    _q.CastOnUnit(mobs[0]);
                }

            }
        }

        private static void Harass()
        {
            var target = SimpleTs.GetTarget(_q.Range, SimpleTs.DamageType.Physical);
            if (target == null) return;
            if (_config.Item("HarassQ").GetValue<bool>() && _q.IsReady())
            {
                _q.CastOnUnit(target, true);
            }
        }

        private static void Combo()
        {
            var useQ = _config.Item("ComboQ").GetValue<bool>() && _q.IsReady();
            var useE = _config.Item("ComboE").GetValue<bool>() && _e.IsReady();
            var useR = _config.Item("ComboR").GetValue<bool>() && _r.IsReady();

            var target = SimpleTs.GetTarget(_e.Range, SimpleTs.DamageType.Physical);
            if (target == null) return;

            if (useR)
            {
                _r.Cast(target);
            }
            if (useE)
            {
                if (_w.IsReady())
                {
                    _w.Cast();
                }
                //_e.CastOnUnit(target, true);
                AutoE();
            }
            if (useQ)
            {
                _q.Cast(target);
            }
        }

        private static void Killsteal()
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(_e.Range)))
            {
                var dmg = DamageLib.getDmg(hero, DamageLib.SpellType.E) + DamageLib.getDmg(hero, DamageLib.SpellType.Q);
                if (_e.IsReady() && hero.Distance(ObjectManager.Player) <= _e.Range && dmg >= hero.Health)
                {
                    _e.CastOnUnit(hero, true);
                    _q.CastOnUnit(hero, true);
                }
            }
        }

        private static void AutoE()
        {
            foreach (var hero in from hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(_e.Range))
                                 let prediction = _e.GetPrediction(hero)
                                 where NavMesh.GetCollisionFlags(
                                     prediction.Position.To2D()
                                         .Extend(ObjectManager.Player.ServerPosition.To2D(), -300)
                                         .To3D())
                                     .HasFlag(CollisionFlags.Wall) || NavMesh.GetCollisionFlags(
                                         prediction.Position.To2D()
                                             .Extend(ObjectManager.Player.ServerPosition.To2D(),
                                                 -(300 / 2))
                                             .To3D())
                                         .HasFlag(CollisionFlags.Wall)
                                 select hero)
            {
                _e.Cast(hero);
            }
        }

    }
}      
    

