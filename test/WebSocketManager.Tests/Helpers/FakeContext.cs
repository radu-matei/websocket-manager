using Microsoft.AspNetCore.Http;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Features;
using System.Collections.Generic;
using System.Security.Claims;

namespace WebSocketManager.Tests.Helpers
{
  internal class FakeContext : HttpContext
  {
    public override IFeatureCollection Features => throw new NotImplementedException();

    public override HttpRequest Request => throw new NotImplementedException();

    public override HttpResponse Response => throw new NotImplementedException();

    public override ConnectionInfo Connection => throw new NotImplementedException();

    public override Microsoft.AspNetCore.Http.WebSocketManager WebSockets => throw new NotImplementedException();

    public override AuthenticationManager Authentication => throw new NotImplementedException();

    public override ClaimsPrincipal User { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public override IDictionary<object, object> Items { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public override IServiceProvider RequestServices { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public override CancellationToken RequestAborted { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public override string TraceIdentifier { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public override ISession Session { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override void Abort()
    {
      throw new NotImplementedException();
    }
  }
}
