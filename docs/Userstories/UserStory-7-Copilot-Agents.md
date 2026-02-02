# UserStory-7: GitHub Copilot Custom Agents & Workspace Instructions

## Objective

Enhance GitHub Copilot's context awareness and suggestion quality by implementing domain-specific workspace instructions, virtual agent documentation, and preparing for future multi-agent support with template agent definition files.

## User Story

**As a** developer working on LocalFinanceManager  
**I want** GitHub Copilot to provide context-aware suggestions based on the specific domain I'm working in (ML, Backend, Frontend, Testing, Database)  
**So that** I get more relevant code suggestions, better architectural guidance, and consistent patterns across the codebase without manually specifying context each time.

## Background

GitHub Copilot's custom agent feature (released January 2026) enables workspace-specific instructions and domain-specialized agents. While the multi-agent system with `.github/agents/` YAML files is newly released, we can immediately benefit from enhanced workspace instructions and prepare for future agent capabilities.

**Current State:**

- Single `.github/copilot-instructions.md` with general project rules
- No domain-specific context sections
- No agent documentation or templates

**Desired State:**

- Domain-specific sections in copilot-instructions.md (ML, Backend, Frontend, Testing, Database)
- Virtual agent documentation in `docs/agents/` for developer reference
- Template `.github/agents/*.yml.template` files ready for activation
- Updated user story template with "Copilot Context" section
- Improved suggestion quality through better context boundaries

## Success Criteria

- ✅ `.github/copilot-instructions.md` extended with 5 domain-specific sections (ML, Backend, Frontend, Testing, Database)
- ✅ Each domain section includes: trigger keywords, relevant file patterns, key implementation patterns, and handoff protocols
- ✅ `docs/agents/` directory created with 6 markdown files documenting virtual agents
- ✅ `.github/agents/` directory created with 5 template YAML files (ml-agent, backend-agent, frontend-agent, testing-agent, database-agent)
- ✅ User story template updated with "Copilot Context" section
- ✅ Handoff protocols documented in `docs/agents/handoff-protocols.md`
- ✅ Agent feature availability verified via VS Code settings (`chat.agent.enabled`)
- ✅ Documentation updated with instructions for using domain context markers
- ✅ No build artifacts or database files committed during implementation

## Acceptance Criteria

**Given** a developer working on ML.NET model training code  
**When** they ask Copilot for help with feature extraction  
**Then** Copilot should reference MLOptions.cs, IMLService interface, and database storage patterns automatically

**Given** a developer implementing a new Blazor component  
**When** they request form validation logic  
**Then** Copilot should suggest FluentValidation patterns and CategorySelector integration

**Given** a developer writing API controller tests  
**When** they ask for integration test examples  
**Then** Copilot should reference TestDbContextFactory and in-memory SQLite patterns

**Given** the user story template is used for new stories  
**When** creating UserStory-17 or beyond  
**Then** the template includes a "Copilot Context" section with domain, files, and handoff requirements

## Implementation Tasks

### 1. Extend .github/copilot-instructions.md with Domain Sections

- [ ] Add "## Domain-Specific Guidelines" header after "Algemene regels" section
- [ ] Create "### ML Development Context" section with:
  - Trigger keywords: "machine learning", "ML model", "category prediction", "feature extraction"
  - File patterns: `LocalFinanceManager.ML/**`, `Controllers/MLController.cs`, `Services/**/*ML*.cs`
  - Key patterns: IMLService interface, database model storage (no filesystem), F1 ≥ 0.85 threshold, min 10 examples/category
  - Reference files: Configuration/MLOptions.cs, docs/Userstories/UserStory-8-ML-Suggestion-Auto-Apply.md
- [ ] Create "### Blazor UI Development Context" section with:
  - Trigger keywords: "Blazor component", "Razor page", "form validation", "Interactive Server"
  - File patterns: `Components/**/*.razor`, `Components/Pages/*`, `Components/Shared/*`
  - Key patterns: CategorySelector.razor embedding, FluentValidation, CascadingParameter, JavaScript interop
  - Reference files: Components/Shared/CategorySelector.razor
- [ ] Create "### API Development Context" section with:
  - Trigger keywords: "API controller", "endpoint", "REST API", "HTTP"
  - File patterns: `Controllers/**/*.cs`, `Services/**/*.cs`, `DTOs/**/*.cs`
  - Key patterns: RFC 7231 Problem Details, DbUpdateConcurrencyException → HTTP 409, IRepository<T> pattern, async/await
  - Reference files: docs/Implementation-Guidelines.md
- [ ] Create "### Testing Context" section with:
  - Trigger keywords: "test", "unit test", "integration test", "E2E", "Playwright"
  - File patterns: `tests/**/*.cs`, `**/*Tests.cs`
  - Key patterns: xUnit/NUnit, in-memory SQLite (:memory:), WebApplicationFactory, Playwright PageObjectModel
  - Reference files: tests/LocalFinanceManager.Tests/TestDbContextFactory.cs
- [ ] Create "### Database Development Context" section with:
  - Trigger keywords: "entity", "migration", "EF Core", "DbContext", "repository"
  - File patterns: `Models/**/*.cs`, `Data/**/*.cs`, `Migrations/**/*.cs`
  - Key patterns: BaseEntity inheritance, RowVersion concurrency, soft-delete (IsArchived), code-first migrations
  - Reference files: Models/BaseEntity.cs, Data/AppDbContext.cs

### 2. Create Virtual Agent Documentation

- [ ] Create `docs/agents/` directory
- [ ] Create `docs/agents/ml-development.md` documenting:
  - Trigger keywords and file patterns
  - Primary responsibilities (model training, feature extraction, inference, MLController integration)
  - Key files and context (LocalFinanceManager.ML/\*, MLOptions.cs)
  - Implementation patterns (SDCA trainer, database storage, F1 threshold)
  - Handoff protocols (→ testing-agent for validation tests, → backend-agent for API integration)
- [ ] Create `docs/agents/backend-development.md` documenting:
  - API controller development, service implementation, repository patterns
  - Handoffs (→ testing-agent for API tests, → database-agent for entity changes)
- [ ] Create `docs/agents/frontend-development.md` documenting:
  - Blazor component creation, Razor syntax, form validation
  - Handoffs (→ testing-agent for E2E tests, → backend-agent for API integration)
- [ ] Create `docs/agents/database-development.md` documenting:
  - Entity creation, migrations, DbContext configuration, seeding
  - Handoffs (→ testing-agent for integration tests, → backend-agent for repositories)
- [ ] Create `docs/agents/testing-development.md` documenting:
  - Unit/integration/E2E test creation, test fixtures, assertions
  - Handoffs (→ documentation-agent for test docs)
- [ ] Create `docs/agents/handoff-protocols.md` documenting:
  - When to hand off between domains (completion criteria)
  - What context to pass (entity requirements, API signatures, test scenarios)
  - Example handoff workflows (ML → Testing → Backend)

### 3. Create Template Agent Definition Files

- [ ] Create `.github/agents/` directory
- [ ] Create `.github/agents/README.md` explaining:
  - Purpose of template files (ready for GitHub Copilot multi-agent GA)
  - Current status (custom agents available, YAML schema emerging)
  - Activation instructions (rename .template → .yml when ready)
- [ ] Create `.github/agents/ml-agent.yml.template` with YAML structure:
  - `name: ml-development`
  - `trigger:` keywords, file_patterns, contexts (@ml)
  - `skills:` ml-model-training, model-evaluation, inference-optimization, ml-api-integration
  - `context:` include/exclude patterns, semantic context
  - `instructions:` ML-specific guidelines from copilot-instructions.md
  - `handoffs:` to testing-agent, backend-agent, database-agent with conditions
  - `constraints:` database-only storage, min examples, F1 threshold
- [ ] Create `.github/agents/backend-agent.yml.template` with:
  - API controller, service, repository development skills
  - Handoffs to testing-agent and database-agent
- [ ] Create `.github/agents/frontend-agent.yml.template` with:
  - Blazor component, Razor syntax, form validation skills
  - Handoffs to testing-agent and backend-agent
- [ ] Create `.github/agents/testing-agent.yml.template` with:
  - Unit, integration, E2E test creation skills
  - Handoffs to documentation-agent
- [ ] Create `.github/agents/database-agent.yml.template` with:
  - Entity, migration, DbContext configuration skills
  - Handoffs to testing-agent and backend-agent

### 4. Update User Story Template

- [ ] Add "## Copilot Context" section to user story template (use UserStory-16 as template basis)
- [ ] Template section should include:
  - **Domain:** Backend / Frontend / ML / Database / Testing / Multi-Domain
  - **Primary Agent (when available):** backend-agent / ml-agent / frontend-agent / etc.
  - **Key Files:** [List of relevant files for context]
  - **Related Stories:** [Links to dependent stories]
  - **Handoff Requirements:** [Checklist of required handoffs]
- [ ] Update existing active user stories (UserStory-8 through UserStory-16) with Copilot Context section

### 5. Documentation & Best Practices

- [ ] Create `docs/copilot-best-practices.md` with:
  - How to use domain context markers (`[ML CONTEXT]`, `[TESTING CONTEXT]`)
  - Effective prompt patterns for each domain
  - Using `@workspace` chat participant for semantic queries
  - Using `#file`, `#codebase` mentions for specific context
  - Examples of good vs. poor prompts
- [ ] Update `CONTRIBUTING.md` with Copilot usage guidelines:
  - Reference domain-specific sections when asking for help
  - Use context markers in commit messages or PR descriptions
  - Leverage virtual agent documentation for onboarding
- [ ] Add VS Code settings verification instructions to README.md:
  - Check `github.copilot.chat.agent.enabled: true`
  - Verify agent picker appears in Chat view (Ctrl+Alt+I)

### 6. Verification & Testing

- [ ] Verify `.github/copilot-instructions.md` has 5 domain sections with complete context
- [ ] Verify `docs/agents/` contains 6 markdown files (5 agents + handoff protocols)
- [ ] Verify `.github/agents/` contains 5 template YAML files + README
- [ ] Test Copilot suggestions in ML context (open FeatureExtractor.cs, ask for optimization)
- [ ] Test Copilot suggestions in Blazor context (open .razor file, ask for form validation)
- [ ] Test Copilot suggestions in API context (open controller, ask for new endpoint)
- [ ] Verify no build artifacts committed (`git status` shows only .md and .yml.template files)
- [ ] Verify VS Code settings show agent picker in Chat view

## Dependencies

- **GitHub Copilot Subscription:** REQUIRED - Individual, Business, or Enterprise plan with chat enabled
- **VS Code Extension:** REQUIRED - GitHub Copilot extension version with custom agent support (v1.x or v2.x from January 2026+)
- **Existing copilot-instructions.md:** REQUIRED - Base file to extend (already exists)
- **User Stories 1-6:** OPTIONAL - Referenced in domain contexts but not blocking

## Estimated Effort

**1-2 days** (~25 implementation tasks: 5 domain sections + 6 docs/agents files + 5 template files + 9 updates/verification)

## Roadmap Timing

**Phase 2** (Weeks 3-4, before UserStory-8 ML implementation) - Enables better Copilot suggestions during ML feature development

## Technical Notes

### Domain Section Template

Each domain section in copilot-instructions.md follows this pattern:

```markdown
### [Domain] Development Context

When working on [domain-specific files] (`pattern/**`):

- Use [key interface/pattern]
- Store [data] in [location] (constraints)
- Follow [specific standard/threshold]
- Files: [key files to reference]
```

### YAML Agent Template Structure

```yaml
name: [agent-name]
version: "1.0"
description: "Specialized agent for [domain]"

trigger:
  keywords: ["keyword1", "keyword2"]
  file_patterns: ["pattern/**"]
  contexts: ["@agent"]

skills:
  - name: skill-name
    description: "Skill description"
    capabilities: ["cap1", "cap2"]

context:
  include: ["files/**"]
  exclude: ["**/bin/**"]

instructions: |
  Domain-specific instructions...

handoffs:
  - to: target-agent
    when: "Trigger condition"
    context: "Context to pass"

constraints:
  - "Constraint 1"
  - "Constraint 2"
```

### Handoff Protocol Pattern

```markdown
### From [Source Agent] to [Target Agent]

**When:** [Completion criteria]
**Context to provide:**

- [Item 1]
- [Item 2]

**What target agent needs:**

- [Action 1]
- [Action 2]
```

## CLI Commands Reference

```powershell
# Verify GitHub Copilot extension is installed
code --list-extensions | Select-String -Pattern "github.copilot"

# Check VS Code settings for agent support
code --list-extensions | Select-String -Pattern "github.copilot"
# Then manually check Settings > Extensions > GitHub Copilot > Chat: Agent Enabled

# Verify no build artifacts before commit
git status
# Should show only .md and .yml.template files

# Stage only documentation files
git add docs/agents/
git add docs/Userstories/UserStory-7-Copilot-Agents.md
git add .github/agents/
git add .github/copilot-instructions.md
git add docs/copilot-best-practices.md

# Commit with descriptive message
git commit -m "Add UserStory-7: GitHub Copilot custom agents and domain-specific instructions

- Extend copilot-instructions.md with 5 domain sections
- Create docs/agents/ virtual agent documentation
- Add .github/agents/*.yml.template files for future multi-agent support
- Update user story template with Copilot Context section
- Add copilot-best-practices.md for effective prompt patterns"
```

## Definition of Done

- [ ] All 25 implementation tasks completed
- [ ] `.github/copilot-instructions.md` has 5 domain-specific sections with complete context
- [ ] `docs/agents/` contains 6 markdown files (5 domain agents + handoff protocols)
- [ ] `.github/agents/` contains 5 YAML template files + README
- [ ] User story template updated with Copilot Context section
- [ ] `docs/copilot-best-practices.md` created with prompt examples
- [ ] `CONTRIBUTING.md` updated with Copilot usage guidelines
- [ ] Copilot suggestions tested in 3+ domains (ML, Blazor, API) with improved relevance
- [ ] No build artifacts committed (verified via `git status`)
- [ ] VS Code agent picker verified working (Ctrl+Alt+I shows Agent/Plan/Ask/Edit options)
- [ ] Documentation reviewed for clarity and completeness
- [ ] Story marked complete in `docs/TODO.md`

## Future Enhancements (Post-Story)

- Monitor GitHub Copilot releases for YAML schema finalization
- Activate template agents when multi-agent system is fully GA
- Create additional specialized agents (devops-agent, documentation-agent)
- Implement custom prompt files with `/` commands
- Integrate MCP servers for external tool capabilities
- Measure suggestion quality improvements via developer feedback

## Copilot Context

**Domain:** Multi-Domain (Documentation, Developer Experience)  
**Primary Agent (when available):** documentation-agent  
**Key Files:** `.github/copilot-instructions.md`, `docs/agents/*.md`, `.github/agents/*.yml.template`  
**Related Stories:** None (foundational story for improving all future development)

**Handoff Requirements:**

- After implementation → Developers can use enhanced context for all future stories
- No specific handoffs required (documentation-only story)
