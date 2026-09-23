# Database Schema (ER Diagram)

```mermaid
erDiagram
    Departments ||--o{ Users : "employs"
    Users ||--o{ Tickets : "creates"
    Users ||--o{ Tickets : "is assigned"
    Users ||--o{ Comments : "writes"
    Users ||--o{ TicketHistory : "changes"
    Categories ||--o{ Tickets : "classifies"
    Tickets ||--o{ Comments : "has"
    Tickets ||--o{ TicketHistory : "logs"

    Departments {
        int Id PK
        string Name
        string Description
    }

    Users {
        int Id PK
        string FirstName
        string LastName
        string Email
        string PasswordHash
        int Role
        int DepartmentId FK
        bool IsActive
    }

    Categories {
        int Id PK
        string Name
        string Description
        bool IsActive
    }

    Tickets {
        int Id PK
        string TicketNumber
        string Title
        string Description
        int CategoryId FK
        int CreatedById FK
        int AssignedToId FK
        int Priority
        int Status
        datetime ResolvedAt
        datetime ClosedAt
    }

    Comments {
        int Id PK
        int TicketId FK
        int UserId FK
        string Body
        bool IsInternal
    }

    TicketHistory {
        int Id PK
        int TicketId FK
        int ChangedById FK
        string FieldName
        string OldValue
        string NewValue
    }
```

## Indexes

| Table          | Index                         | Type    |
|----------------|-------------------------------|---------|
| Users          | Email                         | Unique  |
| Users          | Role                          | Normal  |
| Categories     | Name                          | Unique  |
| Departments    | Name                          | Unique  |
| Tickets        | TicketNumber                  | Unique  |
| Tickets        | Status, Priority, AssignedToId, CreatedById | Normal |
| Tickets        | (Status, CreatedAt) composite | Normal  |
