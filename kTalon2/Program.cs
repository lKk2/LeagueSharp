#region dependencies
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using LeagueSharp.Common;
using LeagueSharp;
using SharpDX;
using Color = System.Drawing.Color;
#endregion

namespace kTalon2
{
    internal class Program
    {
        private const string Champion = "Talon";
        private static readonly List<Spell> Spellist = new List<Spell>();
        private static Spell _q, _w, _e, _r;
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;
        private static Orbwalking.Orbwalker _orbwalker;
        private static Menu _config;
        private static Items.Item _tmt, _rah, _gbd;
        private static SpellSlot _igniteSlot;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != Champion)
                return;
            #region Skillshots

            _igniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");
            _tmt = new Items.Item(3077, 400f); // tiamat
            _rah = new Items.Item(3074, 400f); // hydra
            _gbd = new Items.Item(3142, 0f); // Youmuu's Ghostblade

            _q = new Spell(SpellSlot.Q, 250f);
            _w = new Spell(SpellSlot.W, 600f);
            _e = new Spell(SpellSlot.E, 700f);
            _r = new Spell(SpellSlot.R, 500f);
            

            // fine tune of spells~


            _w.SetSkillshot(5f, 0f, 902f, false, SkillshotType.SkillshotCone);
            _r.SetSkillshot(5f, 650f, 650f, false, SkillshotType.SkillshotCircle);
            Spellist.AddRange(new[] { _q, _w, _e, _r });

            #endregion

            #region Menu
            // Menu 
            _config = new Menu(Player.ChampionName, Player.ChampionName, true);
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            _config.AddSubMenu(targetSelectorMenu);

            _config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            _orbwalker = new Orbwalking.Orbwalker(_config.SubMenu("Orbwalking"));


            // Combo
            _config.AddSubMenu(new Menu("Combo", "combo"));
            _config.SubMenu("combo").AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
            _config.SubMenu("combo").AddItem(new MenuItem("useW", "Use W").SetValue(true));
            _config.SubMenu("combo").AddItem(new MenuItem("useE", "Use E").SetValue(true));
            _config.SubMenu("combo").AddItem(new MenuItem("useR", "Use R").SetValue(true));
            _config.SubMenu("combo").AddItem(new MenuItem("MinR", "Min R Targets").SetValue(new Slider(1, 1, 5)));

            
            // Harrass
            _config.AddSubMenu(new Menu("Harras", "harras"));
            _config.SubMenu("harras").AddItem(new MenuItem("QonPlayer", "Use Q").SetValue(true));
            _config.SubMenu("harras").AddItem(new MenuItem("WonPlayer", "Use W").SetValue(true));
            _config.SubMenu("harras").AddItem(new MenuItem("EonPlayer", "Use E").SetValue(false));
            _config.SubMenu("harras").AddItem(new MenuItem("ManatoHarras", "> Mana Percent to UseSKill").SetValue(new Slider(30, 0, 100)));

            // Lane Clear
            _config.AddSubMenu(new Menu("Lane Clear", "laneclear"));
            _config.SubMenu("laneclear").AddItem(new MenuItem("QonCreep", "use Q").SetValue(true));
            _config.SubMenu("laneclear").AddItem(new MenuItem("WonCreep", "use W").SetValue(true));
            _config.SubMenu("laneclear").AddItem(new MenuItem("ManatoCreep", "> Mana Percent to LaneClear").SetValue(new Slider(30, 0, 100)));

            // Last Hit
           // _config.AddSubMenu(new Menu("Last Hit", "lasthit"));
           // _config.SubMenu("lasthit").AddItem(new MenuItem("QkillCreep", "Use Q to FARM").SetValue(false)); ~ not yet
           // _config.SubMenu("lasthit").AddItem(new MenuItem("ManatoCreep", "> Mana Percent to Farm").SetValue(new Slider(30,0,100))); ~ not yet

            //Items
            _config.AddSubMenu(new Menu("Items", "items"));
            _config.SubMenu("items").AddItem(new MenuItem("useHydra", "Use Hydra").SetValue(true));
            _config.SubMenu("items").AddItem(new MenuItem("useGhost", "Use GhostBlade").SetValue(true));

            // KS
            _config.AddSubMenu(new Menu("KS", "ks"));
            _config.SubMenu("ks").AddItem(new MenuItem("RtoKill", "Use R on Killable Targets").SetValue(true));
            _config.SubMenu("ks").AddItem(new MenuItem("IgtoKill", "Use Ignite on Killable Targets").SetValue(true));

            // Drawning
            _config.AddSubMenu(new Menu("Drawning", "drawning"));
            _config.SubMenu("drawning").AddItem(new MenuItem("DrawW", "Draw W Range").SetValue(true));

            _config.AddToMainMenu(); // add everything
            #endregion

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameSendPacket += Game_OnGameSendPacket;
            Game.PrintChat("kTalon2 Loaded :}");
            
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (_config.SubMenu("drawning").Item("DrawW").GetValue<bool>())
            {
                Utility.DrawCircle(Player.Position, _w.Range, Color.Blue);    
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;
            Ks();
            if (_orbwalker.ActiveMode.ToString() == "Combo")
            {
                Combo();
            }
            if (_orbwalker.ActiveMode.ToString() == "Mixed")
            {
                Mixed();
            }

            if (_orbwalker.ActiveMode.ToString() == "LaneClear")
            {
                Clear();
            }
            if (_orbwalker.ActiveMode.ToString() == "LastHit")
            {
                //LastHit(); not yet~
            }

        }
        #region LaneClear
        private static void Clear()
        {
            if (Player.Mana/Player.MaxMana * 100 < _config.SubMenu("laneclear").Item("ManatoCreep").GetValue<Slider>().Value) return;

            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _w.Range, MinionTypes.All,
                MinionTeam.Enemy, MinionOrderTypes.MaxHealth); // not ideal at ALL need to make a MEC to calc mobs around to use W not only 1 target when have >1~

            if (mobs.Count > 0)
            {
                if (_config.SubMenu("laneclear").Item("WonCreep").GetValue<bool>() && _w.IsReady())
                    _w.Cast(_w.GetLineFarmLocation(mobs).Position.To3D());
                if (_config.SubMenu("laneclear").Item("QonCreep").GetValue<bool>() && _q.IsReady())
                    _q.Cast(mobs[0]);
                if (_config.SubMenu("items").Item("useHydra").GetValue<bool>())
                {
                    if (_rah.IsReady())
                    {
                        _rah.Cast(mobs[0]);
                    }
                    if (_tmt.IsReady())
                    {
                        _tmt.Cast(mobs[0]);
                    }
                }
            }
        }
        #endregion

        #region Harass
        private static void Mixed()
        {
            if (Player.Mana / Player.MaxMana * 100 < _config.SubMenu("harras").Item("ManatoHarras").GetValue<Slider>().Value) return;

            var target = SimpleTs.GetTarget(_w.Range, SimpleTs.DamageType.Physical);
            if (_config.SubMenu("harras").Item("WonPlayer").GetValue<bool>() && _w.IsReady())
            {
                _w.CastOnUnit(target, false);
            }
            if (_config.SubMenu("harras").Item("EonPlayer").GetValue<bool>() && _e.IsReady())
            {
                _e.Cast(target);
            }
            if (_config.SubMenu("harras").Item("QonPlayer").GetValue<bool>() && _q.IsReady())
            {
                _q.Cast(target);
            }
            if (_config.SubMenu("items").Item("useHydra").GetValue<bool>())
            {
                if (_tmt.IsReady())
                {
                    _tmt.Cast(target);
                }
                if (_rah.IsReady())
                {
                    _rah.Cast(target);
                }
            }
        }
        #endregion
        #region combo

        private static void Combo()
        {
            var target = SimpleTs.GetTarget(_w.Range, SimpleTs.DamageType.Physical);

            if (_config.SubMenu("combo").Item("useE").GetValue<bool>() && _e.IsReady())
            {
                _e.CastOnUnit(target, false);
            }
            if (_config.SubMenu("items").Item("useGhost").GetValue<bool>() && _gbd.IsReady())
                _gbd.Cast();
            if (_config.SubMenu("combo").Item("useW").GetValue<bool>() && _w.IsReady())
            {
                _w.CastOnUnit(target, false);
            }
            if (_config.SubMenu("combo").Item("useQ").GetValue<bool>() && _q.IsReady())
            {
                _q.CastOnUnit(target, false);
            }
            if (_config.SubMenu("items").Item("useHydra").GetValue<bool>())
            {
                if (_tmt.IsReady())
                {
                    _tmt.Cast(target);
                }
                if (_rah.IsReady())
                {
                    _rah.Cast(target);
                }
            }
            if (_config.SubMenu("combo").Item("useR").GetValue<bool>() && _r.IsReady() && ObjectManager.Get<Obj_AI_Hero>().Count(hero => hero.IsValidTarget(_r.Range)) >= _config.SubMenu("combo").Item("MinR").GetValue<Slider>().Value)
            {
                _r.CastOnUnit(target, false);
            }

        }
        #endregion

        #region KS

        private static void Ks()
        {
            if (_config.SubMenu("ks").Item("RtoKill").GetValue<bool>() || _config.SubMenu("ks").Item("IgtoKill").GetValue<bool>())
            {
                foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(_w.Range)))
                {
                    if (_r.IsReady() && hero.Distance(Player) <= _r.Range &&
                        DamageLib.getDmg(hero, DamageLib.SpellType.R) >= hero.Health)
                    {
                        _r.CastOnUnit(hero, false);
                    }
                    if (_igniteSlot != SpellSlot.Unknown &&
                        Player.SummonerSpellbook.CanUseSpell(_igniteSlot) == SpellState.Ready &&
                        Player.Distance(hero) < 600 && DamageLib.getDmg(hero, DamageLib.SpellType.IGNITE) > hero.Health)
                    {
                        Player.SummonerSpellbook.CastSpell(_igniteSlot, hero);
                    }
                }
            }
        }
        #endregion

        private static void Game_OnGameSendPacket(GamePacketEventArgs args) // thanks TC-CREW
        {
            if (args.PacketData[0] != Packet.C2S.Cast.Header) return;

            var decodedPacket = Packet.C2S.Cast.Decoded(args.PacketData);
            if (decodedPacket.SourceNetworkId != ObjectManager.Player.NetworkId || decodedPacket.Slot != SpellSlot.R)
                return;

            if (
                ObjectManager.Get<Obj_AI_Hero>()
                    .Count(
                        hero =>
                            hero.IsValidTarget() &&
                            hero.Distance(new Vector2(decodedPacket.ToX, decodedPacket.ToY)) <= _r.Range) == 0)
                args.Process = false;
        }
    }
}
