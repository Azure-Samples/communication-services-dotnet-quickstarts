using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace CallAutomation.Scenarios.Handlers
{

    public class EventGridAuthOptions : AuthenticationSchemeOptions
    {
        public string Secret { get; set; }
        public string HeaderName { get; set; }
        public string QueryParameter { get; set; }
    }


    public class EventGridAuthHandler : AuthenticationHandler<EventGridAuthOptions>
    {
        public const string EventGridAuthenticationScheme = "EventGridAuthenticationScheme";

        public EventGridAuthHandler(IOptionsMonitor<EventGridAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
              : base(options, logger, encoder, clock)
        {

        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (string.IsNullOrEmpty(this.Options.Secret))
            {
                throw new ArgumentException("Must specify Secret Value");
            }

            if (string.IsNullOrWhiteSpace(this.Options.HeaderName) && string.IsNullOrWhiteSpace(this.Options.QueryParameter))
            {
                throw new ArgumentException("Must specify HeaderName or QueryParameter");
            }

            if (!string.IsNullOrWhiteSpace(this.Options.HeaderName))
            {
                if (this.Request.Headers != null && this.Request.Headers.ContainsKey(this.Options.HeaderName))
                {
                    if (this.Request.Headers[this.Options.HeaderName].ToString().Equals(this.Options.Secret, StringComparison.Ordinal))
                    {
                        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("EventGridHandler", "EventGridHandler") })), EventGridAuthenticationScheme)));
                    }
                    else
                    {
                        return Task.FromResult(AuthenticateResult.Fail(new Exception("Secret header was present, but value doesn't match")));
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(this.Options.QueryParameter))
            {
                if (this.Request.Query != null && this.Request.Query.Keys != null && this.Request.Query.Keys.Contains(this.Options.QueryParameter))
                {
                    if (this.Request.Query[this.Options.QueryParameter].ToString().Equals(this.Options.Secret, StringComparison.Ordinal))
                    {
                        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("EventGridHandler", "EventGridHandler") })), EventGridAuthenticationScheme)));
                    }
                    else
                    {
                        return Task.FromResult(AuthenticateResult.Fail(new Exception("Secret header was present, but value doesn't match")));
                    }
                }
            }

            return Task.FromResult(AuthenticateResult.NoResult());
        }
    }

}
