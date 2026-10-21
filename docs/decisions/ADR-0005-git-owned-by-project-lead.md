# ADR-0005 — Git and GitHub are owned by the project lead

Status: Accepted

## Context

Work arrives as one task per prompt, implemented by an AI assistant. Automated commits, branches
or rewrites would make history hard to trust and hard to audit, and a mistaken Git operation can
destroy work that is not yet pushed.

## Decision

The implementer never runs any Git command and never creates Git hooks: no init, status, add,
commit, push, branch, checkout or stash. The project lead performs every version control and
GitHub operation. The implementer instead ends each task with a report and a suggested commit
message in Conventional Commits format.

## Consequences

- Every change to history is a deliberate human act.
- The implementer must leave the working tree in a reviewable state and describe exactly what
  changed, since the lead reads the diff before committing.
- The repository still ships `.gitignore` and `.gitattributes`, because those are project
  configuration files rather than Git operations.
