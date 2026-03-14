# Architecture

Clean Architecture — inner layers MUST NEVER reference outer: Domain → Application → Infrastructure → Web.Ui. External integrations MUST live behind Application-layer interfaces in Infrastructure.

For layer dependencies, what-lives-where, naming conventions, adapter boundary, and DI registration, MUST invoke the `architecture` skill.
