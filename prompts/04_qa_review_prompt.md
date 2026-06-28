# Prompt — QA / Build Review

Review the current Color Gate Rush implementation without editing files.

Check:

- C# compile risks.
- Missing namespaces or Unity package assumptions.
- Trigger/collider/Rigidbody correctness.
- Null references from bootstrap or runtime generation.
- Whether game can run from an empty generated scene.
- Whether any external art/audio asset was introduced.
- Whether mobile and keyboard controls both work.
- Whether `Tools/Color Gate Rush/Bootstrap Project` and `Validate Project` are plausible.

Return findings as:

1. Critical blockers.
2. Important issues.
3. Polish opportunities.
4. Suggested verification steps.

Do not comment on style unless it hides a real bug.
