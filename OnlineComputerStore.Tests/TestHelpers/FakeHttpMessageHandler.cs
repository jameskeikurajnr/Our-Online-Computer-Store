using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OnlineComputerStore.Tests.TestHelpers
{
    // Lets StripePaymentServiceTests script exactly what Stripe's API "returns"
    // without making a real network call — the service is tested against a fake
    // transport, not against Stripe itself.
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public HttpRequestMessage? LastRequest { get; private set; }

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_responder(request));
        }
    }
}
