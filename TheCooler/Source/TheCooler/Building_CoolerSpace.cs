using UnityEngine;
using Verse;
using RimWorld;

namespace TheFarawayDev.TheCooler
{
    public class Building_CoolerSpace : Building_TempControl
    {
        public override void TickRare()
        {
            if (!this.Spawned) return;
            if (this.compPowerTrader == null || !this.compPowerTrader.PowerOn) return;
            if (this.compTempControl == null) return;

            float ambientTemp = this.AmbientTemperature;
            float multiplier;

            // Inverted efficiency fall-off:
            // High efficiency above -10°C, falls off towards -100°C.
            if (ambientTemp > -10f)
            {
                multiplier = 1f;
            }
            else if (ambientTemp < -100f)
            {
                multiplier = 0f;
            }
            else
            {
                multiplier = Mathf.InverseLerp(-100f, -10f, ambientTemp);
            }

            // Energy pushed is negative for cooling (energyPerSecond in Def is -35)
            float energy = this.compTempControl.Props.energyPerSecond * multiplier * 4.166667f;
            float tempChange = GenTemperature.ControlTemperatureTempChange(this.Position, this.Map, energy, this.compTempControl.TargetTemperature);

            bool operatingAtHighPower = !Mathf.Approximately(tempChange, 0f);
            if (operatingAtHighPower)
            {
                this.compPowerTrader.PowerOutput = -this.compPowerTrader.Props.PowerConsumption;
                
                // Retrieve room and apply temperature change
                Room room = this.GetRoom(RegionType.Set_All);
                if (room != null)
                {
                    room.Temperature += tempChange;
                }
            }
            else
            {
                this.compPowerTrader.PowerOutput = -this.compPowerTrader.Props.PowerConsumption * this.compTempControl.Props.lowPowerConsumptionFactor;
            }
            this.compTempControl.operatingAtHighPower = operatingAtHighPower;
        }
    }
}
