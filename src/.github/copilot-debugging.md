# GitHub Copilot Debugging Instructions for MiningCore

These instructions guide Copilot when diagnosing issues, identifying root causes,
reproducing bugs, and proposing fixes in MiningCore. All debugging must follow
MiningCore’s architecture, deterministic behavior, and test-driven workflow.

-------------------------------------------------------------------------------

GENERAL DEBUGGING PRINCIPLES

- Provide clear, technical explanations of root causes.
- Avoid speculation; base conclusions on code, logic, and MiningCore’s architecture.
- When context is incomplete, ask for clarification instead of guessing.
- Propose minimal, targeted fixes that do not alter unrelated behavior.
- Preserve compatibility with existing pool configurations and subsystem boundaries.

-------------------------------------------------------------------------------

BUG REPRODUCTION RULES

- Always begin by generating a failing unit test that reproduces the bug.
- The failing test must:
  - Use xUnit and Arrange/Act/Assert.
  - Mock external dependencies.
  - Be deterministic and isolated.
  - Demonstrate the incorrect behavior clearly.
- Only after the failing test is written should code changes be proposed.

-------------------------------------------------------------------------------

ROOT CAUSE ANALYSIS

- Trace execution through the correct MiningCore subsystem:
  - Job Manager
  - Share Recorder
  - Payment Processor
  - Persistence Layer
  - API Layer
- Identify the exact line, method, or logic path responsible.
- Explain the failure in precise technical terms.
- Distinguish between:
  - deterministic logic errors
  - probabilistic mining math misunderstandings
  - configuration issues
  - persistence or repository issues
  - API validation or DTO mismatches

-------------------------------------------------------------------------------

SAFE FIX RULES

- Fix only the logic directly responsible for the bug.
- Do not introduce new dependencies or frameworks.
- Do not modify public APIs, DTOs, or schemas unless explicitly instructed.
- Do not change concurrency behavior or async semantics unless requested.
- Do not alter coin-specific logic without explicit instruction.
- After proposing a fix:
  - Ensure the new test passes.
  - Ensure all existing tests remain green.
  - Add regression tests if needed.

-------------------------------------------------------------------------------

MININGCORE-SPECIFIC DEBUGGING GUIDANCE

- For mining math issues:
  - Use correct difficulty, target, and share probability formulas.
  - Keep share ETA (probabilistic) separate from payout ETA (deterministic).

- For payout issues:
  - Follow scheduler-based payout logic.
  - Use last payout timestamp + scheduler interval, clamped to next scheduler run.

- For share validation issues:
  - Follow coin-specific job manager behavior.
  - Validate difficulty, target, and nonce logic precisely.

- For persistence issues:
  - Follow repository patterns.
  - Use explicit SQL for performance-critical queries.
  - Avoid schema changes unless explicitly instructed.

-------------------------------------------------------------------------------

LOGGING AND DIAGNOSTICS

- When adding logging:
  - Use MiningCore’s existing logging patterns.
  - Log intent, not noise.
  - Avoid leaking sensitive data.
  - Keep logs structured and actionable.

- When analyzing logs:
  - Identify the subsystem producing the error.
  - Correlate timestamps with scheduler, job manager, or share recorder events.
  - Distinguish between transient and deterministic failures.

-------------------------------------------------------------------------------

API AND DTO DEBUGGING

- Validate request/response DTOs.
- Ensure API endpoints follow MiningCore’s REST conventions.
- Check for:
  - missing fields
  - incorrect types
  - null handling issues
  - validation gaps
- Do not expose internal entities.

-------------------------------------------------------------------------------

INFRASTRUCTURE DEBUGGING (NO DOCKER)

- Assume bare-metal Linux unless told otherwise.
- When diagnosing deployment issues:
  - Check systemd service logs.
  - Validate environment variables.
  - Inspect nginx reverse proxy configuration.
  - Confirm file permissions and paths.
- Do not assume Docker or containerization.

-------------------------------------------------------------------------------

DEBUGGING PROMPTS FOR COPILOT

Use these internal rules when responding to debugging requests:

- "Identify the root cause of this issue and explain it clearly."
- "Write a failing unit test that reproduces this bug."
- "Propose the minimal code change required to make the test pass."
- "Trace the execution path through the correct MiningCore subsystem."
- "Explain why this behavior occurs and how to fix it safely."
- "Analyze this exception and provide a deterministic fix."

-------------------------------------------------------------------------------

STYLE AND CONSISTENCY

- Keep debugging explanations precise and technical.
- Match MiningCore’s coding style when proposing fixes.
- Include comments explaining intent for complex logic.
- Avoid unnecessary verbosity or unrelated suggestions.

-------------------------------------------------------------------------------

END OF FILE
