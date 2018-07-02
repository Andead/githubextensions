[![MIT Licence](https://badges.frapsoft.com/os/mit/mit.svg?v=103)](https://opensource.org/licenses/mit-license.php)
[![Open Source Love](https://badges.frapsoft.com/os/v2/open-source.svg?v=103)](https://github.com/ellerbrock/open-source-badges/)

# github-ext
Extensions for working with Github API

# Downloads
The latest release is available [here](https://github.com/andead/githubext/releases/latest).

# Features
Currently the only feature is auto-merging of pull requests that take too long to pass all checks. 

## Auto merge pull requests
Imagine you have a PR that needs to be merged, but requires some long-running checks to be successful (i.e., a CI build that takes an hour to complete). As those checks are pending, the base branch gets updated by other developers, and the checks have to start over, and over, and over... You have to constantly merge down changes and check if merge is allowed.

Autocomplete feature is addressing this inconvenience. Let's look at the example.

You have a pull request on a GitHub enterprise server `code.company.com` in a repository named `TheProduct` belonging to an organization called `TheCompany`. Pull request number is `123`. Run the app as follows:

`github-ext 123 --server code.company.com --owner TheCompany --repo TheProduct --token <access_token>`

(replace `<access_token>` with a personal access token that has *repo* scope granted).

The app will prompt you for the title of PR so that you don't make any accidental changes to others' branches. It will then get that PR, and if possible, attempt to merge it. If the PR head branch is out-of-date, the app will merge base branch into it and continue looping until PR is merged.

### Unstable state
If some optional checks are not successful, the PR is in what is called an `unstable` state. This may still not prevent you from merging, but by default this is not enabled. You can pass `--merge-unstable` in the arguments to enable merging such pull requests anyway.

## Configuration
You can specify parameters `server`, `owner`, `repo` and `token` in the *appsettings.json* file in the application folder. The values will be used every time they are not explicitly passed in the arguments.
