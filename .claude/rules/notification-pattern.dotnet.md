# Notification Pattern

MUST call `NotifyAsync` after `SaveChangesAsync()`. MUST wrap in try-catch — notification failure MUST NOT fail the business operation. MUST look up recipient user info from `DbContext.Users`.

For NotifyAsync dispatch, UserNotificationRequest fields, channel flags, and in-app notification setup, MUST invoke the `notifications` skill.
