# Examples

If you have setup inbound parse for bugs.example.com, then all emails received at that subdomain will be sent through the Azure function.

## workitem config

You can either configure these variables in the release pipeline (yaml) or the ARM template (keyvault technically also works but that is intended for secrets).

``` json
{
    // dev.azure.com/{yourDevOpsName}
    "Organization": "yourDevOpsName",
    // leave blank to enable the routing options. If set, all emails will sent to this project
    "Project": "",
    // only used if Project is blank, see below for details
    "DetermineTargetProjectVia": "All|Subject|Recipient"
}
```

## Recipient based routing

Only works if `Project` is left blank in the config.

``` json
{
    // All also works, Recipient takes precedence over Subject
    "DetermineTargetProjectVia": "Recipient"
}
```

If you have configured the function to parse the project name from the recipient then an email sent to `<project>@bugs.example.com` will be sent to the Azure DevOps project named `<project>`.

## Subject based routing

Only works if `Project` is left blank in the config.

``` json
{
    // All also works, however Recipient takes precedence over Subject
    "DetermineTargetProjectVia": "Subject"
}
```

Subject parsing will try to detect a pattern in the subject and infer the project name:

```
Project - This is a bug report
Project | This is a bug report
(Project) This is a bug report
(Project) - This is a bug report
[Project] - This is a bug report
[Project] This is a bug report
```

If a pattern is detected, the matching name will be used as the project and trimmed from the bug title.

In the case above, all reports would go to project `Project` with title `This is a bug report`.

Fallback: If no title is matched, the first word is taken and assumed to be the project name. In this case it **is not trimmed from the title**.
