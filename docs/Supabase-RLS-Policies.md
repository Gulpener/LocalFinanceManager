# Supabase RLS Policies

Deze documentatie geeft een veilige baseline voor Row Level Security op de `public`-tabellen van LocalFinanceManager.

Belangrijke aannames:

- `auth.uid()` is de Supabase Auth UUID uit `auth.users.id`.
- De applicatietabellen gebruiken een lokale gebruiker in `public."Users"`, gekoppeld via `Users.SupabaseUserId`.
- Soft delete blijft leidend: `SELECT`-policies verbergen gearchiveerde rijen met `"IsArchived" = false`.
- Deze baseline houdt writes bewust conservatief: gedeelde gebruikers krijgen leesrechten via RLS, maar mutaties blijven in de meeste tabellen owner-only. Dat voorkomt dat een gedeelde `Editor` via directe SQL eigenaarschap of foreign keys kan herschrijven.
- Als je gedeelde `Editor`-writes rechtstreeks vanuit Supabase Client wilt toestaan, doe dat via RPC's of via aanvullende column-level `GRANT`s.

Enum-waarden uit de codebase:

- `PermissionLevel`: `Owner = 0`, `Editor = 1`, `Viewer = 2`
- `ShareStatus`: `Pending = 0`, `Accepted = 1`, `Declined = 2`

## Niet Onder RLS Zetten

Zet **geen** RLS op `public."__EFMigrationsHistory"`.

Waarom niet:

- EF Core gebruikt deze tabel intern om bij te houden welke migrations al zijn toegepast.
- `Database.MigrateAsync()` moet deze tabel zonder extra RLS-beperkingen kunnen lezen en bijwerken.
- RLS op deze tabel kan startup migrations blokkeren of onbetrouwbaar maken.

Dus:

```sql
-- Do not enable RLS on EF Core's internal migration history table.
-- Leave public."__EFMigrationsHistory" unmanaged by these policies.
```

## 0. Helper Functions

Voer dit eerst uit. Deze helpers voorkomen dat dezelfde joins in elke policy terugkomen.

```sql
create or replace function public.current_local_user_id()
returns uuid
language sql
stable
as $$
  select u."Id"
  from public."Users" u
  where u."SupabaseUserId" = auth.uid()::text
    and not u."IsArchived"
  limit 1;
$$;

create or replace function public.is_local_admin()
returns boolean
language sql
stable
as $$
  select exists (
    select 1
    from public."Users" u
    where u."Id" = public.current_local_user_id()
      and u."IsAdmin" = true
      and not u."IsArchived"
  );
$$;

create or replace function public.can_view_account(account_id uuid)
returns boolean
language sql
stable
as $$
  select
    public.is_local_admin()
    or exists (
      select 1
      from public."Accounts" a
      where a."Id" = account_id
        and not a."IsArchived"
        and a."UserId" = public.current_local_user_id()
    )
    or exists (
      select 1
      from public."AccountShares" s
      where s."AccountId" = account_id
        and s."SharedWithUserId" = public.current_local_user_id()
        and s."Status" = 1
        and not s."IsArchived"
    );
$$;

create or replace function public.is_account_owner(account_id uuid)
returns boolean
language sql
stable
as $$
  select
    public.is_local_admin()
    or exists (
      select 1
      from public."Accounts" a
      where a."Id" = account_id
        and a."UserId" = public.current_local_user_id()
        and not a."IsArchived"
    );
$$;

create or replace function public.can_view_budget_plan(plan_id uuid)
returns boolean
language sql
stable
as $$
  select
    public.is_local_admin()
    or exists (
      select 1
      from public."BudgetPlans" bp
      where bp."Id" = plan_id
        and not bp."IsArchived"
        and bp."UserId" = public.current_local_user_id()
    )
    or exists (
      select 1
      from public."BudgetPlanShares" s
      where s."BudgetPlanId" = plan_id
        and s."SharedWithUserId" = public.current_local_user_id()
        and s."Status" = 1
        and not s."IsArchived"
    );
$$;

create or replace function public.is_budget_plan_owner(plan_id uuid)
returns boolean
language sql
stable
as $$
  select
    public.is_local_admin()
    or exists (
      select 1
      from public."BudgetPlans" bp
      where bp."Id" = plan_id
        and bp."UserId" = public.current_local_user_id()
        and not bp."IsArchived"
    );
$$;

create or replace function public.can_view_transaction(tx_id uuid)
returns boolean
language sql
stable
as $$
  select
    public.is_local_admin()
    or exists (
      select 1
      from public."Transactions" t
      where t."Id" = tx_id
        and not t."IsArchived"
        and (
          t."UserId" = public.current_local_user_id()
          or public.can_view_account(t."AccountId")
          or exists (
            select 1
            from public."Accounts" a
            where a."Id" = t."AccountId"
              and a."CurrentBudgetPlanId" is not null
              and public.can_view_budget_plan(a."CurrentBudgetPlanId")
          )
        )
    );
$$;

create or replace function public.is_transaction_owner(tx_id uuid)
returns boolean
language sql
stable
as $$
  select
    public.is_local_admin()
    or exists (
      select 1
      from public."Transactions" t
      where t."Id" = tx_id
        and t."UserId" = public.current_local_user_id()
        and not t."IsArchived"
    );
$$;
```

## 1. Users

Gebruiker mag alleen zijn eigen profielrij zien en aanpassen. Admin mag alle rijen lezen en aanpassen.

```sql
alter table public."Users" enable row level security;
alter table public."Users" force row level security;

drop policy if exists users_select_self_or_admin on public."Users";
create policy users_select_self_or_admin
on public."Users"
for select
to authenticated
using (
  not "IsArchived"
  and (
    "Id" = public.current_local_user_id()
    or public.is_local_admin()
  )
);

drop policy if exists users_update_self_or_admin on public."Users";
create policy users_update_self_or_admin
on public."Users"
for update
to authenticated
using (
  "Id" = public.current_local_user_id()
  or public.is_local_admin()
)
with check (
  "Id" = public.current_local_user_id()
  or public.is_local_admin()
);
```

## 2. UserPreferences

```sql
alter table public."UserPreferences" enable row level security;
alter table public."UserPreferences" force row level security;

drop policy if exists user_preferences_select_own on public."UserPreferences";
create policy user_preferences_select_own
on public."UserPreferences"
for select
to authenticated
using (
  not "IsArchived"
  and (
    "UserId" = public.current_local_user_id()
    or public.is_local_admin()
  )
);

drop policy if exists user_preferences_insert_own on public."UserPreferences";
create policy user_preferences_insert_own
on public."UserPreferences"
for insert
to authenticated
with check (
  "UserId" = public.current_local_user_id()
  or public.is_local_admin()
);

drop policy if exists user_preferences_update_own on public."UserPreferences";
create policy user_preferences_update_own
on public."UserPreferences"
for update
to authenticated
using (
  "UserId" = public.current_local_user_id()
  or public.is_local_admin()
)
with check (
  "UserId" = public.current_local_user_id()
  or public.is_local_admin()
);

drop policy if exists user_preferences_delete_own on public."UserPreferences";
create policy user_preferences_delete_own
on public."UserPreferences"
for delete
to authenticated
using (
  "UserId" = public.current_local_user_id()
  or public.is_local_admin()
);
```

## 3. AppSettings

```sql
alter table public."AppSettings" enable row level security;
alter table public."AppSettings" force row level security;

drop policy if exists app_settings_select_own on public."AppSettings";
create policy app_settings_select_own
on public."AppSettings"
for select
to authenticated
using (
  not "IsArchived"
  and (
    "UserId" = public.current_local_user_id()
    or public.is_local_admin()
  )
);

drop policy if exists app_settings_insert_own on public."AppSettings";
create policy app_settings_insert_own
on public."AppSettings"
for insert
to authenticated
with check (
  "UserId" = public.current_local_user_id()
  or public.is_local_admin()
);

drop policy if exists app_settings_update_own on public."AppSettings";
create policy app_settings_update_own
on public."AppSettings"
for update
to authenticated
using (
  "UserId" = public.current_local_user_id()
  or public.is_local_admin()
)
with check (
  "UserId" = public.current_local_user_id()
  or public.is_local_admin()
);

drop policy if exists app_settings_delete_own on public."AppSettings";
create policy app_settings_delete_own
on public."AppSettings"
for delete
to authenticated
using (
  "UserId" = public.current_local_user_id()
  or public.is_local_admin()
);
```

## 4. Accounts

Lezen volgt eigenaar of geaccepteerde account-share. Mutaties blijven owner-only.

```sql
alter table public."Accounts" enable row level security;
alter table public."Accounts" force row level security;

drop policy if exists accounts_select_visible on public."Accounts";
create policy accounts_select_visible
on public."Accounts"
for select
to authenticated
using (
  not "IsArchived"
  and public.can_view_account("Id")
);

drop policy if exists accounts_insert_owner_only on public."Accounts";
create policy accounts_insert_owner_only
on public."Accounts"
for insert
to authenticated
with check (
  "UserId" = public.current_local_user_id()
  or public.is_local_admin()
);

drop policy if exists accounts_update_owner_only on public."Accounts";
create policy accounts_update_owner_only
on public."Accounts"
for update
to authenticated
using (public.is_account_owner("Id"))
with check (public.is_account_owner("Id"));

drop policy if exists accounts_delete_owner_only on public."Accounts";
create policy accounts_delete_owner_only
on public."Accounts"
for delete
to authenticated
using (public.is_account_owner("Id"));
```

## 5. AccountShares

Owner ziet en beheert shares die hij heeft uitgegeven. Ontvanger ziet eigen invites. Voor accept/decline raad ik RPC's of backend API aan; pure RLS kan niet veilig afdwingen dat de ontvanger alleen `Status` wijzigt.

```sql
alter table public."AccountShares" enable row level security;
alter table public."AccountShares" force row level security;

drop policy if exists account_shares_select_owner_or_recipient on public."AccountShares";
create policy account_shares_select_owner_or_recipient
on public."AccountShares"
for select
to authenticated
using (
  not "IsArchived"
  and (
    "UserId" = public.current_local_user_id()
    or "SharedWithUserId" = public.current_local_user_id()
    or public.is_local_admin()
  )
);

drop policy if exists account_shares_insert_owner_only on public."AccountShares";
create policy account_shares_insert_owner_only
on public."AccountShares"
for insert
to authenticated
with check (
  (
    "UserId" = public.current_local_user_id()
    and public.is_account_owner("AccountId")
  )
  or public.is_local_admin()
);

drop policy if exists account_shares_update_owner_only on public."AccountShares";
create policy account_shares_update_owner_only
on public."AccountShares"
for update
to authenticated
using (
  "UserId" = public.current_local_user_id()
  or public.is_local_admin()
)
with check (
  "UserId" = public.current_local_user_id()
  or public.is_local_admin()
);

drop policy if exists account_shares_delete_owner_only on public."AccountShares";
create policy account_shares_delete_owner_only
on public."AccountShares"
for delete
to authenticated
using (
  "UserId" = public.current_local_user_id()
  or public.is_local_admin()
);
```

## 6. BudgetPlans

Lezen volgt eigenaar of geaccepteerde budgetplan-share. Mutaties blijven owner-only.

```sql
alter table public."BudgetPlans" enable row level security;
alter table public."BudgetPlans" force row level security;

drop policy if exists budget_plans_select_visible on public."BudgetPlans";
create policy budget_plans_select_visible
on public."BudgetPlans"
for select
to authenticated
using (
  not "IsArchived"
  and public.can_view_budget_plan("Id")
);

drop policy if exists budget_plans_insert_owner_only on public."BudgetPlans";
create policy budget_plans_insert_owner_only
on public."BudgetPlans"
for insert
to authenticated
with check (
  (
    "UserId" = public.current_local_user_id()
    and public.is_account_owner("AccountId")
  )
  or public.is_local_admin()
);

drop policy if exists budget_plans_update_owner_only on public."BudgetPlans";
create policy budget_plans_update_owner_only
on public."BudgetPlans"
for update
to authenticated
using (public.is_budget_plan_owner("Id"))
with check (public.is_budget_plan_owner("Id"));

drop policy if exists budget_plans_delete_owner_only on public."BudgetPlans";
create policy budget_plans_delete_owner_only
on public."BudgetPlans"
for delete
to authenticated
using (public.is_budget_plan_owner("Id"));
```

## 7. BudgetPlanShares

Zelfde patroon als bij `AccountShares`.

```sql
alter table public."BudgetPlanShares" enable row level security;
alter table public."BudgetPlanShares" force row level security;

drop policy if exists budget_plan_shares_select_owner_or_recipient on public."BudgetPlanShares";
create policy budget_plan_shares_select_owner_or_recipient
on public."BudgetPlanShares"
for select
to authenticated
using (
  not "IsArchived"
  and (
    "UserId" = public.current_local_user_id()
    or "SharedWithUserId" = public.current_local_user_id()
    or public.is_local_admin()
  )
);

drop policy if exists budget_plan_shares_insert_owner_only on public."BudgetPlanShares";
create policy budget_plan_shares_insert_owner_only
on public."BudgetPlanShares"
for insert
to authenticated
with check (
  (
    "UserId" = public.current_local_user_id()
    and public.is_budget_plan_owner("BudgetPlanId")
  )
  or public.is_local_admin()
);

drop policy if exists budget_plan_shares_update_owner_only on public."BudgetPlanShares";
create policy budget_plan_shares_update_owner_only
on public."BudgetPlanShares"
for update
to authenticated
using (
  "UserId" = public.current_local_user_id()
  or public.is_local_admin()
)
with check (
  "UserId" = public.current_local_user_id()
  or public.is_local_admin()
);

drop policy if exists budget_plan_shares_delete_owner_only on public."BudgetPlanShares";
create policy budget_plan_shares_delete_owner_only
on public."BudgetPlanShares"
for delete
to authenticated
using (
  "UserId" = public.current_local_user_id()
  or public.is_local_admin()
);
```

## 8. Categories

Categorieen volgen het gekoppelde budgetplan. Let op: de huidige `CategoryRepository` gebruikt nog owner-only filtering; onderstaande policy is dus iets ruimer en volgt de bedoelde share-cascade.

```sql
alter table public."Categories" enable row level security;
alter table public."Categories" force row level security;

drop policy if exists categories_select_visible on public."Categories";
create policy categories_select_visible
on public."Categories"
for select
to authenticated
using (
  not "IsArchived"
  and (
    public.is_local_admin()
    or public.can_view_budget_plan("BudgetPlanId")
  )
);

drop policy if exists categories_insert_owner_only on public."Categories";
create policy categories_insert_owner_only
on public."Categories"
for insert
to authenticated
with check (
  (
    "UserId" = public.current_local_user_id()
    and public.is_budget_plan_owner("BudgetPlanId")
  )
  or public.is_local_admin()
);

drop policy if exists categories_update_owner_only on public."Categories";
create policy categories_update_owner_only
on public."Categories"
for update
to authenticated
using (
  public.is_local_admin()
  or "UserId" = public.current_local_user_id()
)
with check (
  public.is_local_admin()
  or "UserId" = public.current_local_user_id()
);

drop policy if exists categories_delete_owner_only on public."Categories";
create policy categories_delete_owner_only
on public."Categories"
for delete
to authenticated
using (
  public.is_local_admin()
  or "UserId" = public.current_local_user_id()
);
```

## 9. BudgetLines

Budgetregels volgen het gekoppelde budgetplan.

```sql
alter table public."BudgetLines" enable row level security;
alter table public."BudgetLines" force row level security;

drop policy if exists budget_lines_select_visible on public."BudgetLines";
create policy budget_lines_select_visible
on public."BudgetLines"
for select
to authenticated
using (
  not "IsArchived"
  and (
    public.is_local_admin()
    or public.can_view_budget_plan("BudgetPlanId")
  )
);

drop policy if exists budget_lines_insert_owner_only on public."BudgetLines";
create policy budget_lines_insert_owner_only
on public."BudgetLines"
for insert
to authenticated
with check (
  public.is_local_admin()
  or public.is_budget_plan_owner("BudgetPlanId")
);

drop policy if exists budget_lines_update_owner_only on public."BudgetLines";
create policy budget_lines_update_owner_only
on public."BudgetLines"
for update
to authenticated
using (
  public.is_local_admin()
  or public.is_budget_plan_owner("BudgetPlanId")
)
with check (
  public.is_local_admin()
  or public.is_budget_plan_owner("BudgetPlanId")
);

drop policy if exists budget_lines_delete_owner_only on public."BudgetLines";
create policy budget_lines_delete_owner_only
on public."BudgetLines"
for delete
to authenticated
using (
  public.is_local_admin()
  or public.is_budget_plan_owner("BudgetPlanId")
);
```

## 10. Transactions

Lezen volgt directe eigenaar, gedeeld account, of een geaccepteerd gedeeld budgetplan via `Accounts.CurrentBudgetPlanId`, exact zoals de repository nu doet.

```sql
alter table public."Transactions" enable row level security;
alter table public."Transactions" force row level security;

drop policy if exists transactions_select_visible on public."Transactions";
create policy transactions_select_visible
on public."Transactions"
for select
to authenticated
using (
  not "IsArchived"
  and public.can_view_transaction("Id")
);

drop policy if exists transactions_insert_owner_only on public."Transactions";
create policy transactions_insert_owner_only
on public."Transactions"
for insert
to authenticated
with check (
  (
    "UserId" = public.current_local_user_id()
    and public.is_account_owner("AccountId")
  )
  or public.is_local_admin()
);

drop policy if exists transactions_update_owner_only on public."Transactions";
create policy transactions_update_owner_only
on public."Transactions"
for update
to authenticated
using (public.is_transaction_owner("Id"))
with check (public.is_transaction_owner("Id"));

drop policy if exists transactions_delete_owner_only on public."Transactions";
create policy transactions_delete_owner_only
on public."Transactions"
for delete
to authenticated
using (public.is_transaction_owner("Id"));
```

## 11. TransactionSplits

Splits volgen de parent-transactie.

```sql
alter table public."TransactionSplits" enable row level security;
alter table public."TransactionSplits" force row level security;

drop policy if exists transaction_splits_select_visible on public."TransactionSplits";
create policy transaction_splits_select_visible
on public."TransactionSplits"
for select
to authenticated
using (
  not "IsArchived"
  and (
    public.is_local_admin()
    or public.can_view_transaction("TransactionId")
  )
);

drop policy if exists transaction_splits_insert_owner_only on public."TransactionSplits";
create policy transaction_splits_insert_owner_only
on public."TransactionSplits"
for insert
to authenticated
with check (
  public.is_local_admin()
  or public.is_transaction_owner("TransactionId")
);

drop policy if exists transaction_splits_update_owner_only on public."TransactionSplits";
create policy transaction_splits_update_owner_only
on public."TransactionSplits"
for update
to authenticated
using (
  public.is_local_admin()
  or public.is_transaction_owner("TransactionId")
)
with check (
  public.is_local_admin()
  or public.is_transaction_owner("TransactionId")
);

drop policy if exists transaction_splits_delete_owner_only on public."TransactionSplits";
create policy transaction_splits_delete_owner_only
on public."TransactionSplits"
for delete
to authenticated
using (
  public.is_local_admin()
  or public.is_transaction_owner("TransactionId")
);
```

## 12. TransactionAudits

Auditregels zijn leesbaar voor wie de onderliggende transactie mag zien. Inserts laat je bij voorkeur alleen via backend/service-role lopen.

```sql
alter table public."TransactionAudits" enable row level security;
alter table public."TransactionAudits" force row level security;

drop policy if exists transaction_audits_select_visible on public."TransactionAudits";
create policy transaction_audits_select_visible
on public."TransactionAudits"
for select
to authenticated
using (
  not "IsArchived"
  and (
    public.is_local_admin()
    or public.can_view_transaction("TransactionId")
  )
);
```

## 13. LabeledExamples

Deze trainingsdata volgt de transactie en categorie. Writes blijven owner-only van de brontransactie.

```sql
alter table public."LabeledExamples" enable row level security;
alter table public."LabeledExamples" force row level security;

drop policy if exists labeled_examples_select_visible on public."LabeledExamples";
create policy labeled_examples_select_visible
on public."LabeledExamples"
for select
to authenticated
using (
  not "IsArchived"
  and (
    public.is_local_admin()
    or public.can_view_transaction("TransactionId")
  )
);

drop policy if exists labeled_examples_insert_owner_only on public."LabeledExamples";
create policy labeled_examples_insert_owner_only
on public."LabeledExamples"
for insert
to authenticated
with check (
  public.is_local_admin()
  or public.is_transaction_owner("TransactionId")
);

drop policy if exists labeled_examples_update_owner_only on public."LabeledExamples";
create policy labeled_examples_update_owner_only
on public."LabeledExamples"
for update
to authenticated
using (
  public.is_local_admin()
  or public.is_transaction_owner("TransactionId")
)
with check (
  public.is_local_admin()
  or public.is_transaction_owner("TransactionId")
);

drop policy if exists labeled_examples_delete_owner_only on public."LabeledExamples";
create policy labeled_examples_delete_owner_only
on public."LabeledExamples"
for delete
to authenticated
using (
  public.is_local_admin()
  or public.is_transaction_owner("TransactionId")
);
```

## 14. MLModels

Deze tabel is niet user-owned. Veiligste baseline: alleen admin lezen; writes via backend/service-role.

```sql
alter table public."MLModels" enable row level security;
alter table public."MLModels" force row level security;

drop policy if exists ml_models_select_admin_only on public."MLModels";
create policy ml_models_select_admin_only
on public."MLModels"
for select
to authenticated
using (
  not "IsArchived"
  and public.is_local_admin()
);
```

## 15. Storage Bucket Policy Voor Profielafbeeldingen

Dit is geen `public`-tabel van je domein, maar wel relevant voor Supabase Storage. Deze policy sluit aan op het padformaat `{userId}/{guid}.{ext}` uit de profiel-user-story.

```sql
drop policy if exists profile_pictures_select_public on storage.objects;
create policy profile_pictures_select_public
on storage.objects
for select
to public
using (bucket_id = 'profile-pictures');

drop policy if exists profile_pictures_insert_own_folder on storage.objects;
create policy profile_pictures_insert_own_folder
on storage.objects
for insert
to authenticated
with check (
  bucket_id = 'profile-pictures'
  and split_part(name, '/', 1)::uuid = public.current_local_user_id()
);

drop policy if exists profile_pictures_update_own_folder on storage.objects;
create policy profile_pictures_update_own_folder
on storage.objects
for update
to authenticated
using (
  bucket_id = 'profile-pictures'
  and split_part(name, '/', 1)::uuid = public.current_local_user_id()
)
with check (
  bucket_id = 'profile-pictures'
  and split_part(name, '/', 1)::uuid = public.current_local_user_id()
);

drop policy if exists profile_pictures_delete_own_folder on storage.objects;
create policy profile_pictures_delete_own_folder
on storage.objects
for delete
to authenticated
using (
  bucket_id = 'profile-pictures'
  and split_part(name, '/', 1)::uuid = public.current_local_user_id()
);
```

## 16. Wat Nog Open Staat

- `CategoryRepository` en delen van `BudgetLineRepository` zijn nu nog strenger dan de share-cascade in deze RLS-notitie.
- `AccountShares` en `BudgetPlanShares` accept/decline vanuit de ontvanger wil je bij voorkeur via backend endpoints of RPC's afdwingen, niet met pure table updates.
- Als je deze policies echt actief gaat gebruiken met Supabase Client-side toegang, test dan expliciet owner, viewer, editor, pending en declined flows.
