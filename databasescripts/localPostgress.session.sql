-- ============================================================
-- Users with counts of all linked rows across all tables
-- Direct links use UserId; indirect links join through parent table
-- ============================================================
SELECT u."Id",
    u."Email",
    u."SupabaseUserId",
    u."IsArchived" AS "UserArchived",
    -- Direct UserId links
    (
        SELECT COUNT(*)
        FROM "Accounts" a
        WHERE a."UserId" = u."Id"
    ) AS "Accounts",
    (
        SELECT COUNT(*)
        FROM "BudgetPlans" bp
        WHERE bp."UserId" = u."Id"
    ) AS "BudgetPlans",
    (
        SELECT COUNT(*)
        FROM "Categories" c
        WHERE c."UserId" = u."Id"
    ) AS "Categories",
    (
        SELECT COUNT(*)
        FROM "Transactions" t
        WHERE t."UserId" = u."Id"
    ) AS "Transactions",
    (
        SELECT COUNT(*)
        FROM "AppSettings" s
        WHERE s."UserId" = u."Id"
    ) AS "AppSettings",
    (
        SELECT COUNT(*)
        FROM "MLModels" m
        WHERE m."UserId" = u."Id"
    ) AS "MLModels",
    (
        SELECT COUNT(*)
        FROM "LabeledExamples" le
        WHERE le."UserId" = u."Id"
    ) AS "LabeledExamples",
    -- Indirect links (via parent table)
    (
        SELECT COUNT(*)
        FROM "BudgetLines" bl
        WHERE bl."BudgetPlanId" IN (
                SELECT "Id"
                FROM "BudgetPlans"
                WHERE "UserId" = u."Id"
            )
    ) AS "BudgetLines",
    (
        SELECT COUNT(*)
        FROM "TransactionSplits" ts
        WHERE ts."TransactionId" IN (
                SELECT "Id"
                FROM "Transactions"
                WHERE "UserId" = u."Id"
            )
    ) AS "TransactionSplits",
    (
        SELECT COUNT(*)
        FROM "TransactionAudits" ta
        WHERE ta."TransactionId" IN (
                SELECT "Id"
                FROM "Transactions"
                WHERE "UserId" = u."Id"
            )
    ) AS "TransactionAudits"
FROM "Users" u
ORDER BY u."Email";