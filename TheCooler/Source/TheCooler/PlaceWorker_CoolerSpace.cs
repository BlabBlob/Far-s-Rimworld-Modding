using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TheFarawayDev.TheCooler
{
    public class PlaceWorker_CoolerSpace : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 loc, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            Map currentMap = Find.CurrentMap;
            if (currentMap == null) return;

            Room room = loc.GetRoom(currentMap);
            if (room != null && !room.UsesOutdoorTemperature)
            {
                GenDraw.DrawFieldEdges(room.Cells.ToList(), GenTemperature.ColorRoomCold, null, null, 2900);
            }
        }
    }
}
