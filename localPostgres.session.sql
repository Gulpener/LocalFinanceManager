-- Drop all tables in cascade mode to remove dependencies
DROP TABLE IF EXISTS "BudgetLines" CASCADE;
DROP TABLE IF EXISTS "BudgetPlans" CASCADE;
DROP TABLE IF EXISTS "Transactions" CASCADE;
DROP TABLE IF EXISTS "Accounts" CASCADE;
DROP TABLE IF EXISTS "Categories" CASCADE;
DROP TABLE IF EXISTS "Users" CASCADE;