# GitHub Copilot Refactor Instructions for MiningCore

These instructions guide Copilot when refactoring MiningCore code. The goal is to
improve clarity, maintainability, and correctness without altering behavior unless
explicitly instructed.

-------------------------------------------------------------------------------

GENERAL REFACTORING PRINCIPLES

- Preserve existing behavior unless the user explicitly requests a behavior change.
- Keep refactors minimal, targeted, and easy to review.
- Avoid speculative abstractions, over-engineering, or unnecessary patterns.
- Maintain compatibility with existing pool configurations and MiningCore’s architecture.
- Ask for clarification if the intent of the refactor is ambiguous.

-------------------------------------------------------------------------------

SAFE REFACTORING RULES

- Do not change public APIs, DTOs, or configuration schemas unless explicitly instructed.
- Do not modify database schemas or persistence models unless explicitly instructed.
- Do not introduce new dependencies or frameworks.
- Do not change concurrency behavior, threading models, or async semantics unless requested.
- Do not alter coin-specific logic or job manager behavior without explicit instruction.

-------------------------------------------------------------------------------

CODE QUALITY IMPROVEMENTS

- Improve readability by simplifying expressions, reducing nesting, and removing dead code.
- Extract small helper methods when it improves clarity without changing behavior.
- Replace duplicated logic with shared helpers only when it does not alter execution flow.
- Improve naming for clarity while preserving meaning.
- Add comments explaining intent where needed.

-------------------------------------------------------------------------------

TEST-DRIVEN REFACTORING

- Before refactoring, ensure all existing tests pass.
- After refactoring, all tests must remain green.
- If tests fail:
  - Fix the refactor, not the tests, unless the user explicitly requested a behavior change.
- When refactoring exposes previously untested behavior:
  - Add new tests to cover it.
- Never delete tests unless explicitly instructed.

-------------------------------------------------------------------------------

UNIT TEST EXPECTATIONS

- Use xUnit and Arrange/Act/Assert.
- Keep tests deterministic and isolated.
- Mock external services, repositories, and infrastructure.
- Do not hit real databases or network endpoints.
- When refactoring code with time-based logic, use injected or mocked clocks.

-------------------------------------------------------------------------------

MININGCORE-SPECIFIC REFACTORING GUIDANCE

- Follow the architecture boundaries:
  - Job Manager
  - Share Recorder
  - Payment Processor
  - Persistence Layer
  - API Layer
- Preserve coin-specific behavior in job managers and block template logic.
- Preserve payout scheduler behavior unless explicitly instructed to modify it.
- Keep probabilistic logic (share ETA) separate from deterministic logic (payout ETA).
- Maintain correct difficulty, target, and share validation math.

-------------------------------------------------------------------------------

BACKEND AND API REFACTORING

- Maintain DTO shapes and REST conventions.
- Keep backend logic explicit and readable.
- Avoid introducing hidden side effects or implicit behavior changes.
- Preserve validation rules, error handling, and response formats.

-------------------------------------------------------------------------------

INFRASTRUCTURE AND DEPLOYMENT

- Assume bare-metal Linux unless told otherwise.
- Prefer systemd and nginx examples when refactoring deployment scripts.
- Do not introduce Docker or containerization unless explicitly requested.

-------------------------------------------------------------------------------

REFACTORING PROMPTS FOR COPILOT

Use these internal rules when responding to user requests:

- "Refactor this code for clarity without changing behavior."
- "Improve maintainability while keeping all tests green."
- "Simplify this logic without altering MiningCore’s architecture."
- "Extract helper methods only when it improves readability."
- "Do not introduce new abstractions unless explicitly requested."

-------------------------------------------------------------------------------

STYLE AND CONSISTENCY

- Match MiningCore’s formatting and naming conventions.
- Keep refactored code consistent with surrounding patterns.
- Use explicit, clear logic instead of clever or compact expressions.
- Include comments explaining intent when refactoring complex logic.

-------------------------------------------------------------------------------

END OF FILE
