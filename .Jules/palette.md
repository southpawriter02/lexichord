## 2024-05-22 - Icon-only buttons need explicit names
**Learning:** Icon-only buttons rely on visual cues that screen readers miss. `ToolTip.Tip` provides some help but `AutomationProperties.Name` is the explicit accessible name.
**Action:** Always add `AutomationProperties.Name` to icon-only buttons, even if they have tooltips.
