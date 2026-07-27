using HarmonyLib;
using RimWorld;
using Verse;

namespace AndroidTiersContinuedPatch
{
    [HarmonyPatch(typeof(HealthAIUtility), "FindMedicineInInventory")]
    public static class MedicineInventoryFixPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn doctor, Pawn patient, ref Medicine __result)
        {
            if (__result == null && doctor?.inventory != null && patient != null)
            {
                // Check if patient is an android
                bool isAndroid = false;
                if (patient.def != null)
                {
                    string defName = patient.def.defName;
                    if (defName.StartsWith("Android") || 
                        defName == "M7Mech" || 
                        defName == "M8Mech" || 
                        defName == "AT_HellUnit" ||
                        defName.StartsWith("Robotic"))
                    {
                        isAndroid = true;
                    }
                }

                // If tending a normal organic pawn, ensure medicine in doctor's inventory is found
                if (!isAndroid)
                {
                    MedicalCareCategory medCare = (patient.playerSettings != null) ? patient.playerSettings.medCare : MedicalCareCategory.NoCare;
                    if (medCare != MedicalCareCategory.NoCare && medCare != MedicalCareCategory.NoMeds)
                    {
                        var container = doctor.inventory.innerContainer;
                        for (int i = 0; i < container.Count; i++)
                        {
                            if (container[i] is Medicine medicine && medCare.AllowsMedicine(medicine.def))
                            {
                                __result = medicine;
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
