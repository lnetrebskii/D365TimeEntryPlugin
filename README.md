# D365TimeEntryPlugin
On creation of a Time Entry record the plugin should evaluate if the start and end date contain different values from each other. In the event that the start and end date are different then a time entry record should be created for every date in the date range from start to end date. The plugin should also ensure that there are no duplicate time entry records created per date.  

## Installation
1. Register the Plugin
2. Add a new step
3. Specify "Message" = "Create"
4. Specify "Primary Entity" = "msdyn_timeentry"
5. Pipeline stage = "PreOperation"

## Follow up tasks
- Catch update events
- Create unit tests. Integration tests are created, but they are slow.
- Don't store signing key public
- Implement CI/CD
- Support multiple environments

## Notes
- EST Time zone is used to calculate dates.