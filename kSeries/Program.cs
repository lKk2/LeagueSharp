using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using LeagueSharp; // l# .dll's
using LeagueSharp.Common;
using SharpDX;


namespace kSeries
{
    class Program
    {
        static bool CompatibleChamp; // get champion for kSeries


        public static Orbwalking.Orbwalker Orbwalker; //Orbwalker instance

        public static List<Spell> SpellList = new List<Spell>();
        public static int x;
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        static Menu Menu;


        static void Main(string[] args)
        {
            Game.OnGameStart += Game_OnGameStart;

            if (Game.Mode == GameMode.Running)
                Game_OnGameStart(null);
        }

        static void Game_OnGameStart(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName == "Morgana")
            {
                CompatibleChamp = true;
            }
            //kSeries Menu Instance

            Q = new Spell(SpellSlot.Q, 1175);
            W = new Spell(SpellSlot.W, 900);
            R = new Spell(SpellSlot.R, 625);
            SpellList.AddRange(new []{Q, W, E, R});

            // fine tune for skill's

            Q.SetSkillshot(0.5f, 70f, 1200f, true, Prediction.SkillshotType.SkillshotLine);
            R.SetSkillshot(0.3f, 20f, 625f, false, Prediction.SkillshotType.SkillshotCircle);
            Game.PrintChat("kSeries Loaded [m0rg4n4 v.b3t4]! By Kk2");
            Menu = new Menu("kSeries", "kSeries", true);
            Menu.AddToMainMenu();

            //Orbwalker SubMenu
            Menu.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Menu.SubMenu("Orbwalking"));

            //Add the targer selector to the menu.
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Menu.AddSubMenu(targetSelectorMenu);

            //Combo Menu
            Menu.AddSubMenu(new Menu("Combo", "Combo"));
            Menu.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("MinRCombo", "Min R Targets").SetValue(new Slider(1, 5, 1)));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Harrax").SetValue(new KeyBind(32, KeyBindType.Press)));


            //TODO: Combos / Harass Menu / Drawning / KS
            

             //Add the events we are going to use
            Game.OnGameUpdate += Game_OnGameUpdate;
            //Game.OnGameUpdate += Game_OnGameUpdate;
           // Drawing.OnDraw += Drawing_OnDraw;  
        }
        private static void Game_OnGameUpdate(EventArgs args)
        {
            // Combo
            if (Menu.SubMenu("Combo").Item("ComboActive").GetValue<KeyBind>().Active)
                Combo();

        }
        
        private static void Combo()
        {
            Menu comboMenu = Menu.SubMenu("Combo");
            bool useQ = comboMenu.Item("UseQCombo").GetValue<bool>() && Q.IsReady();
            bool useW = comboMenu.Item("UseWCombo").GetValue<bool>() && W.IsReady();
            bool useR = comboMenu.Item("UseRCombo").GetValue<bool>() && R.IsReady();
            Orbwalker.SetAttacks(true);

            if (useQ)
            {
                var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
                if (target != null) 
                Q.Cast(target);
            }

            if (useW)
            {
                var target = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
                if (target != null)
                    W.Cast(target);
            }
            if (useR)
            {
                // R with Slider~
                var target = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);
                if (target != null)
                {
                    foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>())
                    {
                        if (enemy.IsValidTarget(R.Range))
                            x = x + 1;
                    }
                    if (x > comboMenu.Item("MinRCombo").GetValue<Slider>().Value)
                    {
                        R.Cast(target);
                        x = 0;
                    }
                }
                    
            }
            
        }
    }
}
