# Data Model Documentation

## Overview

This document describes the database schema and entity relationships for the FamilyFitness Workout Of the Day (WOD) scheduling application. The data model supports group (family) management, user participation, workout session scheduling, and interval-based scoring.

## Entity Relationship Diagram

```
User ←──→ GroupMembership ←──→ Group
 │                               │
 │                               │
 └──→ WorkoutSessionParticipant ←┘
      │                         │
      │                    WorkoutSession
      │                         │
      └─→ WorkoutIntervalScore  ├──→ WorkoutSessionWorkoutType → WorkoutType
```

## Entities

### User

Represents a user who can participate in workout sessions.

**Fields:**
- `Id` (Guid, PK): Unique identifier
- `Username` (string, max 100): Unique username
- `Email` (string, max 255): Unique email address
- `CreatedAt` (DateTime): Account creation timestamp

**Constraints:**
- Unique username
- Unique email

**Relationships:**
- Many GroupMemberships (many-to-many with Groups via GroupMembership)
- Many WorkoutSessionParticipants (participation in workout sessions)

### Group

Represents a named set of users (e.g., family, friends).

**Fields:**
- `Id` (Guid, PK): Unique identifier
- `Name` (string, max 200): Group name
- `Description` (string, max 1000, nullable): Optional description
- `CreatedAt` (DateTime): Creation timestamp

**Relationships:**
- Many GroupMemberships (many-to-many with Users via GroupMembership)
- Many WorkoutSessions (workout sessions for this group)

### GroupMembership

Join table for User-Group many-to-many relationship with additional metadata.

**Fields:**
- `Id` (Guid, PK): Unique identifier
- `GroupId` (Guid, FK): Reference to Group
- `UserId` (Guid, FK): Reference to User
- `JoinedAt` (DateTime): When user joined the group
- `Role` (string, max 50, nullable): Optional role (e.g., "admin", "member")

**Constraints:**
- Unique (GroupId, UserId): A user can only be a member of a group once

**Relationships:**
- Belongs to one Group
- Belongs to one User

### WorkoutType

Configurable exercise types (e.g., Pushups, Plank, Situps, Jumping Jacks).

**Fields:**
- `Id` (string, max 50, PK): Unique identifier
- `Name` (string, max 200): Exercise name
- `Description` (string, max 1000, nullable): Optional description

**Constraints:**
- Unique name

**Notes:**
- WorkoutType uses string IDs for compatibility with existing implementation
- Future enhancement: Add metric field (reps/time/weight)

### WorkoutSession

A concrete Workout Of the Day (WOD) event.

**Fields:**
- `Id` (Guid, PK): Unique identifier
- `GroupId` (Guid, FK): Reference to Group
- `CreatorId` (Guid, FK): User who created the session
- `SessionDate` (DateTime): Date of the workout session
- `StartedAt` (DateTime, nullable): When the session started
- `EndedAt` (DateTime, nullable): When the session ended
- `Status` (WorkoutSessionStatus enum): Current status
- `CreatedAt` (DateTime): Creation timestamp

**Status Values:**
- `Pending` (0): Not yet started
- `Active` (1): Currently in progress
- `Completed` (2): Finished successfully
- `Cancelled` (3): Cancelled before completion

**Constraints:**
- Index on (GroupId, SessionDate) for efficient querying

**Relationships:**
- Belongs to one Group (CASCADE delete)
- Created by one User (Creator, RESTRICT delete)
- Has exactly 4 WorkoutSessionWorkoutTypes (stations)
- Has many WorkoutSessionParticipants

**Business Rules:**
- Has exactly 4 workout types (stations)
- Has 3 rounds (fixed, enforced in application logic)
- When cancelled, remaining unscored intervals are recorded as 0

### WorkoutSessionWorkoutType

Links a WorkoutSession to WorkoutTypes representing the 4 stations.

**Fields:**
- `Id` (Guid, PK): Unique identifier
- `WorkoutSessionId` (Guid, FK): Reference to WorkoutSession
- `WorkoutTypeId` (string, FK): Reference to WorkoutType
- `StationIndex` (int): Station number (1-4)

**Constraints:**
- Unique (WorkoutSessionId, StationIndex): Each session has one workout type per station
- Check: StationIndex BETWEEN 1 AND 4

**Relationships:**
- Belongs to one WorkoutSession (CASCADE delete)
- References one WorkoutType

**Business Rules:**
- Exactly 4 entries per WorkoutSession (enforced at application level)

### WorkoutSessionParticipant

Connects Users to a WorkoutSession with join order.

**Fields:**
- `Id` (Guid, PK): Unique identifier
- `WorkoutSessionId` (Guid, FK): Reference to WorkoutSession
- `UserId` (Guid, FK): Reference to User
- `ParticipantIndex` (int): Order in which they joined (determines rotation)
- `JoinedAt` (DateTime): When they joined the session

**Constraints:**
- Unique (WorkoutSessionId, UserId): One session per user per group per day
- Unique (WorkoutSessionId, ParticipantIndex): Unique participant order within session

**Relationships:**
- Belongs to one WorkoutSession (CASCADE delete)
- Belongs to one User (CASCADE delete)
- Has many WorkoutIntervalScores

**Business Rules:**
- ParticipantIndex determines station rotation order
- At most one session per user per group per day

### WorkoutIntervalScore

Stores scores for each participant's performance at each station in each round.

**Fields:**
- `Id` (Guid, PK): Unique identifier
- `ParticipantId` (Guid, FK): Reference to WorkoutSessionParticipant
- `RoundNumber` (int): Round number (1-3)
- `StationIndex` (int): Station number (1-4)
- `WorkoutTypeId` (string, FK): Direct reference to WorkoutType for easier progression querying
- `Score` (int): Performance score (0 or higher)
- `Weight` (decimal(10,2), nullable): Optional weight used
- `RecordedAt` (DateTime): When score was recorded

**Constraints:**
- Unique (ParticipantId, RoundNumber, StationIndex): One score per participant per round per station
- Index (ParticipantId, WorkoutTypeId, RecordedAt): Optimized for progression queries by workout type
- Check: RoundNumber BETWEEN 1 AND 3
- Check: StationIndex BETWEEN 1 AND 4
- Check: Score >= 0 (zero is allowed for no reps or cancelled sessions)

**Relationships:**
- Belongs to one WorkoutSessionParticipant (CASCADE delete)

**Business Rules:**
- No score editing after submission (enforced at application level)
- Only inserts allowed, no updates (optionally enforce immutability at DB level)
- Score of 0 is valid (e.g., for cancelled sessions or no performance)

## Key Business Rules

### Session Structure
- Each WorkoutSession has exactly **4 stations** (WorkoutTypes)
- Each session has exactly **3 rounds**
- Participants rotate through the 4 stations round by round

### Participation
- Maximum one session per user per group per day
- ParticipantIndex determines rotation order through stations

### Scoring
- Each participant records a score for each round (1-3) at each station (1-4)
- Total of 12 scores per participant per session (3 rounds × 4 stations)
- Scores are immutable once submitted
- Score of 0 is allowed (e.g., for no performance or cancelled sessions)

### Cancellation
- When a session is cancelled, remaining unscored intervals are recorded as 0
- Status changes to Cancelled (3)

### Station Rotation
Participants rotate through stations based on their ParticipantIndex. The rotation is computed as:
```
ActualStationIndex = ((StationIndex - 1 + ParticipantIndex - 1) % 4) + 1
```

For example, with 4 participants and 4 stations:
- Round 1: P1→S1, P2→S2, P3→S3, P4→S4
- Round 2: P1→S2, P2→S3, P3→S4, P4→S1
- Round 3: P1→S3, P2→S4, P3→S1, P4→S2

### Progression Tracking

The `WorkoutIntervalScore` entity includes both `StationIndex` (for rotation logic) and `WorkoutTypeId` (for progression queries). This design enables:

**Easy progression queries** - Find all scores for a specific exercise type:
```sql
SELECT Score, Weight, RecordedAt 
FROM workout_interval_scores 
WHERE ParticipantId = @userId 
  AND WorkoutTypeId = 'pushups'
ORDER BY RecordedAt;
```

**Performance over time** - Track improvement without complex joins:
```sql
SELECT DATE(RecordedAt) as Date, AVG(Score) as AvgScore
FROM workout_interval_scores 
WHERE ParticipantId = @userId 
  AND WorkoutTypeId = 'pushups'
GROUP BY DATE(RecordedAt)
ORDER BY Date;
```

The index on `(ParticipantId, WorkoutTypeId, RecordedAt)` optimizes these queries for fast performance.

## Database Schema

### PostgreSQL Implementation

The schema is implemented in PostgreSQL with the following features:
- **UUID primary keys** for all entities (except WorkoutType which uses string for backward compatibility)
- **Snake_case table names** (e.g., `workout_sessions`, `group_memberships`)
- **Foreign key constraints** with appropriate cascade/restrict rules
- **Unique indexes** to enforce business rules
- **Check constraints** for valid ranges (rounds 1-3, stations 1-4, scores >= 0)
- **Timestamps** stored as `timestamp with time zone`

### Migration

The initial migration is available in:
```
src/FamilyFitness.Infrastructure/Migrations/[timestamp]_InitialWorkoutDataModel.cs
```

To apply the migration:
```bash
cd src/FamilyFitness.Api
dotnet ef database update --project ../FamilyFitness.Infrastructure
```

## Future Enhancements

1. **WorkoutType Metrics**: Add optional metric field to WorkoutType (reps/time/weight)
2. **Score Immutability**: Enforce score immutability at database level with triggers
3. **Session Validation**: Add database trigger to enforce exactly 4 WorkoutSessionWorkoutTypes per session
4. **Audit Trail**: Add created_by/updated_by fields for audit purposes
5. **Soft Deletes**: Implement soft delete pattern for historical data preservation

## Entity Framework Core Configuration

All entity configurations are defined in `FamilyFitnessDbContext.cs` using the Fluent API:
- Table names and column constraints
- Relationships and foreign keys
- Unique indexes
- Check constraints
- Cascade behaviors

See `src/FamilyFitness.Infrastructure/FamilyFitnessDbContext.cs` for implementation details.
