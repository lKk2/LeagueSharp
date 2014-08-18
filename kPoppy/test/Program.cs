using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Design;
using Color = System.Drawing.Color;


namespace kPoppy
{
    class Program
    {
        public static string Champion = "Poppy";
        public static Orbwalking.Orbwalker Orbwalker;
        public static Obj_AI_Base Player = ObjectManager.Player; // just an instance of player

        public static Spell Q, W, E, R;
        public static Items.Item DfgItem;
        public static Menu Menu;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.BaseSkinName != Champion) return; // if hero != from poppy doenst work script

            Q = new Spell(SpellSlot.Q, 250f);
            W = new Spell(SpellSlot.W, Q.Range);
            E = new Spell(SpellSlot.E, 525f);
            R = new Spell(SpellSlot.R, 600f);
            DfgItem = Utility.Map.GetMap() == Utility.Map.MapType.TwistedTreeline || Utility.Map.GetMap() == Utility.Map.MapType.CrystalScar ? new Items.Item(3188, 750) : new Items.Item(3128, 750);
           
            
            // Menu Istance With OrbWalker
            Menu = new Menu("kPoppy", "kPoppy", true);
            Menu.AddSubMenu(new Menu("OrbWalker", "OrbWalker"));
            Orbwalker = new Orbwalking.Orbwalker(Menu.SubMenu("OrbWalker"));
            //Target selector and menu
            var ts = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(ts);
            Menu.AddSubMenu(ts);

            // Combo mode
            Menu.AddSubMenu(new Menu("Combo", "Combo"));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ComboQ", "Use Q").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ComboE", "Use Smart E (STUN)").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ComboR", "Use R").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo KEY").SetValue(new KeyBind(32, KeyBindType.Press)));

            // Harass Mode
            Menu.AddSubMenu(new Menu("Harass", "Harass"));
            Menu.SubMenu("Harass").AddItem(new MenuItem("HarassQ", "Use Q").SetValue(true));
            Menu.SubMenu("Harass").AddItem(new MenuItem("HarrasActive", "Harass Key").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));

            // KS Mode
            Menu.AddSubMenu(new Menu("KS Mode", "KSMode"));
            Menu.SubMenu("KSMode").AddItem(new MenuItem("KSActive", "Use E+Q to KS").SetValue(true));


            // Jungle Mode
            Menu.AddSubMenu(new Menu("Jungle", "Jungle"));
            Menu.SubMenu("Jungle").AddItem(new MenuItem("JActive", "Jungle Farm Key").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            Menu.AddSubMenu(new Menu("Drawning", "Drawning"));
            
            Menu.AddToMainMenu(); // add this menu to main MENU at all~ 
            


            Drawing.OnDraw += Drawing_OnDraw; // Add onDraw function~
            Game.OnGameUpdate += Game_OnGameUpdate; // adds OnGameUpdate function~

            Game.PrintChat("kPoppy version 1.0 Loaded! by Kk2");
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            // Future drawning Options~
            Utility.DrawCircle(Player.Position, R.Range, Color.Blue);
        }


        static void Game_OnGameUpdate(EventArgs args)
        {
            if (Menu.Item("JActive").GetValue<KeyBind>().Active)
            {
                FarmJungle();
            }

            if (Menu.Item("HarrasActive").GetValue<KeyBind>().Active)
            {
                Harass();
            }
            if (Menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }

            var ks = Menu.Item("KSActive").GetValue<bool>();
            if (ks)
            {
                Killsteal();
            }
        }

        public static void Combo()
        {
            var useQ = Menu.Item("ComboQ").GetValue<bool>() && Q.IsReady();
            var useE = Menu.Item("ComboE").GetValue<bool>() && E.IsReady();
            var useR = Menu.Item("ComboR").GetValue<bool>() && R.IsReady();

            var target = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);
            if (target == null) return;

            if (useR)
            {
                R.Cast(target);
            }
            if (useE)
            {
                if (W.IsReady())
                {
                    W.Cast();
                }
                AutoE();
            }
            if (useQ)
            {
                Q.Cast(target);
                DfgItem.Cast(target);
            }
            
        }


        public static void FarmJungle()
        {
            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range,
                MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count > 0)
            {
                Orbwalker.SetMovement(true);
                Orbwalker.SetAttacks(true);
                if (W.IsReady())
                {
                    W.Cast();
                }
                if (E.IsReady())
                {
                    E.CastOnUnit(mobs[0]);
                }
                if (Q.IsReady())
                {
                    Q.CastOnUnit(mobs[0]);
                }

            }
        }

        public static void Harass()
        {
            var target = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);
            if (target == null) return;
            if (Menu.Item("HarassQ").GetValue<bool>() && Q.IsReady())
            {
                Q.Cast(target);
            }
        }

        private static void AutoE()
        {
            foreach (var hero in from hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(E.Range))
                                 let prediction = E.GetPrediction(hero)
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
                E.Cast(hero);
            }
        }

        private static void Killsteal()
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(E.Range)))
            {
                var dmg = DamageLib.getDmg(hero, DamageLib.SpellType.E) + DamageLib.getDmg(hero, DamageLib.SpellType.Q);
                if (E.IsReady() && Q.IsReady() && hero.Distance(ObjectManager.Player) <= E.Range && dmg >= hero.Health)
                {
                    E.CastOnUnit(hero, true);
                    Q.CastOnUnit(hero, true); 
                }
            }
        }

    }
}
