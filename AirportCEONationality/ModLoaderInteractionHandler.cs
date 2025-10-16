using AirportCEOModLoader.WatermarkUtils;
using AirportCEOModLoader.SaveLoadUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace AirportCEONationality
{

    internal class ModLoaderInteractionHandler
    {
        internal static void SetUpInteractions()
        {
            // More will probably be added!
            AirportCEONationality.LogInfo("Seting up ModLoader interactions");

            WatermarkUtils.Register(new WatermarkInfo("T-N", Assembly.GetExecutingAssembly().GetName().Version.ToString(), true));
            CoroutineEventDispatcher.RegisterToLaunchGamePhase(RouteGenerationController.Instance.Setup, CoroutineEventDispatcher.CoroutineAttachmentType.After);


            AirportCEONationality.LogInfo("Completed ModLoader interactions!");
        }
    }
}
