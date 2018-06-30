# github-ext
Extensions for working with Github API

# Autocompletion
Currently the only feature is autocompletion of pull requests that take too long to pass all checks. 

## Usage
So if you have a pull request that you want to leave for auto-completion. You can call

`github-ext <pr_number>`

Since autocomplete is the only feature now, it will be triggered for the pull request. The app will prompt you for the title of PR so that you don't make any accidental changes to others' branches. It will then fetch that PR, and if possible, attempt to merge it. If the PR head branch is out-of-date, the app will merge base branch into it and continue looping until PR is merged.

## Configuration
Server name, repo owner etc. can be either passed as command line arguments or specified in *appsettings.json*. You would need a personal OAuth access token with scope *repo* to use the app.
