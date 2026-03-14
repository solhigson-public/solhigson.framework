# Notification Pattern

Notifications MUST be dispatched AFTER the business operation succeeds (after persistence). Notification failure MUST NOT fail the business operation — MUST wrap in error handling.

For dispatch patterns and channel configuration, MUST invoke the `notifications` skill.
