# trigger-jenkins-build
A Plastic trigger to launch a Jenkins job when the attribute of a Plastic branch / changeset / label is set to given value.

## Brief summary: How it works
This application is suitable to be executed as a Plastic Server Trigger when the attribute value of a Plastic object (branch, changeset or label) is set to a configured value.
The Plastic object spec whose attribute value has changed this way is forwarded to the Jenkins Job in an environment variable, named "PLASTICSCM_MERGEBOT_UPDATE_SPEC".

The trigger is written in C# .NET Framework (4.5 or above).

## 1- Installing the trigger
* Compile this solution.
* Copy the executable file `jenkinstrigger.exe` in the Plastic Server computer, in a path of your convenience. Example: `C:\plastic_triggers\jenkins`
* Copy the configuration file `jenkinstrigger.conf` in the same path as well.
* Edit the `jenkinstrigger.conf` at your convenience: In this file you can configure the Jenkins server url, credentials, and the job to be launched. More info below.
* Enable this trigger to your Plastic Server to be executed when the attribute of a plastic branch / changeset / label is set, by issuing the following plastic command as an example:
** `cm trigger create after-chattvalue JenkinsBuildOnAttrChange C:\plastic_triggers\jenkins\jenkinstrigger.exe`

## 2- Configuring the trigger
The `jenkinstrigger.conf` file is self-documented and pretty easy to tweak. It's a simple, line-by-line `key=value` file where you can configure the following parameters:
* url: the Jenkins server location. Example: 'url=http://my-jenkins-server:8080'
* user: valid Jenkins server user, capable of triggering builds
* password: the password for the Jenkins server configured.
* job: the Jenkins job to be executed. Example: 'debug-pipeline-job'
* attrname: the name of the Plastic attribute for your branches / changesets / labels that will be used to determine whether launch this trigger or not. Example: 'attrname=status'
* attrvalue: value of specified the attribute name configured above that means the Jenkins build has to be launched. Example: 'attrvalue=resolved'
* repositories: optional: comma-separated list of repositories in the Plastic Server allowed to launch the jenkins job. Leave empty to enable all the repositories in the plastic server. Example: 'repositories=default,tools'
* skipprefixes: optional: comma-separated list: the trigger will ignore the branches/labels/changesets matching with the prefixes configured in this list
