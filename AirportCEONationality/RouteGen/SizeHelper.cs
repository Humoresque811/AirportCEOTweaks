using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirportCEONationality;

public static class SizeHelper
{
    public static bool IsSmallerThan(this Enums.GenericSize size1, Enums.GenericSize size2)
    {
        return (byte)size1 > (byte)size2; // the int casts are backwards for some reason
    }
    public static bool IsLargerThan(this Enums.GenericSize size1, Enums.GenericSize size2)
    {
        return (byte)size1 < (byte)size2; // the int casts are backwards for some reason
    }
    public static bool IsEqualTo(this Enums.GenericSize size1, Enums.GenericSize size2)
    {
        return (byte)size1 == (byte)size2; // the int casts are backwards for some reason
    }
}
