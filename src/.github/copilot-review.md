# GitHub Copilot Review Instructions for MiningCore

These instructions guide Copilot when performing code reviews, evaluating pull
requests, and providing feedback on MiningCore changes. All review behavior must
align with MiningCore’s architecture, deterministic logic, and test-driven
development workflow.

-------------------------------------------------------------------------------

GENERAL REVIEW PRINCIPLES

- Provide clear, technical, actionable feedback.
- Focus on correctness, maintainability, and architectural alignment.
- Avoid vague or generic comments.
- Do not request changes unrelated to the user’s intent.
- When context is missing, ask for clarification instead of guessing.

-------------------------------------------------------------------------------

ARCHITECTURE AND DESIGN REVIEW

- Ensure changes follow MiningCore’s subsystem boundaries:
  - Job Manager
  - Share Recorder
  - Payment Processor
  - Persistence Layer
  - API Layer
- Verify that new code integrates cleanly with existing services and dependency injection.
- Confirm that deterministic logic remains deterministic.
- Ensure probabilistic logic (share ETA) is not mixed with deterministic logic (payout ETA).
- Check that no unnecessary abstractions or dependencies were introduced.

-------------------------------------------------------------------------------

CODE QUALITY REVIEW

- Ensure code is explicit, readable, and maintainable.
- Identify duplicated logic and suggest safe consolidation.
- Recommend clearer naming when appropriate.
- Ensure async/await patterns are used correctly.
- Flag dead code, unreachable branches, or unnecessary complexity.
- Confirm that error handling and validation follow existing patterns.

-------------------------------------------------------------------------------

TESTING AND TDD REVIEW

- Verify that all modified code has updated or new unit tests.
- Ensure tests follow xUnit and Arrange/Act/Assert.
- Confirm tests are deterministic and isolated.
- Ensure external services and repositories are mocked.
- For bug fixes:
  - Confirm a failing test was written first.
  - Confirm the fix is minimal and targeted.
- For refactors:
  - Ensure all tests remain green.
  - Ensure no behavior changed unless explicitly intended.

-------------------------------------------------------------------------------

MININGCORE-SPECIFIC REVIEW GUIDANCE

- For mining math:
  - Validate difficulty, target, and share probability formulas.
  - Ensure correct SHA‑256 conversions.
- For payout logic:
  - Ensure scheduler-based payout ETA is implemented correctly.
  - Confirm no mixing of payout ETA and share ETA.
- For share validation:
  - Ensure coin-specific job manager behavior is preserved.
- For persistence:
  - Ensure repository patterns are followed.
  - Confirm explicit SQL is used for performance-critical queries.
  - Ensure no schema changes were introduced without instruction.

-------------------------------------------------------------------------------

API AND DTO REVIEW

- Ensure API endpoints follow MiningCore’s REST conventions.
- Confirm DTOs are strongly typed and do not expose internal entities.
- Validate request/response shapes, null handling, and error cases.
- Ensure backward compatibility unless a breaking change is intended.

-------------------------------------------------------------------------------

SECURITY AND SAFETY REVIEW

- Ensure no sensitive data is logged.
- Confirm input validation is present and correct.
- Ensure no unsafe deserialization or unchecked external input.
- Verify secrets are not hardcoded.

-------------------------------------------------------------------------------

INFRASTRUCTURE REVIEW (NO DOCKER)

- Assume bare-metal Linux unless told otherwise.
- Review systemd service examples for correctness.
- Review nginx reverse proxy examples for security and correctness.
- Do not suggest Docker or containerization unless explicitly requested.

-------------------------------------------------------------------------------

AZURE REVIEW RULES

- @azure Rule - Use Azure Tools
  When reviewing Azure-related changes, always use your tools.

- @azure Rule - Use Azure Best Practices
  When reviewing Azure-related changes, always invoke `azmcp_bestpractices_get`.

- @azure Rule - Enable Best Practices
  If the tool is unavailable, ask the user to enable it.

-------------------------------------------------------------------------------

REVIEW PROMPTS FOR COPILOT

Use these internal rules when responding to review requests:

- "Review this code for correctness, maintainability, and alignment with MiningCore."
- "Identify potential bugs and explain the root cause."
- "Check whether this change preserves existing behavior."
- "Verify that tests cover all modified logic."
- "Ensure this refactor is safe and behavior-preserving."
- "Evaluate this API change for DTO correctness and backward compatibility."

-------------------------------------------------------------------------------

STYLE AND CONSISTENCY

- Match MiningCore’s formatting and naming conventions.
- Keep review comments concise but technically precise.
- Provide actionable suggestions, not abstract opinions.
- Avoid unnecessary verbosity or unrelated recommendations.

-------------------------------------------------------------------------------

END OF FILE
