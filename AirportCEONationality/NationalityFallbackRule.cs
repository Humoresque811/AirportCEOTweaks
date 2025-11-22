using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirportCEONationality;

enum NationalityFallbackRule
{
    [Description("Fallback to vanilla generation")]
    FallbackVanilla,

    [Description("Fallback to vanilla generation & notify")]
    FallbackVanillaNotify,

    [Description("Don't generate")]
    DontGenerate,
}