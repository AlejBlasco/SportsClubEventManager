using System.Runtime.CompilerServices;

// Allows the Infrastructure test project to unit test internal members (e.g.
// ActiveEventsGaugeUpdater.GetActiveEventsCountAsync, extracted specifically for isolated
// testing per issue #42's design) without widening their visibility to public. This is a
// test-only visibility grant; it does not change any runtime behavior.
[assembly: InternalsVisibleTo("SportsClubEventManager.Infrastructure.Tests")]
