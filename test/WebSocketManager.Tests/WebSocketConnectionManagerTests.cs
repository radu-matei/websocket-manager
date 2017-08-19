using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using WebSocketManager.Tests.Helpers;
using Xunit;

namespace WebSocketManager.Tests
{
    public class WebSocketConnectionManagerTests
    {
        private readonly WebSocketConnectionManager _manager;

        public WebSocketConnectionManagerTests()
        {
            _manager = new WebSocketConnectionManager();
        }

        public class GetSocketById : WebSocketConnectionManagerTests
        {
            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData("foo")]
            public void WhenNonExistentId_ShouldReturnNull(string id)
            {
                var socket = _manager.GetSocketById(id);

                Assert.Null(socket);
            }

            [Fact]
            public void WhenExistingId_ShouldReturnSocket()
            {
                var socket = new FakeSocket();
                var context = new FakeContext();
                _manager.AddSocket(socket, context);
                var id = _manager.GetId(socket);

                Assert.Same(socket, _manager.GetSocketById(id));
            }
        }

        public class GetAll : WebSocketConnectionManagerTests
        {
            [Fact]
            public void WhenEmpty_ShouldReturnZero()
            {
                Assert.Equal(0, _manager.GetAll().Count);
            }

            [Fact]
            public void WhenOneSocket_ShouldReturnOne()
            {
                _manager.AddSocket(new FakeSocket(), new FakeContext());

                Assert.Equal(1, _manager.GetAll().Count);
            }
        }

        public class GetId : WebSocketConnectionManagerTests
        {
            [Fact]
            public void WhenNull_ShouldReturnNull()
            {
                var id = _manager.GetId(null);

                Assert.Null(id);
            }

            [Fact]
            public void WhenUntrackedInstance_ShouldReturnNull()
            {
                var id = _manager.GetId(new FakeSocket());

                Assert.Null(id);
            }

            [Fact]
            public void WhenTrackedInstance_ShouldReturnId()
            {
                var socket = new FakeSocket();
                _manager.AddSocket(socket, new FakeContext());

                var id = _manager.GetId(socket);

                Assert.NotNull(id);
            }
        }

        public class AddSocket : WebSocketConnectionManagerTests
        {
            [Fact(Skip = "At the moment the implementation allows adding null references")]
            public void WhenNull_ShouldNotNotContainSocket()
            {
                _manager.AddSocket(null, null);

                Assert.Equal(0, _manager.GetAll().Count);
            }

            [Fact]
            public void WhenInstance_ShouldContainSocket()
            {
                _manager.AddSocket(new FakeSocket(), new FakeContext());

                Assert.Equal(1, _manager.GetAll().Count);
            }
        }

        public class RemoveSocket : WebSocketConnectionManagerTests
        {
            [Theory(Skip = "Currently it doesn't check if the socket was removed or not, so we get an NRE")]
            [InlineData(null)]
            [InlineData("")]
            [InlineData("foo")]
            public async Task WhenNonExistentId_ShouldNotThrowException(string id)
            {
                await _manager.RemoveSocket(id);
            }
        }
    }
}