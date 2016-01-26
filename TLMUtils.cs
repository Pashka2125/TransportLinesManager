using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using TLMCW = Klyte.TransportLinesManager.TLMConfigWarehouse;

namespace Klyte.TransportLinesManager
{
    public class TLMLineUtils
    {
        public static void RemoveAllFromLine(ushort lineId)
        {
            var line = Singleton<TransportManager>.instance.m_lines.m_buffer[(int)lineId];
            VehicleManager instance = Singleton<VehicleManager>.instance;
            ushort num2 = line.m_vehicles;
            int num3 = 0;
            while (num2 != 0)
            {
                ushort nextLineVehicle = instance.m_vehicles.m_buffer[(int)num2].m_nextLineVehicle;
                VehicleInfo info2 = instance.m_vehicles.m_buffer[(int)num2].Info;
                info2.m_vehicleAI.SetTransportLine(num2, ref instance.m_vehicles.m_buffer[(int)num2], 0);
                num2 = nextLineVehicle;
                if (++num3 > 16384)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
        }

        public static void getLineNamingParameters(ushort lineIdx, out ModoNomenclatura prefix, out Separador s, out ModoNomenclatura suffix, out bool zeros, out bool invertPrefixSuffix)
        {
            string nil;
            getLineNamingParameters(lineIdx, out prefix, out s, out suffix, out zeros, out invertPrefixSuffix, out nil);

        }

        public static ItemClass.SubService getLineNamingParameters(ushort lineIdx, out ModoNomenclatura prefix, out Separador s, out ModoNomenclatura suffix, out bool zeros, out bool invertPrefixSuffix, out string icon)
        {
            TLMCW.ConfigIndex transportType = TLMCW.getConfigIndexForLine(lineIdx);

            suffix = (ModoNomenclatura)TLMCW.getCurrentConfigInt(transportType | TLMCW.ConfigIndex.SUFFIX);
            s = (Separador)TLMCW.getCurrentConfigInt(transportType | TLMCW.ConfigIndex.SEPARATOR);
            prefix = (ModoNomenclatura)TLMCW.getCurrentConfigInt(transportType | TLMCW.ConfigIndex.PREFIX);
            zeros = TLMCW.getCurrentConfigBool(transportType | TLMCW.ConfigIndex.LEADING_ZEROS);
            invertPrefixSuffix = TLMCW.getCurrentConfigBool(transportType | TLMCW.ConfigIndex.INVERT_PREFIX_SUFFIX);
            switch (transportType)
            {
                case TLMCW.ConfigIndex.TRAM_CONFIG:
                    icon = "TramIcon";
                    return ItemClass.SubService.PublicTransportTrain;
                case TLMCW.ConfigIndex.BULLET_TRAIN_CONFIG:
                    icon = "BulletTrainIcon";
                    return ItemClass.SubService.PublicTransportTrain;
                case TLMCW.ConfigIndex.TRAIN_CONFIG:
                    icon = "TrainIcon";
                    return ItemClass.SubService.PublicTransportTrain;
                case TLMCW.ConfigIndex.METRO_CONFIG:
                    icon = "SubwayIcon";
                    return ItemClass.SubService.PublicTransportMetro;
                case TLMCW.ConfigIndex.BUS_CONFIG:
                    icon = "BusIcon";
                    return ItemClass.SubService.PublicTransportBus;
                case TLMCW.ConfigIndex.LOW_BUS_CONFIG:
                    icon = "LowBusIcon";
                    return ItemClass.SubService.PublicTransportBus;
                case TLMCW.ConfigIndex.HIGH_BUS_CONFIG:
                    icon = "HighBusIcon";
                    return ItemClass.SubService.PublicTransportBus;
                default:
                    icon = "BusIcon";
                    return ItemClass.SubService.None;
            }
        }


        /// <summary>
        /// </summary>
        /// <returns><c>true</c>, if recusive search for near stops was ended, <c>false</c> otherwise.</returns>
        /// <param name="pos">Position.</param>
        /// <param name="maxDistance">Max distance.</param>
        /// <param name="linesFound">Lines found.</param>
        public static bool GetNearLines(Vector3 pos, float maxDistance, ref List<ushort> linesFound)
        {
            int num = Mathf.Max((int)((pos.x - maxDistance) / 64f + 135f), 0);
            int num2 = Mathf.Max((int)((pos.z - maxDistance) / 64f + 135f), 0);
            int num3 = Mathf.Min((int)((pos.x + maxDistance) / 64f + 135f), 269);
            int num4 = Mathf.Min((int)((pos.z + maxDistance) / 64f + 135f), 269);
            bool noneFound = true;
            NetManager nm = Singleton<NetManager>.instance;
            TransportManager tm = Singleton<TransportManager>.instance;
            for (int i = num2; i <= num4; i++)
            {
                for (int j = num; j <= num3; j++)
                {
                    ushort num6 = nm.m_nodeGrid[i * 270 + j];
                    int num7 = 0;
                    while (num6 != 0)
                    {
                        NetInfo info = nm.m_nodes.m_buffer[(int)num6].Info;

                        if ((info.m_class.m_service == ItemClass.Service.PublicTransport))
                        {
                            ushort transportLine = nm.m_nodes.m_buffer[(int)num6].m_transportLine;
                            if (transportLine != 0 && TLMCW.getCurrentConfigBool(TLMCW.getConfigIndexForLine(transportLine) | TLMConfigWarehouse.ConfigIndex.SHOW_IN_LINEAR_MAP))
                            {
                                TransportInfo info2 = tm.m_lines.m_buffer[(int)transportLine].Info;
                                if (!linesFound.Contains(transportLine) && (tm.m_lines.m_buffer[(int)transportLine].m_flags & TransportLine.Flags.Temporary) == TransportLine.Flags.None)
                                {
                                    float num8 = Vector3.SqrMagnitude(pos - nm.m_nodes.m_buffer[(int)num6].m_position);
                                    if (num8 < maxDistance * maxDistance)
                                    {
                                        linesFound.Add(transportLine);
                                        GetNearLines(nm.m_nodes.m_buffer[(int)num6].m_position, maxDistance, ref linesFound);
                                        noneFound = false;
                                    }
                                }
                            }
                        }

                        num6 = nm.m_nodes.m_buffer[(int)num6].m_nextGridNode;
                        if (++num7 >= 32768)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
            return noneFound;
        }
        //GetNearStopPoints
        public static bool GetNearStopPoints(Vector3 pos, float maxDistance, ref List<ushort> stopsFound, int depth = 0)
        {
            if (depth >= 4) return false;
            int num = Mathf.Max((int)((pos.x - maxDistance) / 64f + 135f), 0);
            int num2 = Mathf.Max((int)((pos.z - maxDistance) / 64f + 135f), 0);
            int num3 = Mathf.Min((int)((pos.x + maxDistance) / 64f + 135f), 269);
            int num4 = Mathf.Min((int)((pos.z + maxDistance) / 64f + 135f), 269);
            bool noneFound = true;
            NetManager nm = Singleton<NetManager>.instance;
            TransportManager tm = Singleton<TransportManager>.instance;
            for (int i = num2; i <= num4; i++)
            {
                for (int j = num; j <= num3; j++)
                {
                    ushort stopId = nm.m_nodeGrid[i * 270 + j];
                    int num7 = 0;
                    while (stopId != 0)
                    {
                        NetInfo info = nm.m_nodes.m_buffer[(int)stopId].Info;

                        if ((info.m_class.m_service == ItemClass.Service.PublicTransport) &&
                            ((info.m_class.m_subService == ItemClass.SubService.PublicTransportTrain)
                            || (info.m_class.m_subService == ItemClass.SubService.PublicTransportMetro)))
                        {
                            ushort transportLine = nm.m_nodes.m_buffer[(int)stopId].m_transportLine;
                            if (transportLine != 0)
                            {
                                if (!stopsFound.Contains(stopId))
                                {
                                    float num8 = Vector3.SqrMagnitude(pos - nm.m_nodes.m_buffer[(int)stopId].m_position);
                                    if (num8 < maxDistance * maxDistance)
                                    {
                                        stopsFound.Add(stopId);
                                        GetNearStopPoints(nm.m_nodes.m_buffer[(int)stopId].m_position, maxDistance, ref stopsFound, depth + 1);
                                        noneFound = false;
                                    }
                                }
                            }
                        }

                        stopId = nm.m_nodes.m_buffer[(int)stopId].m_nextGridNode;
                        if (++num7 >= 32768)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
            return noneFound;
        }

        public static Vector2 gridPositionGameDefault(Vector3 pos)
        {
            int x = Mathf.Max((int)((pos.x) / 64f + 135f), 0);
            int z = Mathf.Max((int)((-pos.z) / 64f + 135f), 0);
            return new Vector2(x, z);
        }


        public static Vector2 gridPosition81Tiles(Vector3 pos)
        {
            int x = Mathf.Max((int)((pos.x) / 64f + 243f), 0);
            int z = Mathf.Max((int)((-pos.z) / 64f + 243f), 0);
            return new Vector2(x, z);
        }

        /// <summary>
        /// Index the lines.
        /// </summary>
        /// <returns>The lines indexed.</returns>
        /// <param name="intersections">Intersections.</param>
        /// <param name="t">Transport line to ignore.</param>
        public static Dictionary<string, ushort> IndexLines(List<ushort> intersections, TransportLine t = default(TransportLine))
        {
            TransportManager tm = Singleton<TransportManager>.instance;
            Dictionary<String, ushort> otherLinesIntersections = new Dictionary<String, ushort>();
            foreach (ushort s in intersections)
            {
                TransportLine tl = tm.m_lines.m_buffer[(int)s];
                if (t.Equals(default(TransportLine)) || tl.Info.GetSubService() != t.Info.GetSubService() || tl.m_lineNumber != t.m_lineNumber)
                {
                    string transportTypeLetter = "";
                    switch (TLMCW.getConfigIndexForLine(s))
                    {
                        case TLMConfigWarehouse.ConfigIndex.BUS_CONFIG:
                            transportTypeLetter = "G";
                            break;
                        case TLMConfigWarehouse.ConfigIndex.HIGH_BUS_CONFIG:
                            transportTypeLetter = "F";
                            break;
                        case TLMConfigWarehouse.ConfigIndex.LOW_BUS_CONFIG:
                            transportTypeLetter = "H";
                            break;
                        case TLMConfigWarehouse.ConfigIndex.METRO_CONFIG:
                            transportTypeLetter = "E";
                            break;
                        case TLMConfigWarehouse.ConfigIndex.TRAIN_CONFIG:
                            transportTypeLetter = "C";
                            break;
                        case TLMConfigWarehouse.ConfigIndex.TRAM_CONFIG:
                            transportTypeLetter = "D";
                            break;
                        case TLMConfigWarehouse.ConfigIndex.BULLET_TRAIN_CONFIG:
                            transportTypeLetter = "B";
                            break;
                    }
                    otherLinesIntersections.Add(transportTypeLetter + tl.m_lineNumber.ToString().PadLeft(5, '0'), s);
                }
            }
            return otherLinesIntersections;
        }

        public static void PrintIntersections(string airport, string port, string taxi, UIPanel intersectionsPanel, Dictionary<string, ushort> otherLinesIntersections, float scale = 1.0f, int maxItemsForSizeSwap = 3)
        {
            TransportManager tm = Singleton<TransportManager>.instance;

            int intersectionCount = otherLinesIntersections.Count;
            if (!String.IsNullOrEmpty(airport))
            {
                intersectionCount++;
            }
            if (!String.IsNullOrEmpty(port))
            {
                intersectionCount++;
            }
            if (!String.IsNullOrEmpty(taxi))
            {
                intersectionCount++;
            }
            float size = scale * (intersectionCount > maxItemsForSizeSwap ? 20 : 40);
            float multiplier = scale * (intersectionCount > maxItemsForSizeSwap ? 0.4f : 0.8f);
            foreach (var s in otherLinesIntersections.OrderBy(x => x.Key))
            {
                TransportLine intersectLine = tm.m_lines.m_buffer[(int)s.Value];
                String bgSprite;
                ModoNomenclatura sufixo, prefixo;
                Separador separador;
                bool zeros;
                bool invertPrefixSuffix;
                ItemClass.SubService ss = getLineNamingParameters(s.Value, out prefixo, out separador, out sufixo, out zeros, out invertPrefixSuffix, out bgSprite);
                UIButtonLineInfo lineCircleIntersect = null;
                TLMUtils.createUIElement<UIButtonLineInfo>(ref lineCircleIntersect, intersectionsPanel.transform);
                lineCircleIntersect.autoSize = false;
                lineCircleIntersect.width = size;
                lineCircleIntersect.height = size;
                lineCircleIntersect.color = intersectLine.m_color;
                lineCircleIntersect.pivot = UIPivotPoint.MiddleLeft;
                lineCircleIntersect.verticalAlignment = UIVerticalAlignment.Middle;
                lineCircleIntersect.name = "LineFormat";
                lineCircleIntersect.relativePosition = new Vector3(0f, 0f);
                lineCircleIntersect.atlas = TLMController.taLineNumber;
                lineCircleIntersect.normalBgSprite = bgSprite;
                lineCircleIntersect.hoveredColor = Color.white;
                lineCircleIntersect.hoveredTextColor = Color.red;
                lineCircleIntersect.lineID = s.Value;
                lineCircleIntersect.tooltip = tm.GetLineName(s.Value);
                lineCircleIntersect.eventClick += TLMController.instance.lineInfoPanel.openLineInfo;
                UILabel lineNumberIntersect = null;
                TLMUtils.createUIElement<UILabel>(ref lineNumberIntersect, lineCircleIntersect.transform);
                lineNumberIntersect.autoSize = false;
                lineNumberIntersect.autoHeight = false;
                lineNumberIntersect.width = lineCircleIntersect.width;
                lineNumberIntersect.pivot = UIPivotPoint.MiddleCenter;
                lineNumberIntersect.textAlignment = UIHorizontalAlignment.Center;
                lineNumberIntersect.verticalAlignment = UIVerticalAlignment.Middle;
                lineNumberIntersect.name = "LineNumber";
                lineNumberIntersect.height = size;
                lineNumberIntersect.relativePosition = new Vector3(-0.5f, 0.5f);
                lineNumberIntersect.textColor = Color.white;
                lineNumberIntersect.outlineColor = Color.black;
                lineNumberIntersect.useOutline = true;
                bool day, night;
                intersectLine.GetActive(out day, out night);
                if (!day || !night)
                {
                    UILabel daytimeIndicator = null;
                    TLMUtils.createUIElement<UILabel>(ref daytimeIndicator, lineCircleIntersect.transform);
                    daytimeIndicator.autoSize = false;
                    daytimeIndicator.width = size;
                    daytimeIndicator.height = size;
                    daytimeIndicator.color = Color.white;
                    daytimeIndicator.pivot = UIPivotPoint.MiddleLeft;
                    daytimeIndicator.verticalAlignment = UIVerticalAlignment.Middle;
                    daytimeIndicator.name = "LineTime";
                    daytimeIndicator.relativePosition = new Vector3(0f, 0f);
                    daytimeIndicator.atlas = TLMController.taLineNumber;
                    daytimeIndicator.backgroundSprite = day ? "DayIcon" : night ? "NightIcon" : "DisabledIcon";
                }
                setLineNumberCircleOnRef(intersectLine.m_lineNumber, prefixo, separador, sufixo, zeros, lineNumberIntersect, invertPrefixSuffix);
                lineNumberIntersect.textScale *= multiplier;
                lineNumberIntersect.relativePosition *= multiplier;
            }
            if (airport != string.Empty)
            {
                addExtraStationBuildingIntersection(intersectionsPanel, size, "AirplaneIcon", airport);
            }
            if (port != string.Empty)
            {
                addExtraStationBuildingIntersection(intersectionsPanel, size, "ShipIcon", port);
            }
            if (taxi != string.Empty)
            {
                addExtraStationBuildingIntersection(intersectionsPanel, size, "TaxiIcon", taxi);
            }
        }

        private static void addExtraStationBuildingIntersection(UIComponent parent, float size, string bgSprite, string description)
        {
            UILabel lineCircleIntersect = null;
            TLMUtils.createUIElement<UILabel>(ref lineCircleIntersect, parent.transform);
            lineCircleIntersect.autoSize = false;
            lineCircleIntersect.width = size;
            lineCircleIntersect.height = size;
            lineCircleIntersect.pivot = UIPivotPoint.MiddleLeft;
            lineCircleIntersect.verticalAlignment = UIVerticalAlignment.Middle;
            lineCircleIntersect.name = "LineFormat";
            lineCircleIntersect.relativePosition = new Vector3(0f, 0f);
            lineCircleIntersect.atlas = TLMController.taLineNumber;
            lineCircleIntersect.backgroundSprite = bgSprite;
            lineCircleIntersect.tooltip = description;
        }

        public static void setLineNumberCircleOnRef(int num, ModoNomenclatura prefix, Separador s, ModoNomenclatura sufix, bool zeros, UILabel reference, bool invertPrefixSuffix, float ratio = 1f)
        {
            reference.text = TLMUtils.getString(prefix, s, sufix, num, zeros, invertPrefixSuffix);
            int lenght = reference.text.Length;
            if (lenght >= 4)
            {
                reference.textScale = 1f * ratio;
                reference.relativePosition = new Vector3(0f, 1f);
            }
            else if (lenght == 3)
            {
                reference.textScale = 1.25f * ratio;
                reference.relativePosition = new Vector3(0f, 1.5f);
            }
            else if (lenght == 2)
            {
                reference.textScale = 1.75f * ratio;
                reference.relativePosition = new Vector3(-0.5f, 0.5f);
            }
            else {
                reference.textScale = 2.3f * ratio;
                reference.relativePosition = new Vector3(-0.5f, 0f);
            }
        }


    }

    public class TLMUtils
    {
        public static void doLog(string format, params object[] args)
        {
            if (TransportLinesManagerMod.debugMode)
            {
                Debug.LogWarningFormat("TLMv" + TransportLinesManagerMod.majorVersion + " " + format, args);
            }
        }
        public static void createUIElement<T>(ref T uiItem, Transform parent) where T : Component
        {
            GameObject container = new GameObject();
            container.transform.parent = parent;
            uiItem = container.AddComponent<T>();
        }

        public static void uiTextFieldDefaults(UITextField uiItem)
        {
            uiItem.selectionSprite = "EmptySprite";
            uiItem.useOutline = true;
            uiItem.hoveredBgSprite = "TextFieldPanelHovered";
            uiItem.focusedBgSprite = "TextFieldPanel";
            uiItem.builtinKeyNavigation = true;
            uiItem.submitOnFocusLost = true;
        }

        public static Color contrastColor(Color color)
        {
            int d = 0;

            // Counting the perceptive luminance - human eye favors green color... 
            double a = (0.299 * color.r + 0.587 * color.g + 0.114 * color.b);

            if (a > 0.5)
                d = 0; // bright colors - black font
            else
                d = 1; // dark colors - white font

            return new Color(d, d, d, 1);
        }

        public static float calcBezierLenght(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float precision)
        {

            Vector3 aa = (-a + 3 * (b - c) + d);
            Vector3 bb = 3 * (a + c) - 6 * b;
            Vector3 cc = 3 * (b - a);

            int len = (int)(1.0f / precision);
            float[] arcLengths = new float[len + 1];
            arcLengths[0] = 0;

            Vector3 ov = a;
            Vector3 v;
            float clen = 0.0f;
            for (int i = 1; i <= len; i++)
            {
                float t = (i * precision);
                v = ((aa * t + (bb)) * t + cc) * t + a;
                clen += (ov - v).magnitude;
                arcLengths[i] = clen;
                ov = v;
            }
            return clen;

        }

        public static void createDragHandle(UIComponent parent, UIComponent target)
        {
            createDragHandle(parent, target, -1);
        }

        public static void createDragHandle(UIComponent parent, UIComponent target, float height)
        {
            UIDragHandle dh = null;
            createUIElement<UIDragHandle>(ref dh, parent.transform);
            dh.target = target;
            dh.relativePosition = new Vector3(0, 0);
            dh.width = parent.width;
            dh.height = height < 0 ? parent.height : height;
            dh.name = "DragHandle";
            dh.Start();
        }

        public static void initButton(UIButton button, bool isCheck, string baseSprite)
        {
            string sprite = baseSprite;//"ButtonMenu";
            string spriteHov = baseSprite + "Hovered";
            button.normalBgSprite = sprite;
            button.disabledBgSprite = sprite + "Disabled";
            button.hoveredBgSprite = spriteHov;
            button.focusedBgSprite = spriteHov;
            button.pressedBgSprite = isCheck ? sprite + "Pressed" : spriteHov;
            button.textColor = new Color32(255, 255, 255, 255);
        }

        public static void initButtonSameSprite(UIButton button, string baseSprite)
        {
            string sprite = baseSprite;//"ButtonMenu";
            button.normalBgSprite = sprite;
            button.disabledBgSprite = sprite;
            button.hoveredBgSprite = sprite;
            button.focusedBgSprite = sprite;
            button.pressedBgSprite = sprite;
            button.textColor = new Color32(255, 255, 255, 255);
        }

        public static void initButtonFg(UIButton button, bool isCheck, string baseSprite)
        {
            string sprite = baseSprite;//"ButtonMenu";
            string spriteHov = baseSprite + "Hovered";
            button.normalFgSprite = sprite;
            button.disabledFgSprite = sprite;
            button.hoveredFgSprite = spriteHov;
            button.focusedFgSprite = spriteHov;
            button.pressedFgSprite = isCheck ? sprite + "Pressed" : spriteHov;
            button.textColor = new Color32(255, 255, 255, 255);
        }

        public static void copySpritesEvents(UIButton source, UIButton target)
        {
            target.disabledBgSprite = source.disabledBgSprite;
            target.focusedBgSprite = source.focusedBgSprite;
            target.hoveredBgSprite = source.hoveredBgSprite;
            target.normalBgSprite = source.normalBgSprite;
            target.pressedBgSprite = source.pressedBgSprite;

            target.disabledFgSprite = source.disabledFgSprite;
            target.focusedFgSprite = source.focusedFgSprite;
            target.hoveredFgSprite = source.hoveredFgSprite;
            target.normalFgSprite = source.normalFgSprite;
            target.pressedFgSprite = source.pressedFgSprite;

        }

        public static string[] getStringOptionsForPrefix(ModoNomenclatura m)
        {
            List<string> saida = new List<string>(new string[] { "" });

            switch (m)
            {
                case ModoNomenclatura.GregoMaiusculo:
                    saida.AddRange(gregoMaiusculo.Select(x => x.ToString()));
                    break;
                case ModoNomenclatura.GregoMinusculo:
                    saida.AddRange(gregoMinusculo.Select(x => x.ToString()));
                    break;
                case ModoNomenclatura.CirilicoMaiusculo:
                    saida.AddRange(cirilicoMaiusculo.Select(x => x.ToString()));
                    break;
                case ModoNomenclatura.CirilicoMinusculo:
                    saida.AddRange(cirilicoMinusculo.Select(x => x.ToString()));
                    break;
                case ModoNomenclatura.LatinoMaiusculo:
                    saida.AddRange(latinoMaiusculo.Select(x => x.ToString()));
                    break;
                case ModoNomenclatura.LatinoMinusculo:
                    saida.AddRange(latinoMinusculo.Select(x => x.ToString()));
                    break;
                case ModoNomenclatura.Numero:
                    for (int i = 1; i <= 64; i++)
                    {
                        saida.Add(i.ToString());
                    }
                    break;
            }
            if (TLMUtils.nomenclaturasComNumeros.Contains(m))
            {
                saida.AddRange(numeros.Select(x => x.ToString()));
            }
            return saida.ToArray();
        }

        public static string getString(ModoNomenclatura prefixo, Separador s, ModoNomenclatura sufixo, int numero, bool leadingZeros, bool invertPrefixSuffix)
        {
            string prefixoSaida = "";
            string separadorSaida = "";
            string sufixoSaida = "";
            if (prefixo != ModoNomenclatura.Nenhum)
            {
                prefixoSaida = getStringFromNumber(getStringOptionsForPrefix(prefixo), numero / 1000 + 1);
                numero = numero % 1000;
            }

            if (numero > 0)
            {
                if (prefixoSaida != "" && s != Separador.Nenhum)
                {
                    switch (s)
                    {
                        case Separador.Barra:
                            separadorSaida = "/";
                            break;
                        case Separador.Espaco:
                            separadorSaida = " ";
                            break;
                        case Separador.Hifen:
                            separadorSaida = "-";
                            break;
                        case Separador.Ponto:
                            separadorSaida = ".";
                            break;
                        case Separador.QuebraLinha:
                            separadorSaida = "\n";
                            break;
                    }
                }
                switch (sufixo)
                {
                    case ModoNomenclatura.GregoMaiusculo:
                        sufixoSaida = getStringFromNumber(gregoMaiusculo, numero);
                        break;
                    case ModoNomenclatura.GregoMinusculo:
                        sufixoSaida = getStringFromNumber(gregoMinusculo, numero);
                        break;
                    case ModoNomenclatura.CirilicoMaiusculo:
                        sufixoSaida = getStringFromNumber(cirilicoMaiusculo, numero);
                        break;
                    case ModoNomenclatura.CirilicoMinusculo:
                        sufixoSaida = getStringFromNumber(cirilicoMinusculo, numero);
                        break;
                    case ModoNomenclatura.LatinoMaiusculo:
                        sufixoSaida = getStringFromNumber(latinoMaiusculo, numero);
                        break;
                    case ModoNomenclatura.LatinoMinusculo:
                        sufixoSaida = getStringFromNumber(latinoMinusculo, numero);
                        break;
                    default:
                        if (leadingZeros && prefixoSaida != "")
                        {
                            sufixoSaida = numero.ToString("D3");
                        }
                        else {
                            sufixoSaida = numero.ToString();
                        }
                        break;
                }

                if (invertPrefixSuffix && sufixo == ModoNomenclatura.Numero)
                {
                    return sufixoSaida + separadorSaida + prefixoSaida;
                }
                else
                {
                    return prefixoSaida + separadorSaida + sufixoSaida;
                }
            }
            else {
                return prefixoSaida;
            }
        }

        private static string getString(ModoNomenclatura m, int numero)
        {

            switch (m)
            {
                case ModoNomenclatura.GregoMaiusculo:
                    return getStringFromNumber(gregoMaiusculo, numero);
                case ModoNomenclatura.GregoMinusculo:
                    return getStringFromNumber(gregoMinusculo, numero);
                case ModoNomenclatura.CirilicoMaiusculo:
                    return getStringFromNumber(cirilicoMaiusculo, numero);
                case ModoNomenclatura.CirilicoMinusculo:
                    return getStringFromNumber(cirilicoMinusculo, numero);
                case ModoNomenclatura.LatinoMaiusculo:
                    return getStringFromNumber(latinoMaiusculo, numero);
                case ModoNomenclatura.LatinoMinusculo:
                    return getStringFromNumber(latinoMinusculo, numero);
                default:
                    return "" + numero;
            }
        }

        public static string getStringFromNumber(string[] array, int number)
        {
            int arraySize = array.Length;
            string saida = "";
            while (number > 0)
            {
                int idx = (number - 1) % arraySize;
                saida = "" + array[idx] + saida;
                if (number % arraySize == 0)
                {
                    number /= arraySize;
                    number--;
                }
                else {
                    number /= arraySize;
                }

            }
            return saida;
        }

        public static void setLineColor(ushort lineIdx, Color color)
        {

            Singleton<TransportManager>.instance.m_lines.m_buffer[(int)lineIdx].m_color = color;
            Singleton<TransportManager>.instance.m_lines.m_buffer[(int)lineIdx].m_flags |= TransportLine.Flags.CustomColor;
        }

        public static void setLineName(ushort lineIdx, string name)
        {
            InstanceID lineIdSelecionado = default(InstanceID);
            lineIdSelecionado.TransportLine = lineIdx;
            if (name.Length > 0)
            {
                Singleton<InstanceManager>.instance.SetName(lineIdSelecionado, name);
                Singleton<TransportManager>.instance.m_lines.m_buffer[(int)lineIdx].m_flags |= TransportLine.Flags.CustomName;
            }
            else {
                Singleton<TransportManager>.instance.m_lines.m_buffer[(int)lineIdx].m_flags &= ~TransportLine.Flags.CustomName;
            }
        }

        public static string calculateAutoName(ushort lineIdx)
        {
            TransportManager tm = Singleton<TransportManager>.instance;
            TransportLine t = tm.m_lines.m_buffer[(int)lineIdx];
            ItemClass.SubService ss = ItemClass.SubService.None;
            if (t.Info.m_transportType == TransportInfo.TransportType.Train)
            {
                ss = ItemClass.SubService.PublicTransportTrain;
            }
            else if (t.Info.m_transportType == TransportInfo.TransportType.Metro)
            {
                ss = ItemClass.SubService.PublicTransportMetro;
            }
            int stopsCount = t.CountStops(lineIdx);
            ushort[] stopBuildings = new ushort[stopsCount];
            MultiMap<ushort, Vector3> bufferToDraw = new MultiMap<ushort, Vector3>();
            int middle;
            if (t.Info.m_transportType != TransportInfo.TransportType.Bus && CalculateSimmetry(ss, stopsCount, t, out middle))
            {
                ItemClass.Service nil;
                ItemClass.SubService nil2;
                return getStationName(t.GetStop(middle), ss, out nil, out nil2) + " - " + getStationName(t.GetStop(middle + stopsCount / 2), ss, out nil, out nil2);
            }
            else
            {
                float autoNameSimmetryImprecision = 0.075f;
                DistrictManager dm = Singleton<DistrictManager>.instance;
                Dictionary<int, KeyValuePair<TLMCW.ConfigIndex, String>> stationsList = new Dictionary<int, KeyValuePair<TLMCW.ConfigIndex, String>>();
                NetManager nm = Singleton<NetManager>.instance;
                for (int j = 0; j < stopsCount; j++)
                {
                    ItemClass.Service service;
                    ItemClass.SubService subservice;
                    String value = getStationName(t.GetStop(j), ss, out service, out subservice, true);
                    stationsList.Add(j, new KeyValuePair<TLMCW.ConfigIndex, string>(subservice.toConfigIndex() != 0 ? subservice.toConfigIndex() : service.toConfigIndex(), value));
                }
                uint mostImportantCategoryInLine = stationsList.Select(x => (x.Value.Key).getPriority()).Min();
                if (mostImportantCategoryInLine < int.MaxValue)
                {
                    var mostImportantPlaceIdx = stationsList.Where(x => x.Value.Key.getPriority() == mostImportantCategoryInLine).Min(x => x.Key);
                    var destiny = stationsList[mostImportantPlaceIdx];

                    var inverseIdxCenter = (mostImportantPlaceIdx + stopsCount / 2) % stopsCount;
                    int simmetryMargin = (int)Math.Ceiling(stopsCount * autoNameSimmetryImprecision);
                    int resultIdx = -1;
                    var destBuilding = getStationBuilding((uint)mostImportantPlaceIdx, ss);
                    BuildingManager bm = Singleton<BuildingManager>.instance;
                    for (int i = 0; i <= simmetryMargin; i++)
                    {
                        int currentI = (inverseIdxCenter + i + stopsCount) % stopsCount;
                        var iBuilding = getStationBuilding((uint)currentI, ss);
                        if ((resultIdx == -1 || stationsList[currentI].Key.getPriority() < stationsList[resultIdx].Key.getPriority())
                            && iBuilding != destBuilding
                            && (!stationsList[currentI].Key.isPublicTransport() || bm.m_buildings.m_buffer[currentI].Info.GetAI().GetType() == typeof(TransportStationAI)))
                        {
                            resultIdx = currentI;
                        }
                        if (i == 0) continue;
                        currentI = (inverseIdxCenter - i + stopsCount) % stopsCount;
                        iBuilding = getStationBuilding((uint)currentI, ss);
                        if ((resultIdx == -1 || stationsList[currentI].Key.getPriority() < stationsList[resultIdx].Key.getPriority())
                            && iBuilding != destBuilding
                            && (!stationsList[currentI].Key.isPublicTransport() || bm.m_buildings.m_buffer[currentI].Info.GetAI().GetType() == typeof(TransportStationAI)))
                        {
                            resultIdx = currentI;
                        }
                    }
                    string originName = "";
                    int districtOriginId = -1;
                    if (resultIdx >= 0 && stationsList[resultIdx].Key.isLineNamingEnabled())
                    {
                        var origin = stationsList[resultIdx];
                        originName = origin.Key.getPrefixTextNaming().Trim() + (origin.Key.getPrefixTextNaming().Trim() != string.Empty ? " " : "") + origin.Value + " - ";
                    }
                    else
                    {
                        NetNode nn = nm.m_nodes.m_buffer[t.GetStop((int)resultIdx)];
                        Vector3 location = nn.m_position;
                        districtOriginId = dm.GetDistrict(location);
                        if (districtOriginId > 0)
                        {
                            District d = dm.m_districts.m_buffer[districtOriginId];
                            originName = dm.GetDistrictName(districtOriginId) + " - ";
                        }
                        else {
                            originName = "";
                        }
                    }
                    if (!destiny.Key.isLineNamingEnabled())
                    {
                        NetNode nn = nm.m_nodes.m_buffer[t.GetStop((int)resultIdx)];
                        Vector3 location = nn.m_position;
                        int districtDestinyId = dm.GetDistrict(location);
                        if (districtDestinyId == districtOriginId)
                        {
                            District d = dm.m_districts.m_buffer[districtDestinyId];
                            return (TLMCW.getCurrentConfigBool(TLMCW.ConfigIndex.CIRCULAR_IN_SINGLE_DISTRICT_LINE) ? "Circular " : "") + dm.GetDistrictName(districtDestinyId);
                        }
                        else if (districtDestinyId > 0)
                        {
                            District d = dm.m_districts.m_buffer[districtDestinyId];
                            return originName + dm.GetDistrictName(districtDestinyId);
                        }
                    }
                    return originName + destiny.Key.getPrefixTextNaming().Trim() + (destiny.Key.getPrefixTextNaming().Trim() != string.Empty ? " " : "") + destiny.Value;
                }
                else
                {
                    return autoNameByDistrict(t, stopsCount, out middle);
                }
            }
        }

        private static string autoNameByDistrict(TransportLine t, int stopsCount, out int middle)
        {

            DistrictManager dm = Singleton<DistrictManager>.instance;
            NetManager nm = Singleton<NetManager>.instance;
            string result = "";
            byte lastDistrict = 0;
            Vector3 local;
            byte district;
            List<int> districtList = new List<int>();
            for (int j = 0; j < stopsCount; j++)
            {
                local = nm.m_nodes.m_buffer[(int)t.GetStop(j)].m_bounds.center;
                district = dm.GetDistrict(local);
                if ((district != lastDistrict) && district != 0)
                {
                    districtList.Add(district);
                }
                if (district != 0)
                {
                    lastDistrict = district;
                }
            }

            local = nm.m_nodes.m_buffer[(int)t.GetStop(0)].m_bounds.center;
            district = dm.GetDistrict(local);
            if ((district != lastDistrict) && district != 0)
            {
                districtList.Add(district);
            }
            middle = -1;
            int[] districtArray = districtList.ToArray();
            if (districtArray.Length == 1)
            {
                return (TLMCW.getCurrentConfigBool(TLMCW.ConfigIndex.CIRCULAR_IN_SINGLE_DISTRICT_LINE) ? "Circular " : "") + dm.GetDistrictName(districtArray[0]);
            }
            else if (findSimetry(districtArray, out middle))
            {
                int firstIdx = middle;
                int lastIdx = middle + districtArray.Length / 2;

                result = dm.GetDistrictName(districtArray[firstIdx % districtArray.Length]) + " - " + dm.GetDistrictName(districtArray[lastIdx % districtArray.Length]);
                if (lastIdx - firstIdx > 1)
                {
                    result += ", via ";
                    for (int k = firstIdx + 1; k < lastIdx; k++)
                    {
                        result += dm.GetDistrictName(districtArray[k % districtArray.Length]);
                        if (k + 1 != lastIdx)
                        {
                            result += ", ";
                        }
                    }
                }
                return result;
            }
            else {
                bool inicio = true;
                foreach (int i in districtArray)
                {
                    result += (inicio ? "" : " - ") + dm.GetDistrictName(i);
                    inicio = false;
                }
                return result;
            }
        }

        public static bool CalculateSimmetry(ItemClass.SubService ss, int stopsCount, TransportLine t, out int middle)
        {
            int j;
            NetManager nm = Singleton<NetManager>.instance;
            BuildingManager bm = Singleton<BuildingManager>.instance;
            middle = -1;
            //try to find the loop
            for (j = -1; j < stopsCount / 2; j++)
            {
                int offsetL = (j + stopsCount) % stopsCount;
                int offsetH = (j + 2) % stopsCount;
                NetNode nn1 = nm.m_nodes.m_buffer[(int)t.GetStop(offsetL)];
                NetNode nn2 = nm.m_nodes.m_buffer[(int)t.GetStop(offsetH)];
                ushort buildingId1 = bm.FindBuilding(nn1.m_position, 100f, ItemClass.Service.PublicTransport, ss, Building.Flags.None, Building.Flags.Untouchable);
                ushort buildingId2 = bm.FindBuilding(nn2.m_position, 100f, ItemClass.Service.PublicTransport, ss, Building.Flags.None, Building.Flags.Untouchable);
                //					DebugOutputPanel.AddMessage(PluginManager.MessageType.Warning,"buildingId1="+buildingId1+"|buildingId2="+buildingId2);
                //					DebugOutputPanel.AddMessage(PluginManager.MessageType.Warning,"offsetL="+offsetL+"|offsetH="+offsetH);
                if (buildingId1 == buildingId2)
                {
                    middle = j + 1;
                    break;
                }
            }
            //				DebugOutputPanel.AddMessage(PluginManager.MessageType.Warning,"middle="+middle);
            if (middle >= 0)
            {
                for (j = 1; j <= stopsCount / 2; j++)
                {
                    int offsetL = (-j + middle + stopsCount) % stopsCount;
                    int offsetH = (j + middle) % stopsCount;
                    //						DebugOutputPanel.AddMessage(PluginManager.MessageType.Warning,"offsetL="+offsetL+"|offsetH="+offsetH);
                    //						DebugOutputPanel.AddMessage(PluginManager.MessageType.Warning,"t.GetStop (offsetL)="+t.GetStop (offsetH)+"|t.GetStop (offsetH)="+t.GetStop (offsetH));
                    NetNode nn1 = nm.m_nodes.m_buffer[(int)t.GetStop(offsetL)];
                    NetNode nn2 = nm.m_nodes.m_buffer[(int)t.GetStop(offsetH)];
                    ushort buildingId1 = bm.FindBuilding(nn1.m_position, 100f, ItemClass.Service.PublicTransport, ss, Building.Flags.None, Building.Flags.Untouchable);
                    ushort buildingId2 = bm.FindBuilding(nn2.m_position, 100f, ItemClass.Service.PublicTransport, ss, Building.Flags.None, Building.Flags.Untouchable);
                    //						DebugOutputPanel.AddMessage(PluginManager.MessageType.Warning,"buildingId1="+buildingId1+"|buildingId2="+buildingId2);
                    //						DebugOutputPanel.AddMessage(PluginManager.MessageType.Warning,"buildingId1="+buildingId1+"|buildingId2="+buildingId2);
                    //						DebugOutputPanel.AddMessage(PluginManager.MessageType.Warning,"offsetL="+offsetL+"|offsetH="+offsetH);
                    if (buildingId1 != buildingId2)
                    {
                        return false;
                    }
                }
            }
            else {
                return false;
            }
            return true;
        }

        public static readonly ItemClass.Service[] seachOrder = new ItemClass.Service[]{
            ItemClass.Service.PublicTransport,
            ItemClass.Service.Monument,
            ItemClass.Service.Beautification,
            ItemClass.Service.Government,
            ItemClass.Service.HealthCare,
            ItemClass.Service.FireDepartment,
            ItemClass.Service.PoliceDepartment,
            ItemClass.Service.Tourism,
            ItemClass.Service.Education,
            ItemClass.Service.Garbage,
            ItemClass.Service.Office,
            ItemClass.Service.Commercial,
            ItemClass.Service.Industrial,
            ItemClass.Service.Residential,
            ItemClass.Service.Electricity,
            ItemClass.Service.Water
        };




        public static string getStationName(uint stopId, ItemClass.SubService ss, out ItemClass.Service serviceFound, out ItemClass.SubService subserviceFound, bool excludeCargo = false)
        {
            NetManager nm = Singleton<NetManager>.instance;
            BuildingManager bm = Singleton<BuildingManager>.instance;
            NetNode nn = nm.m_nodes.m_buffer[(int)stopId];
            ushort buildingId = getStationBuilding(stopId, ss, excludeCargo);

            Vector3 location = nn.m_position;
            Building b = bm.m_buildings.m_buffer[buildingId];
            while (b.m_parentBuilding > 0)
            {
                doLog("getStationName(): building id {0} - parent = {1}", buildingId, b.m_parentBuilding);
                buildingId = b.m_parentBuilding;
                b = bm.m_buildings.m_buffer[buildingId];
            }
            if (buildingId > 0)
            {
                InstanceID iid = default(InstanceID);
                iid.Building = buildingId;
                serviceFound = b.Info.GetService();
                subserviceFound = b.Info.GetSubService();
                return bm.GetBuildingName(buildingId, iid);
            }
            else {
                serviceFound = ItemClass.Service.None;
                subserviceFound = ItemClass.SubService.None;
                DistrictManager dm = Singleton<DistrictManager>.instance;
                int dId = dm.GetDistrict(location);
                if (dId > 0)
                {
                    District d = dm.m_districts.m_buffer[dId];
                    return dm.GetDistrictName(dId);
                }
                else {
                    return "[X=" + location.x + "|Y=" + location.y + "|Z=" + location.z + "]";
                }
            }
        }


        public static Vector3 getStationBuildingPosition(uint stopId, ItemClass.SubService ss)
        {
            ushort buildingId = getStationBuilding(stopId, ss);


            if (buildingId > 0)
            {
                BuildingManager bm = Singleton<BuildingManager>.instance;
                Building b = bm.m_buildings.m_buffer[buildingId];
                InstanceID iid = default(InstanceID);
                iid.Building = buildingId;
                return b.m_position;
            }
            else
            {
                NetManager nm = Singleton<NetManager>.instance;
                NetNode nn = nm.m_nodes.m_buffer[(int)stopId];
                return nn.m_position;
            }
        }

        public static ushort getStationBuilding(uint stopId, ItemClass.SubService ss, bool excludeCargo = false)
        {
            NetManager nm = Singleton<NetManager>.instance;
            BuildingManager bm = Singleton<BuildingManager>.instance;
            NetNode nn = nm.m_nodes.m_buffer[(int)stopId];
            ushort buildingId = 0, tempBuildingId;

            if (ss != ItemClass.SubService.None)
            {
                tempBuildingId = bm.FindBuilding(nn.m_position, 100f, ItemClass.Service.PublicTransport, ss, Building.Flags.None, Building.Flags.Untouchable);
                if (!excludeCargo || bm.m_buildings.m_buffer[tempBuildingId].Info.GetAI().GetType() == typeof(TransportStationAI))
                {
                    buildingId = tempBuildingId;
                }
            }
            if (buildingId == 0)
            {
                tempBuildingId = bm.FindBuilding(nn.m_position, 100f, ItemClass.Service.PublicTransport, ItemClass.SubService.None, Building.Flags.Active, Building.Flags.Untouchable);
                if (!excludeCargo || bm.m_buildings.m_buffer[tempBuildingId].Info.GetAI().GetType() == typeof(TransportStationAI))
                {
                    buildingId = tempBuildingId;
                }
                if (buildingId == 0)
                {
                    tempBuildingId = bm.FindBuilding(nn.m_position, 100f, ItemClass.Service.PublicTransport, ItemClass.SubService.None, Building.Flags.None, Building.Flags.Untouchable);
                    if (!excludeCargo || bm.m_buildings.m_buffer[tempBuildingId].Info.GetAI().GetType() == typeof(TransportStationAI))
                    {
                        buildingId = tempBuildingId;
                    }
                    if (buildingId == 0)
                    {
                        int iterator = 1;
                        while (buildingId == 0 && iterator < seachOrder.Count())
                        {
                            buildingId = bm.FindBuilding(nn.m_position, 100f, seachOrder[iterator], ItemClass.SubService.None, Building.Flags.None, Building.Flags.Untouchable);
                            iterator++;
                        }
                    }
                }
            }
            return buildingId;

        }

        public static bool findSimetry(int[] array, out int middle)
        {
            middle = -1;
            int size = array.Length;
            if (size == 0)
                return false;
            for (int j = -1; j < size / 2; j++)
            {
                int offsetL = (j + size) % size;
                int offsetH = (j + 2) % size;
                if (array[offsetL] == array[offsetH])
                {
                    middle = j + 1;
                    break;
                }
            }
            //			DebugOutputPanel.AddMessage(PluginManager.MessageType.Warning,"middle="+middle);
            if (middle >= 0)
            {
                for (int k = 1; k <= size / 2; k++)
                {
                    int offsetL = (-k + middle + size) % size;
                    int offsetH = (k + middle) % size;
                    if (array[offsetL] != array[offsetH])
                    {
                        return false;
                    }
                }
            }
            else {
                return false;
            }
            return true;
        }

        public class UIButtonLineInfo : UIButton
        {
            public ushort lineID;
        }

        private static string[] latinoMaiusculo = {
            "A",
            "B",
            "C",
            "D",
            "E",
            "F",
            "G",
            "H",
            "I",
            "J",
            "K",
            "L",
            "M",
            "N",
            "O",
            "P",
            "Q",
            "R",
            "S",
            "T",
            "U",
            "V",
            "W",
            "X",
            "Y",
            "Z"
        };
        private static string[] latinoMinusculo = {
            "a",
            "b",
            "c",
            "d",
            "e",
            "f",
            "g",
            "h",
            "i",
            "j",
            "k",
            "l",
            "m",
            "n",
            "o",
            "p",
            "q",
            "r",
            "s",
            "t",
            "u",
            "v",
            "w",
            "x",
            "y",
            "z"
        };
        private static string[] gregoMaiusculo = {
            "Α",
            "Β",
            "Γ",
            "Δ",
            "Ε",
            "Ζ",
            "Η",
            "Θ",
            "Ι",
            "Κ",
            "Λ",
            "Μ",
            "Ν",
            "Ξ",
            "Ο",
            "Π",
            "Ρ",
            "Σ",
            "Τ",
            "Υ",
            "Φ",
            "Χ",
            "Ψ",
            "Ω"
        };
        private static string[] gregoMinusculo = {
            "α",
            "β",
            "γ",
            "δ",
            "ε",
            "ζ",
            "η",
            "θ",
            "ι",
            "κ",
            "λ",
            "μ",
            "ν",
            "ξ",
            "ο",
            "π",
            "ρ",
            "σ",
            "τ",
            "υ",
            "φ",
            "χ",
            "ψ",
            "ω"
        };
        private static string[] cirilicoMaiusculo = {
            "А",
            "Б",
            "В",
            "Г",
            "Д",
            "Е",
            "Ё",
            "Ж",
            "З",
            "И",
            "Й",
            "К",
            "Л",
            "М",
            "Н",
            "О",
            "П",
            "Р",
            "С",
            "Т",
            "У",
            "Ф",
            "Х",
            "Ц",
            "Ч",
            "Ш",
            "Щ",
            "Ъ",
            "Ы",
            "Ь",
            "Э",
            "Ю",
            "Я"
        };
        private static string[] cirilicoMinusculo = {
            "а",
            "б",
            "в",
            "г",
            "д",
            "е",
            "ё",
            "ж",
            "з",
            "и",
            "й",
            "к",
            "л",
            "м",
            "н",
            "о",
            "п",
            "р",
            "с",
            "т",
            "у",
            "ф",
            "х",
            "ц",
            "ч",
            "ш",
            "щ",
            "ъ",
            "ы",
            "ь",
            "э",
            "ю",
            "я"
        };

        private static string[] numeros = {
            "0",
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9"
        };


        public static readonly ModoNomenclatura[] nomenclaturasComNumeros = new ModoNomenclatura[]
        {
        ModoNomenclatura. LatinoMinusculoNumero ,
        ModoNomenclatura. LatinoMaiusculoNumero ,
        ModoNomenclatura. GregoMinusculoNumero,
        ModoNomenclatura. GregoMaiusculoNumero,
        ModoNomenclatura. CirilicoMinusculoNumero,
        ModoNomenclatura. CirilicoMaiusculoNumero
        };
    }

    public class ResourceLoader
    {

        public static Assembly ResourceAssembly
        {
            get
            {
                //return null;
                return Assembly.GetAssembly(typeof(ResourceLoader));
            }
        }

        public static byte[] loadResourceData(string name)
        {
            name = "TransportLinesManager." + name;

            UnmanagedMemoryStream stream = (UnmanagedMemoryStream)ResourceAssembly.GetManifestResourceStream(name);
            if (stream == null)
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Error, "Could not find resource: " + name);
                return null;
            }

            BinaryReader read = new BinaryReader(stream);
            return read.ReadBytes((int)stream.Length);
        }

        public static string loadResourceString(string name)
        {
            name = "TransportLinesManager." + name;

            UnmanagedMemoryStream stream = (UnmanagedMemoryStream)ResourceAssembly.GetManifestResourceStream(name);
            if (stream == null)
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Error, "Could not find resource: " + name);
                return null;
            }

            StreamReader read = new StreamReader(stream);
            return read.ReadToEnd();
        }

        public static Texture2D loadTexture(int x, int y, string filename)
        {
            try
            {
                Texture2D texture = new Texture2D(x, y);
                texture.LoadImage(loadResourceData(filename));
                return texture;
            }
            catch (Exception e)
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Error, "The file could not be read:" + e.Message);
            }

            return null;
        }
    }
    public enum ModoNomenclatura
    {
        Numero = 0,
        LatinoMinusculo = 1,
        LatinoMaiusculo = 2,
        GregoMinusculo = 3,
        GregoMaiusculo = 4,
        CirilicoMinusculo = 5,
        CirilicoMaiusculo = 6,
        Nenhum = 7,
        LatinoMinusculoNumero = 8,
        LatinoMaiusculoNumero = 9,
        GregoMinusculoNumero = 10,
        GregoMaiusculoNumero = 11,
        CirilicoMinusculoNumero = 12,
        CirilicoMaiusculoNumero = 13,
    }

    public enum Separador
    {
        Nenhum = 0,
        Hifen = 1,
        Ponto = 2,
        Barra = 3,
        Espaco = 4,
        QuebraLinha = 5
    }

    public class Range<T> where T : IComparable<T>
    {
        /// <summary>
        /// Minimum value of the range
        /// </summary>
        public T Minimum { get; set; }

        /// <summary>
        /// Maximum value of the range
        /// </summary>
        public T Maximum { get; set; }

        public Range(T min, T max)
        {
            if (min.CompareTo(max) >= 0)
            {
                var temp = min;
                min = max;
                max = temp;
            }
            Minimum = min;
            Maximum = max;
        }

        /// <summary>
        /// Presents the Range in readable format
        /// </summary>
        /// <returns>String representation of the Range</returns>
        public override string ToString() { return String.Format("[{0} - {1}]", Minimum, Maximum); }

        /// <summary>
        /// Determines if the range is valid
        /// </summary>
        /// <returns>True if range is valid, else false</returns>
        public Boolean IsValid() { return Minimum.CompareTo(Maximum) <= 0; }

        /// <summary>
        /// Determines if the provided value is inside the range
        /// </summary>
        /// <param name="value">The value to test</param>
        /// <returns>True if the value is inside Range, else false</returns>
        public Boolean ContainsValue(T value)
        {
            return (Minimum.CompareTo(value) <= 0) && (value.CompareTo(Maximum) <= 0);
        }


        /// <summary>
        /// Determines if the provided value is inside the range
        /// </summary>
        /// <param name="value">The value to test</param>
        /// <returns>True if the value is inside Range, else false</returns>
        public Boolean IsBetweenLimits(T value)
        {
            return (Minimum.CompareTo(value) < 0) && (value.CompareTo(Maximum) < 0);
        }

        /// <summary>
        /// Determines if this Range is inside the bounds of another range
        /// </summary>
        /// <param name="Range">The parent range to test on</param>
        /// <returns>True if range is inclusive, else false</returns>
        public Boolean IsInsideRange(Range<T> Range)
        {
            return this.IsValid() && Range.IsValid() && Range.ContainsValue(this.Minimum) && Range.ContainsValue(this.Maximum);
        }



        /// <summary>
        /// Determines if another range is inside the bounds of this range
        /// </summary>
        /// <param name="Range">The child range to test</param>
        /// <returns>True if range is inside, else false</returns>
        public Boolean ContainsRange(Range<T> Range)
        {
            return this.IsValid() && Range.IsValid() && this.ContainsValue(Range.Minimum) && this.ContainsValue(Range.Maximum);
        }

        /// <summary>
        /// Determines if another range intersect this range
        /// </summary>
        /// <param name="Range">The child range to test</param>
        /// <returns>True if range is inside, else false</returns>
        public Boolean IntersectRange(Range<T> Range)
        {
            return this.IsValid() && Range.IsValid() && (this.ContainsValue(Range.Minimum) || this.ContainsValue(Range.Maximum) || Range.ContainsValue(this.Maximum) || Range.ContainsValue(this.Maximum));
        }

        public Boolean IsBorderSequence(Range<T> Range)
        {
            return this.IsValid() && Range.IsValid() && (this.Maximum.Equals(Range.Minimum) || this.Minimum.Equals(Range.Maximum));
        }
    }

    public static class Vector2Extensions
    {
        public static float GetAngleToPoint(this Vector2 from, Vector2 to)
        {
            float ca = to.x - from.x;
            float co = to.y - from.y;
            if (co == 0)
            {
                return ca > 0 ? 0 : 180;
            }
            else if (ca < 0)
            {
                return Mathf.Atan(co / ca) * Mathf.Rad2Deg + 180;
            }
            else
            {
                return Mathf.Atan(co / ca) * Mathf.Rad2Deg;
            }
        }
    }

    public static class Int32Extensions
    {
        public static int ParseOrDefault(string val, int defaultVal)
        {
            try
            {
                return int.Parse(val);
            }
            catch (Exception e)
            {
                return defaultVal;
            }
        }
    }
}
