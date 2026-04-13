# UserStory-26: Settings-accordeon in navigatiemenu

## Status

- [ ] Not Started

## Description

Als ingelogde gebruiker wil ik in het navigatiemenu een duidelijke Settings-accordeon met tandwiel zien, zodat geavanceerde instellingen logisch gegroepeerd zijn. Daarnaast wil ik dat Beheer als laatste menu-item staat, zodat de primaire navigatie bovenaan overzichtelijk blijft.

## Acceptance Criteria

- [ ] Het navigatiemenu bevat een Settings-accordeon met een tandwiel-icoon
- [ ] De items Automatisch toewijzen en Back-up & herstel staan alleen binnen de Settings-accordeon
- [ ] Automatisch toewijzen en Back-up & herstel zijn niet langer als losse top-level menu-items zichtbaar
- [ ] De bestaande routes voor Automatisch toewijzen en Back-up & herstel blijven ongewijzigd en werken na de menuwijziging
- [ ] Beheer staat als laatste menu-item in de navigatie
- [ ] De admin-only zichtbaarheid van Beheer blijft ongewijzigd
- [ ] Het open/dichtklappen van de Settings-accordeon werkt correct op desktop en mobiel
- [ ] Bij klikken op een submenu-item sluit het mobiele menu zoals bestaande navigatie-items

## Tasks

- [ ] Pas de navigatie-markup aan in LocalFinanceManager/Components/Layout/NavMenu.razor:
  - [ ] Voeg een Settings-accordeon toe met tandwiel-icoon
  - [ ] Verplaats Automatisch toewijzen en Back-up & herstel naar de accordeon
  - [ ] Verplaats Beheer naar de onderkant van de navigatielijst
- [ ] Breid de component state in NavMenu uit voor accordion open/dicht gedrag (inclusief mobiel gedrag)
- [ ] Voeg of update data-testid attributen voor stabiele UI-tests op accordion en submenu-items
- [ ] Update styling in LocalFinanceManager/Components/Layout/NavMenu.razor.css voor:
  - [ ] Accordion header, chevron-state en submenu-indenting
  - [ ] Toegankelijke focus- en hover-states
  - [ ] Consistente spacing in collapsed en expanded sidebar-modus
- [ ] Update unit/component tests in tests/LocalFinanceManager.Tests/Components/NavMenuTests.cs:
  - [ ] Verifieer dat Settings zichtbaar is voor ingelogde gebruikers
  - [ ] Verifieer dat Automatisch toewijzen en Back-up & herstel onder Settings vallen
  - [ ] Verifieer dat Beheer niet zichtbaar is voor non-admin gebruikers
- [ ] Update admin/e2e tests in tests/LocalFinanceManager.E2E/Admin/AdminPanelTests.cs:
  - [ ] Verifieer dat Beheer zichtbaar blijft voor admin gebruikers
  - [ ] Verifieer dat Beheer onderaan het menu staat
- [ ] Voeg e2e navigatietest toe of breid uit in tests/LocalFinanceManager.E2E/UX/ThemeAndNavTests.cs:
  - [ ] Accordion open/dicht gedrag op mobiel
  - [ ] Navigatie via submenu-items sluit mobiele sidebar correct

## Notes

- Scope: alleen herstructurering van navigatie-UI in de sidebar
- Out of scope: wijzigingen aan autorisatie, route-definities of backend-functionaliteit
- Houd bestaande labelnamen en routes intact om regressie in gebruikersflows te voorkomen