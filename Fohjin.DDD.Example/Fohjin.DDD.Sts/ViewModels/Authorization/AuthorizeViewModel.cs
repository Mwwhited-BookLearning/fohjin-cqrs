namespace Fohjin.DDD.Sts.ViewModels.Authorization;

// Only ever rendered if a client's ConsentType isn't Implicit. The one seeded dev client
// (Program.cs) is Implicit, so in normal use a caller only ever sees the login screen, not
// this - kept anyway so the authorization endpoint is well-formed rather than assuming its
// only caller forever.
public class AuthorizeViewModel
{
    public required string ApplicationName { get; set; }
    public string? Scope { get; set; }
}
