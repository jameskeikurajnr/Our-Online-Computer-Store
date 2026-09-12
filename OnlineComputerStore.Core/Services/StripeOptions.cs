namespace OnlineComputerStore.Core.Services
{
    // Bound from the "Stripe" section of appsettings.json. Same "off by default,
    // fully usable without it" pattern as AiOptions/EmailOptions — with Enabled
    // false (or no SecretKey), checkout just places the order directly with no
    // payment step, so the site still works before you've set up a Stripe account.
    public class StripeOptions
    {
        public bool Enabled { get; set; } = false;

        // Publishable key (pk_test_/pk_live_) — safe to expose client-side if ever needed,
        // kept here mainly so it lives next to its secret counterpart.
        public string PublishableKey { get; set; } = "";

        // Secret key (sk_test_/sk_live_) — server-side only, never sent to the browser.
        public string SecretKey { get; set; } = "";

        // Optional: signing secret for a Stripe webhook endpoint (whsec_...), if one is
        // added later for production hardening. Not required for the redirect-based flow.
        public string WebhookSecret { get; set; } = "";

        // Lower-case ISO currency code Stripe expects, e.g. "usd", "aud".
        public string Currency { get; set; } = "usd";
    }
}
