# API Review Web Site

This site allows us to browse the backlog and share notes.

## Missing

* Sharing notes
* Deploy to Azure
* We should consider using GraphQL to speed up retreival of issues

## GraphQL

This query gets us the data from GitHub:

```text
{
  viewer {
    login
  }
  repository(name: "runtime", owner: "dotnet") {
    issues(filterBy: {since: "2020-06-25T16:54:00Z"}, first: 100) {
      nodes {
        number
        timelineItems(first: 250, since: "2020-06-25T16:54:00Z") {
          nodes {
            ... on IssueComment {
              id
              bodyText
              url
              createdAt
            }
            ... on LabeledEvent {
              id
              label {
                name
              }
              createdAt
            }
          }
        }
        labels(first: 100) {
          nodes {
            color
            name
          }
        }
        createdAt
        author {
          login
        }
        state
        title
      }
    }
  }
}
```
