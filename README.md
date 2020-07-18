# API Review Web Site

This site allows us to browse the backlog and share notes.

## Missing

* The server is in UTC; this means that our UI is parsed and rendered in UTC
  too, which we don't want.
* We should consider using GraphQL to speed up retreival of issues

## Dashboard

Consider a dashboard page like this:

```text
Repos
[x] dotnet/designs
[x] dotnet/runtime
[x] dotnet/winforms

Milestones
[x] 5.0
[ ] 6.0
[ ] Future

       | Suggestion | Ready For Review | Needs Work | Approved
-------+------------+------------------+------------+---------
Area 1 |            |                  |            |
Area 2 |            |                  |            |
```

## Storing Meetings & Notes

Consider adding a way to store past reviews and schedule meetings.

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

### Requesting

* Mark api-ready-for-review
* Mark as blocking

### Notifications

* Issue labbeled as api-ready-for-review
* Review completed

### Reviews

`/reviews`
  - filter by date
  - list of reviews, paged

`/reviews/new`
  - creates new review

`/reviews/{id}`
  - shows a given review
  - complete
    - find video + review decisions
    - saving that will update comments
  - edit title + date
  - edit description
  - add/remove issues
  - delete

`/reviews/today` or even `/`
  - same as `/review/{id}`, but points to the first review on today's day that wasn't completed yet.

-------------------------------------

Title
Relative Date
Status

MarkdownDescription

#if !complete
* links to isses

#if complete
* links to decision comment
* decison
* video

Embedded Video

-------------------------------------
