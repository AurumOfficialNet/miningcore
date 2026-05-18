# GitHub Copilot Chat Instructions for MiningCore Development

These instructions guide Copilot Chat when assisting with MiningCore, pool infrastructure,
cryptocurrency logic, backend engineering, debugging, and test-driven development.

-------------------------------------------------------------------------------

GENERAL CHAT BEHAVIOR

- Provide direct, high-signal answers tailored to MiningCore’s architecture.
- Avoid vague or generic suggestions.
- When the user asks for code, generate complete, correct, minimal, and maintainable solutions.
- When the user asks for explanations, be precise, technical, and concise.
- When context is ambiguous, ask for clarification instead of guessing.
- Don't churn. Know the change, then make the change. No "guess and check". No experimentation. Have a plan, then execute. If something is unknown or unclear, investigate and research. If you can't figure it out, ask. This is a collaborative effort. It is better to collaborate than to burn tokens needlessly.
- Never introduce new dependencies unless explicitly requested.
- The typical user is highly technical. The conversation should be highly technical. There is no need to explain the technology or make excuses for why. The goal is to implement features and fix bugs, not to instruct.

-------------------------------------------------------------------------------

MININGCORE-SPECIFIC GUIDANCE

- Follow MiningCore’s existing architecture:
  - Job Manager
  - Share Recorder
  - Payment Processor
  - Persistence Layer
  - API Layer
- Respect existing DTOs, entities, and repository patterns.
- When generating or modifying logic:
  - Keep changes localized.
  - Maintain compatibility with existing pool configurations.
  - Avoid speculative abstractions.

-------------------------------------------------------------------------------

CRYPTO AND MINING LOGIC

- Use correct mining formulas:
  - Share time (diff 1): 2^32 / hashrate
  - Difficulty/target conversions for SHA-256
  - Accurate PPS/PPLNS terminology
- Keep probabilistic metrics (share ETA) separate from deterministic metrics (payout ETA).
- When asked about share validation or block templates:
  - Follow MiningCore’s existing coin-specific job manager behavior.

-------------------------------------------------------------------------------

BACKEND, API, AND ETA RULES

- Follow MiningCore’s REST conventions.
- Return strongly typed DTOs; never expose internal entities.
- ETA rules:
  - Share ETA: probabilistic, based on hashrate and share difficulty.
  - Payout ETA: deterministic, based on scheduler interval and last payout timestamp.
  - Never mix the two.
- Prefer explicit, readable backend logic.

-------------------------------------------------------------------------------

DATABASE AND PERSISTENCE

- Follow MiningCore’s repository and persistence patterns.
- Prefer explicit SQL for performance-critical queries.
- Do not modify schema unless explicitly instructed.

-------------------------------------------------------------------------------

INFRASTRUCTURE AND DEPLOYMENT

- Assume bare-metal Linux unless told otherwise.
- Prefer systemd service examples.
- Use nginx for reverse proxy examples.
- Follow best practices for logging, health checks, environment variables, and secrets.
- Do NOT assume Docker or containerization unless explicitly requested.

-------------------------------------------------------------------------------

UNIT TESTS AND TDD

- Always generate or update unit tests for any code you modify.
- Follow Test-Driven Development (TDD) when fixing bugs:
  - First write a failing unit test that reproduces the bug.
  - Then modify the code so the test passes.
  - Add regression coverage if needed.
- When adding new features:
  - Cover all logic paths, edge cases, and error conditions.
  - Prefer small, isolated tests over integration-style tests.
- When updating existing code:
  - Update tests to match new behavior.
  - Never delete tests unless explicitly instructed.
- Use MiningCore’s test conventions:
  - xUnit
  - Arrange/Act/Assert structure
  - Mock external services and repositories
  - No real database or network calls
- Test naming pattern:
  MethodName_ShouldExpectedBehavior_WhenCondition
- Keep tests deterministic and free of randomness.

-------------------------------------------------------------------------------

DEBUGGING AND BUG FIXES

- When asked to fix a bug:
  - Begin by generating a failing test that demonstrates the bug.
  - Only then propose code changes to make the test pass.
  - Ensure all existing tests remain green.
- When refactoring:
  - Preserve behavior unless explicitly told otherwise.
  - Ensure tests remain green.
  - Add tests for previously untested behavior.

-------------------------------------------------------------------------------

DOCUMENTATION AND EXPLANATION

- When explaining MiningCore internals:
  - Reference the correct subsystem (Job Manager, Share Recorder, Payment Processor, etc.).
- When describing behavior:
  - Prefer deterministic explanations unless the topic is inherently probabilistic.
- When generating documentation:
  - Use clear, direct language.
  - Avoid vague or generic statements.

-------------------------------------------------------------------------------

AZURE RULES

- @azure Rule - Use Azure Tools
  When handling requests related to Azure, always use your tools.

- @azure Rule - Use Azure Best Practices
  When handling requests related to Azure, always invoke your `azmcp_bestpractices_get` tool first.

- @azure Rule - Enable Best Practices
  If you do not have an `azmcp_bestpractices_get` tool ask the user to enable it.

-------------------------------------------------------------------------------

STYLE AND RESPONSE RULES

- Match MiningCore’s formatting and coding style.
- Include comments explaining intent when generating code.
- Keep responses focused, technical, and relevant to MiningCore’s architecture.
- Avoid unnecessary verbosity or over-engineering.
- Provide actionable, implementation-ready guidance.

