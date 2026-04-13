# BugReport-11: Login Beheer Button Not Shown

## Status

- [ ] Open

## Summary

Na inloggen wordt de knop "Beheer" soms niet getoond in de navigatie, ook voor gebruikers met admin-rechten.

## Environment

- Version: latest
- Scope: productie
- Frequency: intermitterend (race condition)

## Steps to Reproduce

1. Deploy een nieuwe versie van de applicatie.
2. Log in met een admin-account.
3. Controleer de navigatiebalk direct na login.

## Expected Behaviour

De knop "Beheer" is direct zichtbaar voor admin-gebruikers na succesvol inloggen.

## Actual Behaviour

De knop "Beheer" ontbreekt soms direct na login.

## Workaround

- Een browser refresh toont de knop daarna meestal wel.

## Impact

- Admin-functionaliteit is minder vindbaar in productie.
- Leidt tot verwarring over rechten en loginstatus.
- Verhoogt support- en triage-last door intermitterend gedrag.

## Related Reports

- `docs/BugReports/Archive/BugReport-9-BlazorCircuit-AdminSettings.md` (gerelateerd auth/circuit timing context, maar geen duplicaat)

## Suspected Scope

Waarschijnlijk timing/race-condition in de initialisatie van admin-state voor de navigatie:

- `LocalFinanceManager/Components/Layout/NavMenu.razor`
- `LocalFinanceManager/Services/IUserContext.cs`
- `LocalFinanceManager/Services/UserContext.cs`
- `LocalFinanceManager/Components/Shared/AdminRouteGuard.razor`

Mogelijke oorzaak: admin-status wordt te vroeg als false geëvalueerd tijdens startup/warmup, waarna de UI-state niet direct wordt hersteld zonder refresh.

## Tasks

- [ ] Reproduce in productie-achtige omgeving met cold start na deploy
- [ ] Voeg tijdelijke logging toe rond user context en admin-check in nav initialisatie
- [ ] Valideer timing van login-state versus render van navigatie
- [ ] Borg dat admin-state opnieuw wordt geëvalueerd zodra user context beschikbaar is
- [ ] Voeg regressietest toe voor admin-knop zichtbaarheid direct na login (zonder refresh)
- [ ] Verifieer dat route-toegang en knop-zichtbaarheid consistent blijven voor admin/non-admin

## Acceptance Criteria

- [ ] "Beheer" is direct zichtbaar na login voor admin-gebruikers, ook na verse deploy/cold start
- [ ] Geen handmatige refresh nodig om de knop zichtbaar te maken
- [ ] Non-admin gebruikers zien de knop niet
- [ ] Regressietest toegevoegd/geüpdatet en passing
