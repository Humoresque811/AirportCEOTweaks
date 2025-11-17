using AirportCEONationality;
using System;
using System.Linq;
using Xunit;

namespace AirportCEOTweaksTests
{
    public class NationalityTests
    {
        [Fact]
        public void SizeHelper_IsSmallerThan_1()
        {
            Enums.GenericSize size1 = Enums.GenericSize.Medium;
            Enums.GenericSize size2 = Enums.GenericSize.Large;

            Assert.True(size1.IsSmallerThan(size2));
        }
        [Fact]
        public void SizeHelper_IsSmallerThan_2()
        {
            Enums.GenericSize size1 = Enums.GenericSize.Large;
            Enums.GenericSize size2 = Enums.GenericSize.Small;

            Assert.False(size1.IsSmallerThan(size2));
        }
        [Fact]
        public void SizeHelper_IsLargerThan_1()
        {
            Enums.GenericSize size1 = Enums.GenericSize.Large;
            Enums.GenericSize size2 = Enums.GenericSize.Small;

            Assert.True(size1.IsLargerThan(size2));
        }
        [Fact]
        public void SizeHelper_IsLargerThan_2()
        {
            Enums.GenericSize size2 = Enums.GenericSize.Large;
            Enums.GenericSize size1 = Enums.GenericSize.Small;

            Assert.False(size1.IsLargerThan(size2));
        }
        [Fact]
        public void SizeHelper_IsEqualTo_1()
        {
            Enums.GenericSize size2 = Enums.GenericSize.Large;
            Enums.GenericSize size1 = Enums.GenericSize.Large;

            Assert.True(size1.IsEqualTo(size2));
        }

    }
}
