﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

namespace SAwareness.Timers
{
    class Health
    {
        public static Menu.MenuItemSettings HealthTimer = new Menu.MenuItemSettings(typeof(Health));

        private static readonly Utility.Map GMap = Utility.Map.GetMap();
        private static List<HealthObject> Healths = new List<HealthObject>();
        private int lastGameUpdateTime = 0;

        public Health()
        {
            Game.OnGameUpdate += Game_OnGameUpdate;
            InitHealthObjects();
        }

        ~Health()
        {
            Game.OnGameUpdate -= Game_OnGameUpdate;
            Healths = null;
        }

        public bool IsActive()
        {
            return Timer.Timers.GetActive() && HealthTimer.GetActive();
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            HealthTimer.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("TIMERS_HEALTH_MAIN"), "SAwarenessTimersHealth"));
            HealthTimer.MenuItems.Add(
                HealthTimer.Menu.AddItem(new MenuItem("SAwarenessTimersHealthSpeech", Language.GetString("GLOBAL_VOICE")).SetValue(false)));
            HealthTimer.MenuItems.Add(
                HealthTimer.Menu.AddItem(new MenuItem("SAwarenessTimersHealthActive", Language.GetString("GLOBAL_ACTIVE")).SetValue(false)));
            return HealthTimer;
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (!IsActive() || lastGameUpdateTime + new Random().Next(500, 1000) > Environment.TickCount)
                return;

            lastGameUpdateTime = Environment.TickCount;

            if (HealthTimer.GetActive())
            {
                HealthObject healthDestroyed = null;
                foreach (HealthObject health in Healths)
                {
                    if (health.Obj.IsValid)
                    {
                        if (health.Obj.Health > 0)
                        {
                            health.Locked = false;
                            health.NextRespawnTime = 0;
                            health.Called = false;
                        }
                        else if (health.Obj.Health < 1 && health.Locked == false)
                        {
                            health.Locked = true;
                            health.NextRespawnTime = health.RespawnTime + (int)Game.ClockTime;
                        }
                    }
                    if (health.NextRespawnTime < (int)Game.ClockTime && health.Locked)
                    {
                        healthDestroyed = health;
                    }
                }
                if (healthDestroyed != null)
                {
                    healthDestroyed.Text.Remove();
                    Healths.Remove(healthDestroyed);
                }
                foreach (Obj_AI_Minion health in ObjectManager.Get<Obj_AI_Minion>())
                {
                    HealthObject nHealth = null;
                    if (health.Name.Contains("Health"))
                    {
                        HealthObject health1 = Healths.Find(jm => jm.Obj.NetworkId == health.NetworkId);
                        if (health1 == null)
                            nHealth = new HealthObject(health);
                    }

                    if (nHealth != null)
                        Healths.Add(nHealth);
                }
            }

            /////

            if (HealthTimer.GetActive())
            {
                foreach (HealthObject health in Healths)
                {
                    if (health.Locked)
                    {
                        if (health.NextRespawnTime - (int)Game.ClockTime <= 0 || health.MapType != GMap.Type)
                            continue;
                        int time = Timer.Timers.GetMenuItem("SAwarenessTimersRemindTime").GetValue<Slider>().Value;
                        if (!health.Called && health.NextRespawnTime - (int)Game.ClockTime <= time &&
                            health.NextRespawnTime - (int)Game.ClockTime >= time - 1)
                        {
                            health.Called = true;
                            Timer.PingAndCall("Heal respawns in " + time + " seconds!", health.Position);
                            if (HealthTimer.GetMenuItem("SAwarenessTimersHealthSpeech").GetValue<bool>())
                            {
                                Speech.Speak("Heal respawns in " + time + " seconds!");
                            }
                        }
                    }
                }
            }
        }

        public void InitHealthObjects()
        {
            foreach (Obj_AI_Minion objectType in ObjectManager.Get<Obj_AI_Minion>())
            {
                if (objectType.Name.Contains("Health"))
                    Healths.Add(new HealthObject(objectType));
            }
        }

        public class HealthObject
        {
            public bool Called;
            public bool Locked;
            public Utility.Map.MapType MapType;
            public int NextRespawnTime;
            public Obj_AI_Minion Obj;
            public Vector3 Position;
            public int RespawnTime;
            public int SpawnTime;
            public Render.Text Text;

            public HealthObject()
            {

            }

            public HealthObject(Obj_AI_Minion obj)
            {
                Obj = obj;
                if (obj != null && obj.IsValid)
                    Position = obj.Position;
                else
                    Position = new Vector3();
                SpawnTime = (int)Game.ClockTime;
                RespawnTime = 40;
                NextRespawnTime = 0;
                Locked = false;
                MapType = Utility.Map.MapType.HowlingAbyss;
                Called = false;
                Text = new Render.Text(0, 0, "", Timer.Timers.GetMenuItem("SAwarenessTimersTextScale").GetValue<Slider>().Value, new ColorBGRA(Color4.White));
                Timer.Timers.GetMenuItem("SAwarenessTimersTextScale").ValueChanged += HealthObject_ValueChanged;
                Text.TextUpdate = delegate
                {
                    return (NextRespawnTime - (int)Game.ClockTime).ToString();
                };
                Text.PositionUpdate = delegate
                {
                    Vector2 sPos = Drawing.WorldToMinimap(Position);
                    return new Vector2(sPos.X, sPos.Y);
                };
                Text.VisibleCondition = sender =>
                {
                    return Timer.Timers.GetActive() && HealthTimer.GetActive() && NextRespawnTime > 0 && MapType == GMap.Type;
                };
                Text.OutLined = true;
                Text.Centered = true;
                Text.Add();
            }

            void HealthObject_ValueChanged(object sender, OnValueChangeEventArgs e)
            {
                Text.Remove();
                Text.TextFontDescription = new FontDescription
                {
                    FaceName = "Calibri",
                    Height = e.GetNewValue<Slider>().Value,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.Default,
                };
                Text.Add();
            }
        }
    }
}
