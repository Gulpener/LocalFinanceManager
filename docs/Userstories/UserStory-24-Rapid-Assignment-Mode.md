# UserStory-24: Rapid Assignment Mode (Doorlopen-modus)

## Status

- [ ] Not Started

## Description

Als gebruiker die veel ongeassignde transacties heeft, wil ik een "Doorlopen-modus" waarbij na het opslaan van een toewijzing automatisch de volgende niet-toegewezen transactie in de modal verschijnt, zodat ik snel door alle transacties heen kan werken zonder steeds de modal handmatig te openen.

## Acceptance Criteria

- [ ] Er is een "Doorlopen modus" toggle zichtbaar in de filterbalk op de transactiepagina
- [ ] Wanneer de toggle aan staat en een toewijzing wordt opgeslagen, opent de modal automatisch voor de volgende niet-toegewezen transactie
- [ ] "Volgende transactie" is gedefinieerd als de eerstvolgende `!IsAssigned` transactie ná de huidige positie in de gefilterde lijst (volgorde: datum aflopend)
- [ ] Als er geen volgende niet-toegewezen transactie is, sluit de modal normaal
- [ ] Wanneer de gebruiker de modal handmatig sluit (Cancel/Escape), stopt het doorlopen — modal opent niet automatisch
- [ ] Wanneer de toggle uit staat, is het gedrag identiek aan de huidige situatie
- [ ] De toggle-staat blijft behouden zolang de pagina open is (niet persistent over sessies)

## Tasks

- [ ] Voeg `private bool rapidMode = false;` en `private TransactionDto? _rapidNextTransaction = null;` toe aan `Transactions.razor` @code
- [ ] Voeg "Doorlopen modus" checkbox-toggle toe in de filterbalk van `Transactions.razor`
- [ ] Wijzig `OnAssignmentSuccess()` in `Transactions.razor`: sla index van huidige transactie op vóór reload; zoek na reload+ApplyFilters de eerste `!IsAssigned` transactie vanaf die index; sla op in `_rapidNextTransaction` als `rapidMode` aan staat
- [ ] Wijzig `CloseAssignModal()` in `Transactions.razor`: als `_rapidNextTransaction != null`, heropen modal voor die transactie en reset `_rapidNextTransaction`
- [ ] Zorg dat expliciete sluiting (Cancel/Escape via `OnClose` callback) `_rapidNextTransaction` wist zodat doorlopen stopt
- [ ] Voeg unit tests toe in `TransactionsTests` voor de next-transaction-logica
- [ ] Voeg e2e test toe: activeer doorlopen-modus, wijs twee transacties toe, verifieer dat modal automatisch doorgaat

## Notes

- Alle wijzigingen zitten uitsluitend in `Transactions.razor`; `TransactionAssignModal.razor` blijft ongewijzigd
- `_rapidNextTransaction` wordt gezet in `OnAssignmentSuccess()` (na reload) en geconsumeerd in `CloseAssignModal()`
- Expliciete sluiting via Cancel/Escape roept `CloseAssignModal()` aan via de bestaande `OnClose` EventCallback — hier moet `_rapidNextTransaction = null` worden gezet vóór de normale sluitlogica om doorlopen te stoppen
