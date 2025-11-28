# Improvements

## Improvement 1: Accountnumber should always be type IBAN

Currently, account numbers are stored as plain strings without validation. This should be changed to:

- Validate all account numbers against IBAN format
- Add IBAN validation on input fields
- Convert existing account numbers to IBAN format
- Display proper error messages for invalid IBANs

## Improvement 2: Update layout to be more modern

## Improvement 3: Application should be an executable

## Improvement 4: Database should be a shared file in for example onedrive

## Improvement 5: Create option to add, list and delete accounts
