using Xunit;
using Andon.Core.Constants;

namespace Andon.Tests.Unit.Core.Constants
{
    public class DefaultValuesTests
    {
        [Fact]
        public void ConnectionMethod_ShouldBeUDP()
        {
            Assert.Equal("UDP", DefaultValues.ConnectionMethod);
        }

        [Fact]
        public void FrameVersion_ShouldBe4E()
        {
            Assert.Equal("4E", DefaultValues.FrameVersion);
        }

        [Fact]
        public void TimeoutMs_ShouldBe1000()
        {
            Assert.Equal(1000, DefaultValues.TimeoutMs);
        }

        [Fact]
        public void TimeoutSlmp_ShouldBe4()
        {
            Assert.Equal((ushort)4, DefaultValues.TimeoutSlmp);
        }

        [Fact]
        public void IsBinary_ShouldBeTrue()
        {
            Assert.True(DefaultValues.IsBinary);
        }

        [Fact]
        public void MonitoringIntervalMs_ShouldBe1000()
        {
            Assert.Equal(1000, DefaultValues.MonitoringIntervalMs);
        }
    }
}
