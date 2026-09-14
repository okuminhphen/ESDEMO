# Repository workflow

- Work on `develop` by default unless the user requests a different branch.
- After completing a requested code change or fix, run the relevant checks and automatically commit the task changes.
- Write commit messages in English.
- Do not push automatically. The user pushes `develop` themselves unless they explicitly request a push for the current task.
- Leave merges from `develop` into `main` to the user on GitHub unless explicitly requested.
- Do not include unrelated changes, secrets, local environment files or generated build output in commits.
