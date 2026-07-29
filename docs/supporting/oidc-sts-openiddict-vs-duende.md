# OAuth/OIDC STS choice: OpenIddict over Duende IdentityServer

## The requirement

A lightweight Security Token Service (STS) that's genuinely usable for local development
out of the box, but structured so a real-world identity provider (Entra ID, Auth0,
Keycloak, Okta, ...) can be swapped in later **without changing application code** — only
configuration (the OIDC authority URL, client id/secret, scopes).

## The comparison

| | OpenIddict | Duende IdentityServer |
|---|---|---|
| License | MIT, no commercial restriction | Free for dev/test/personal; **production use requires a paid license** (Starter/Business/Enterprise tiers) |
| Maturity/docs | Smaller community, more "bare metal" — e.g. client-credentials flow needs custom token-endpoint code | Larger community, more turnkey, more built-in UI/features |
| Protocol scope | OAuth2 + OIDC only | OAuth2 + OIDC (+ SAML in some tiers) |
| Fit for "embed a dev STS, swap for real IdP later" | Good — you own the whole token endpoint, so it's a thin, disposable shim | Good functionally, but production licensing terms are the wrong shape for an example/reference project people might actually run in production without realizing they need a license |

**Decision: OpenIddict.** Zero licensing entanglement (important for a public reference
project), and the "more bare metal" downside is actually a *good* fit here — this STS only
ever needs to support one grant type for one dev client, so OpenIddict's low-level control
means the whole thing can be small and legible rather than configuring away a large
feature surface we don't need.

## Design implication

The WebAPI must authenticate purely via **standard OIDC discovery** (`Authority` +
`.well-known/openid-configuration`) — `AddAuthentication().AddJwtBearer(o => o.Authority = ...)`
— with zero OpenIddict-specific code or types anywhere outside the STS project itself.
That's what makes "swap in a real IdP later" a config change instead of a code change. The
STS project should be named/structured so it's obviously a dev-only component (e.g.
`Fohjin.DDD.Sts`), never referenced by the WebAPI project directly — only reachable via its
HTTP discovery document, same as any external IdP would be.

## Sources

- [Licensing | Duende Docs](https://docs.duendesoftware.com/general/licensing/)
- [Duende IdentityServer vs Keycloak vs OpenIddict in .NET: Which to Use in 2026?](https://codingdroplets.com/duende-identityserver-vs-keycloak-vs-openiddict-in-net-which-to-use-in-2026)
- [Top 5 authentication solutions for secure .NET apps in 2026 (WorkOS)](https://workos.com/blog/top-authentication-solutions-net-2026)
- [Replacing IdentityServer4 in .NET: Duende, OpenIddict or Azure Entra ID](https://www.software-assist.nl/en/blog/replacing-identityserver4)
