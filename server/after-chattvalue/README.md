# trigger-jenkins-build
A trigger that launches a given Jenkins Job when the attribute value of a Plastic object (branch, changeset or label) is set to a configured value.
The Plastic object spec whose attribute value has changed this way is forwarded to the Jenkins Job in an environment variable, named "PLASTICSCM_MERGEBOT_UPDATE_SPEC".
