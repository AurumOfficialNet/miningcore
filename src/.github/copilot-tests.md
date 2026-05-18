# GitHub Copilot Test Instructions for MiningCore

These instructions guide Copilot when generating, updating, or reasoning about
unit tests for MiningCore and related backend components.

-------------------------------------------------------------------------------

GENERAL TESTING PRINCIPLES

- Always generate or update unit tests for any code that is added, modified, or refactored.
- Follow Test-Driven Development (TDD) when fixing bugs:
  - First write a failing unit test that reproduces the bug.
  - Then modify the code so the test passes.
  - Add regression coverage if needed.
- Tests must be deterministic, isolated, and fast.
- Prefer small, focused unit tests over integration-style tests.
- Never hit real databases, network endpoints, or external services.

-------------------------------------------------------------------------------

TEST FRAMEWORK AND STRUCTURE

- Use xUnit for all tests.
- Use the Arrange / Act / Assert pattern.
- Use descriptive test names following the pattern:
  MethodName_ShouldExpectedBehavior_WhenCondition
- Include comments explaining the purpose of each test.
- Keep each test focused on a single behavior.

-------------------------------------------------------------------------------

MOCKING AND ISOLATION

- Mock external services, repositories, and infrastructure dependencies.
- Use lightweight mocking frameworks (e.g., Moq) unless instructed otherwise.
- Do not instantiate real database contexts, HTTP clients, or network sockets.
- Avoid randomness, time-dependent behavior, or nondeterministic logic.
- When time is involved, mock or inject a clock abstraction.

-------------------------------------------------------------------------------

WHEN ADDING NEW FEATURES

- Generate tests that cover:
  - All new logic paths
  - Edge cases
  - Null handling
  - Error conditions
- Ensure coverage for both expected and unexpected inputs.
- Validate that new behavior does not break existing tests.

-------------------------------------------------------------------------------

WHEN FIXING BUGS

- Begin by generating a failing test that reproduces the bug.
- Only after the failing test is written should code changes be proposed.
- After the fix:
  - Ensure the new test passes.
  - Ensure all existing tests remain green.
  - Add regression tests if the bug reveals previously untested behavior.

-------------------------------------------------------------------------------

WHEN MODIFYING EXISTING CODE

- Update corresponding tests to reflect the new behavior.
- Never delete tests unless explicitly instructed.
- If behavior changes intentionally:
  - Rewrite tests to match the intended behavior.
  - Add new tests for uncovered logic.
- If behavior should not change:
  - Ensure all existing tests remain green.

-------------------------------------------------------------------------------

MININGCORE-SPECIFIC TEST GUIDANCE

- Follow MiningCore’s architecture and subsystem boundaries:
  - Job Manager
  - Share Recorder
  - Payment Processor
  - Persistence Layer
  - API Layer
- When testing mining logic:
  - Use correct difficulty, target, and share math.
  - Avoid probabilistic randomness; mock hashrate or difficulty inputs.
- When testing payout logic:
  - Use deterministic scheduler-based ETA calculations.
  - Do not mix payout ETA with share ETA.
- When testing API endpoints:
  - Test DTO behavior, not internal entities.
  - Mock repositories and services.

-------------------------------------------------------------------------------

TEST QUALITY AND MAINTAINABILITY

- Keep tests readable and maintainable.
- Avoid over-mocking or unnecessary complexity.
- Prefer explicit assertions over broad or ambiguous ones.
- Use helper methods sparingly and only when they improve clarity.
- Ensure each test communicates intent clearly.

-------------------------------------------------------------------------------

STYLE AND CONSISTENCY

- Match MiningCore’s coding style and formatting.
- Use consistent naming, structure, and assertion patterns.
- Keep test files organized by subsystem and feature area.

-------------------------------------------------------------------------------

END OF FILE
