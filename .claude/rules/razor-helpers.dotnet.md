# Razor View Helpers

All form controls and buttons in Razor views MUST use `@Html.CustomHelper()`. Tag helpers for form controls (`asp-for`, `asp-items`, `asp-validation-for`) are PROHIBITED — MUST use the CustomHelper equivalent. Any UI pattern repeated 2+ times MUST be extracted.

For CustomHelper catalog, adding typed helpers, composite extraction, and partial view guidelines, MUST invoke the `razor-views` skill.
