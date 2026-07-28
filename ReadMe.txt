# CQRS, the book

In 2009 I have had the pleasure of spending a 2 day course and many geek beers with 
Greg Young talking about Domain-Driven Design specifically focussed on Command Query 
Responsibility Segregation (CQRS).

The example project I created based on these discussions was very well received by 
the community and regarded a good reference project to explain and learn the patterns 
that make up CQRS. I decided to add the different blog posts I wrote about the example 
into a single book so it is easy to find and read.

You can find the book here: https://leanpub.com/cqrs

---

# x86 vs x64

This section described the original System.Data.SQLite native-binary setup and no longer
applies. Storage now goes through EF Core / Microsoft.Data.Sqlite, which has no x86/x64
binary-swap step.

If you have any questions or other feedback then I would love to hear about it at
Mark.Nijhof@Cre8iveThought.com

-Mark

---

While this is based on Mark's book I have been working on updating this to .NET 10.0

After the effort to convert this to more modern infrastructure hopefully others will find
this of use.

-Thanks,
Matt Whited

## Known Issues

The original concurrency/event-processing issues from the .NET 7 pass were tracked down to
several concrete bugs during the .NET 10 modernization (fire-and-forget bus dispatch,
async-void exception swallowing, dead test wiring, a wizard-step state bug) and fixed - see
the `poc/dotnet10-modernization` branch history. The WinForms UI itself has not been
manually exercised end-to-end during this pass, only through the automated test suite, so
treat live UI behavior (including auto-refresh after a command) as unverified rather than
confirmed-working.

