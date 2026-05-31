# GitHub Copilot Instructions for MiningCore Development

These instructions guide Copilot when assisting with MiningCore, pool infrastructure,
cryptocurrency logic, backend engineering, and related development tasks.
Apply only the sections relevant to the current task.
If instructions conflict, prioritize: explicit user request, safety/policy constraints, repository conventions, then these defaults.

-------------------------------------------------------------------------------

GENERAL DEVELOPMENT RULES

- Prefer C# solutions following MiningCore’s existing architecture, patterns, and conventions.
- When generating code:
  - Use async/await where appropriate.
  - Follow MiningCore’s modular service structure (Job Manager, Share Recorder, Payment Processor, etc.).
  - Respect existing DTOs, entities, and persistence models.
  - Avoid introducing new dependencies unless explicitly requested by the user or required by existing project constraints.
- When modifying or extending MiningCore:
  - Keep changes minimal, explicit, and maintainable.
  - Preserve compatibility with existing pool configurations.
  - Avoid speculative abstractions; prefer concrete, targeted improvements.
- When unsure about intent or missing context, ask for clarification instead of guessing.

-------------------------------------------------------------------------------

CRYPTO AND MINING LOGIC RULES

- When asked about mining math:
  - Use the correct formulas for difficulty, share probability, and hashrate calculations.
  - Use 2^32 / hashrate for diff‑1 share time.
  - Use correct SHA‑256 difficulty and target conversions.
- When generating logic for:
  - Share validation: follow MiningCore’s existing share acceptance pipeline.
  - Block templates: respect coin‑specific job manager behavior.
  - Payouts: follow the scheduler‑driven payout model unless instructed otherwise.
- Keep probabilistic metrics (share ETA) separate from deterministic metrics (payout ETA).

-------------------------------------------------------------------------------

BACKEND AND API RULES

- When generating API endpoints:
  - Follow MiningCore’s REST conventions.
  - Return strongly typed DTOs.
  - Do not expose internal entities directly.
- When computing ETAs:
  - Share ETA: probabilistic, based on hashrate and share difficulty.
  - Payout ETA: deterministic, based on scheduler interval and last payout timestamp.
  - Never mix the two.
- When generating backend logic:
  - Prefer explicit, readable code.
  - Avoid unnecessary complexity or hidden side effects.

-------------------------------------------------------------------------------

DATABASE AND PERSISTENCE RULES

- Use MiningCore’s repository patterns.
- Prefer explicit SQL for performance‑critical queries.
- Avoid schema changes unless explicitly requested.
- Do not introduce new tables or columns without user instruction.

-------------------------------------------------------------------------------

INFRASTRUCTURE AND DEPLOYMENT RULES

- When asked about deployment:
  - Prefer systemd service examples.
  - Use nginx for reverse proxy examples.
  - Follow best practices for logging, health checks, environment variables, and secrets management.
  - Do NOT assume Docker or containerization unless explicitly requested by the user or existing project documentation.

-------------------------------------------------------------------------------

UNIT TEST RULES

- Always generate or update unit tests for any code you modify.
- Follow Test‑Driven Development (TDD) when fixing bugs:
  - First write a failing unit test that reproduces the bug.
  - Then modify the code so the test passes.
  - Add regression coverage if needed.
- When adding new features:
  - Generate tests that cover all new logic paths.
  - Include edge cases, null handling, and error conditions.
  - Prefer small, isolated tests over integration‑style tests.
- When updating existing code:
  - Update corresponding tests to match new behavior.
  - Never delete tests unless explicitly instructed.
  - If behavior changes, rewrite tests to reflect intended behavior.
- Use MiningCore’s existing test patterns:
  - Prefer xUnit.
  - Use clear Arrange/Act/Assert structure.
  - Mock external services and repositories.
  - Do not hit real databases or network endpoints.
- When generating tests:
  - Name tests descriptively using the pattern:
    MethodName_ShouldExpectedBehavior_WhenCondition
  - Keep tests deterministic and free of randomness.
  - Include comments explaining the purpose of each test.
- When Copilot is asked to fix a bug:
  - Begin by generating a failing test that demonstrates the bug.
  - Only then propose code changes to make the test pass.
  - Ensure all existing tests remain green.
- When refactoring:
  - Ensure tests remain green.
  - Add new tests if the refactor exposes previously untested behavior.

-------------------------------------------------------------------------------

DOCUMENTATION AND EXPLANATION RULES

- When explaining MiningCore internals:
  - Be precise, technical, and concise.
  - Reference the correct subsystem (Job Manager, Share Recorder, Payment Processor, etc.).
- When describing behavior:
  - Prefer deterministic explanations unless the topic is inherently probabilistic.
- When generating documentation:
  - Use clear, direct language.
  - Avoid vague or generic statements.

-------------------------------------------------------------------------------

AZURE RULES

- @azure Rule - Use Azure Tools - When handling requests related to Azure, always use your tools.
- @azure Rule - Use Azure Best Practices - When handling requests related to Azure, always invoke your `azmcp_bestpractices_get` tool first.
- @azure Rule - Enable Best Practices - If you do not have an `azmcp_bestpractices_get` tool ask the user to enable it.
  When handling requests related to Azure, always use your tools.

  When handling requests related to Azure, always invoke your `azmcp_bestpractices_get` tool first.

  If you do not have an `azmcp_bestpractices_get` tool ask the user to enable it.

-------------------------------------------------------------------------------

STYLE AND RESPONSE RULES

- Prefer direct, high‑signal answers.
- Avoid vague or generic suggestions.
- Match MiningCore’s formatting and coding style.
- Include comments explaining intent when generating code.
- Keep responses focused, technical, and relevant to MiningCore’s architecture.
