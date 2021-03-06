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
    class Altar
    {
        public static Menu.MenuItemSettings AltarTimer = new Menu.MenuItemSettings(typeof(Altar));

        private static readonly Utility.Map GMap = Utility.Map.GetMap();
        private static List<AltarObject> Altars = new List<AltarObject>();

        private int lastGameUpdateTime = 0;

        public Altar()
        {
            Game.OnGameUpdate += Game_OnGameUpdate;
            InitAltarObjects();
        }

        ~Altar()
        {
            Game.OnGameUpdate -= Game_OnGameUpdate;
            Altars = null;
        }

        public bool IsActive()
        {
            return Timer.Timers.GetActive() && AltarTimer.GetActive();
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            AltarTimer.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("TIMERS_ALTAR_MAIN"), "SAwarenessTimersAltar"));
            AltarTimer.MenuItems.Add(
                AltarTimer.Menu.AddItem(new MenuItem("SAwarenessTimersAltarSpeech", Language.GetString("GLOBAL_VOICE")).SetValue(false)));
            AltarTimer.MenuItems.Add(
                AltarTimer.Menu.AddItem(new MenuItem("SAwarenessTimersAltarActive", Language.GetString("GLOBAL_ACTIVE")).SetValue(false)));
            return AltarTimer;
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (!IsActive() || lastGameUpdateTime + new Random().Next(500, 1000) > Environment.TickCount)
                return;

            lastGameUpdateTime = Environment.TickCount;

            if (AltarTimer.GetActive())
            {
                AltarObject altarDestroyed = null;
                foreach (AltarObject altar in Altars)
                {
                    if (altar.Obj.IsValid)
                    {
                        bool hasBuff = false;
                        foreach (BuffInstance buff in altar.Obj.Buffs)
                        {
                            if (buff.Name == "treelinelanternlock")
                            {
                                hasBuff = true;
                                break;
                            }
                        }
                        if (!hasBuff)
                        {
                            altar.Locked = false;
                            altar.NextRespawnTime = 0;
                            altar.Called = false;
                        }
                        else if (hasBuff && altar.Locked == false)
                        {
                            altar.Locked = true;
                            altar.NextRespawnTime = altar.RespawnTime + (int)Game.ClockTime;
                        }
                    }
                    else
                    {
                        if (altar.NextRespawnTime < (int)Game.ClockTime)
                        {
                            altarDestroyed = altar;
                        }
                    }
                }
                if (altarDestroyed != null && Altars.Remove(altarDestroyed))
                {
                }
                foreach (Obj_AI_Minion altar in ObjectManager.Get<Obj_AI_Minion>())
                {
                    AltarObject nAltar = null;
                    if (altar.Name.Contains("Buffplat"))
                    {
                        AltarObject health1 = Altars.Find(jm => jm.Obj.NetworkId == altar.NetworkId);
                        if (health1 == null)
                            if (altar.Name.Contains("_L"))
                                nAltar = new AltarObject("Left Altar", altar);
                            else
                                nAltar = new AltarObject("Right Altar", altar);
                    }

                    if (nAltar != null)
                        Altars.Add(nAltar);
                }
            }

            /////

            if (AltarTimer.GetActive())
            {
                foreach (AltarObject altar in Altars)
                {
                    if (altar.Locked)
                    {
                        if (altar.NextRespawnTime <= 0 || altar.MapType != GMap.Type)
                            continue;
                        int time = Timer.Timers.GetMenuItem("SAwarenessTimersRemindTime").GetValue<Slider>().Value;
                        if (!altar.Called && altar.NextRespawnTime - (int)Game.ClockTime <= time &&
                            altar.NextRespawnTime - (int)Game.ClockTime >= time - 1)
                        {
                            altar.Called = true;
                            Timer.PingAndCall(altar.Name + " unlocks in " + time + " seconds!", altar.Obj.ServerPosition);
                            if (AltarTimer.GetMenuItem("SAwarenessTimersAltarSpeech").GetValue<bool>())
                            {
                                Speech.Speak(altar.Name + " unlocks in " + time + " seconds!");
                            }
                        }
                    }
                }
            }
        }

        public void InitAltarObjects()
        {
            foreach (Obj_AI_Minion objectType in ObjectManager.Get<Obj_AI_Minion>())
            {
                if (objectType.Name.Contains("Buffplat"))
                {
                    if (objectType.Name.Contains("_L"))
                        Altars.Add(new AltarObject("Left Altar", objectType));
                    else
                        Altars.Add(new AltarObject("Right Altar", objectType));
                }
            }
        }

        public class AltarObject
        {
            public bool Called;
            public String[] LockNames;
            public bool Locked;
            public Vector3 MapPosition;
            public Utility.Map.MapType MapType;
            public Vector3 MinimapPosition;
            public String Name;
            public int NextRespawnTime;
            public Obj_AI_Minion Obj;
            public GameObject ObjOld;
            public String ObjectName;
            public int RespawnTime;
            public int SpawnTime;
            public String[] UnlockNames;
            public Render.Text Text;

            public AltarObject(String name, Obj_AI_Minion obj)
            {
                Name = name;
                Obj = obj;
                SpawnTime = 185;
                RespawnTime = 90;
                Locked = false;
                NextRespawnTime = 0;
                MapType = Utility.Map.MapType.TwistedTreeline;
                Called = false;
                Text = new Render.Text(0, 0, "", Timer.Timers.GetMenuItem("SAwarenessTimersTextScale").GetValue<Slider>().Value, new ColorBGRA(Color4.White));
                Timer.Timers.GetMenuItem("SAwarenessTimersTextScale").ValueChanged += AltarObject_ValueChanged;
                Text.TextUpdate = delegate
                {
                    return (NextRespawnTime - (int)Game.ClockTime).ToString();
                };
                Text.PositionUpdate = delegate
                {
                    if (Obj.ServerPosition.Length().Equals(0.0f))
                        return new Vector2(0, 0);
                    Vector2 sPos = Drawing.WorldToMinimap(Obj.ServerPosition);
                    return new Vector2(sPos.X, sPos.Y);
                };
                Text.VisibleCondition = sender =>
                {
                    return Timer.Timers.GetActive() && AltarTimer.GetActive() && NextRespawnTime > 0 && MapType == GMap.Type && Text.X != 0;
                };
                Text.OutLined = true;
                Text.Centered = true;
                Text.Add();
            }

            void AltarObject_ValueChanged(object sender, OnValueChangeEventArgs e)
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
