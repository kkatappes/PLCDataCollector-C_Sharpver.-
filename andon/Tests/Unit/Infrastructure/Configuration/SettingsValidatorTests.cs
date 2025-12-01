using Xunit;
using Andon.Infrastructure.Configuration;
using System;

namespace Andon.Tests.Unit.Infrastructure.Configuration
{
    public class SettingsValidatorTests
    {
        private readonly SettingsValidator _validator;

        public SettingsValidatorTests()
        {
            _validator = new SettingsValidator();
        }

        #region IpAddress Validation Tests

        [Theory]
        [InlineData("192.168.1.10")]
        [InlineData("172.30.40.15")]
        [InlineData("10.0.0.1")]
        public void ValidateIpAddress_WhenValidFormat_ShouldNotThrow(string ipAddress)
        {
            // Act & Assert
            var exception = Record.Exception(() => _validator.ValidateIpAddress(ipAddress));
            Assert.Null(exception);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void ValidateIpAddress_WhenEmpty_ShouldThrowArgumentException(string ipAddress)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.ValidateIpAddress(ipAddress));
            Assert.Contains("必須項目 'IPAddress'", exception.Message);
        }

        [Theory]
        [InlineData("999.999.999.999")]
        [InlineData("abc.def.ghi.jkl")]
        [InlineData("192.168.1")]
        public void ValidateIpAddress_WhenInvalidFormat_ShouldThrowArgumentException(string ipAddress)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.ValidateIpAddress(ipAddress));
            Assert.Contains("IPAddressの形式が不正です", exception.Message);
        }

        [Fact]
        public void ValidateIpAddress_When0000_ShouldThrowArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.ValidateIpAddress("0.0.0.0"));
            Assert.Contains("IPAddress '0.0.0.0' は使用できません", exception.Message);
        }

        #endregion

        #region Port Validation Tests

        [Theory]
        [InlineData(1)]
        [InlineData(8192)]
        [InlineData(65535)]
        public void ValidatePort_WhenInRange_ShouldNotThrow(int port)
        {
            // Act & Assert
            var exception = Record.Exception(() => _validator.ValidatePort(port));
            Assert.Null(exception);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(70000)]
        public void ValidatePort_WhenOutOfRange_ShouldThrowArgumentException(int port)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.ValidatePort(port));
            Assert.Contains("Portの値が範囲外です", exception.Message);
        }

        #endregion

        #region ConnectionMethod Validation Tests

        [Theory]
        [InlineData("TCP")]
        [InlineData("UDP")]
        [InlineData("tcp")]
        [InlineData("udp")]
        public void ValidateConnectionMethod_WhenValid_ShouldNotThrow(string connectionMethod)
        {
            // Act & Assert
            var exception = Record.Exception(() => _validator.ValidateConnectionMethod(connectionMethod));
            Assert.Null(exception);
        }

        [Theory]
        [InlineData("HTTP")]
        [InlineData("FTP")]
        [InlineData("")]
        public void ValidateConnectionMethod_WhenInvalid_ShouldThrowArgumentException(string connectionMethod)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.ValidateConnectionMethod(connectionMethod));
            Assert.Contains("ConnectionMethodの値が不正です", exception.Message);
        }

        #endregion

        #region FrameVersion Validation Tests

        [Theory]
        [InlineData("3E")]
        [InlineData("4E")]
        [InlineData("3e")]
        [InlineData("4e")]
        public void ValidateFrameVersion_WhenValid_ShouldNotThrow(string frameVersion)
        {
            // Act & Assert
            var exception = Record.Exception(() => _validator.ValidateFrameVersion(frameVersion));
            Assert.Null(exception);
        }

        [Theory]
        [InlineData("5E")]
        [InlineData("2E")]
        [InlineData("")]
        public void ValidateFrameVersion_WhenInvalid_ShouldThrowArgumentException(string frameVersion)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.ValidateFrameVersion(frameVersion));
            Assert.Contains("FrameVersionの値が不正です", exception.Message);
        }

        #endregion

        #region Timeout Validation Tests

        [Theory]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(30000)]
        public void ValidateTimeout_WhenInRange_ShouldNotThrow(int timeoutMs)
        {
            // Act & Assert
            var exception = Record.Exception(() => _validator.ValidateTimeout(timeoutMs));
            Assert.Null(exception);
        }

        [Theory]
        [InlineData(50)]
        [InlineData(40000)]
        public void ValidateTimeout_WhenOutOfRange_ShouldThrowArgumentException(int timeoutMs)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.ValidateTimeout(timeoutMs));
            Assert.Contains("Timeoutの値が範囲外です", exception.Message);
        }

        #endregion

        #region MonitoringIntervalMs Validation Tests

        [Theory]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(60000)]
        public void ValidateMonitoringIntervalMs_WhenInRange_ShouldNotThrow(int intervalMs)
        {
            // Act & Assert
            var exception = Record.Exception(() => _validator.ValidateMonitoringIntervalMs(intervalMs));
            Assert.Null(exception);
        }

        [Theory]
        [InlineData(50)]
        [InlineData(70000)]
        public void ValidateMonitoringIntervalMs_WhenOutOfRange_ShouldThrowArgumentException(int intervalMs)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.ValidateMonitoringIntervalMs(intervalMs));
            Assert.Contains("MonitoringIntervalMsの値が範囲外です", exception.Message);
        }

        #endregion
    }
}
