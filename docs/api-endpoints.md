# API Endpoints Documentation

This document describes all available REST API endpoints for the FamilyFitness application.

## Base URL

- **Development**: `https://localhost:7001`
- **Production**: TBD

## Authentication

Currently, all endpoints are open (no authentication required). Authentication will be added in a future iteration.

---

## Users

### Get All Users
```
GET /api/users
```

**Response**: Array of User objects

### Get User by ID
```
GET /api/users/{id}
```

**Parameters**:
- `id` (GUID): User ID

**Response**: User object or 404 if not found

### Create User
```
POST /api/users
```

**Body**:
```json
{
  "username": "string (required, max 100 chars)",
  "email": "string (required, max 255 chars)"
}
```

**Response**: Created User object (201) or error (400/409)

### Update User
```
PUT /api/users/{id}
```

**Parameters**:
- `id` (GUID): User ID

**Body**:
```json
{
  "id": "guid (must match URL)",
  "username": "string (required, max 100 chars)",
  "email": "string (required, max 255 chars)"
}
```

**Response**: Updated User object (200) or error (400/404/409)

### Delete User
```
DELETE /api/users/{id}
```

**Parameters**:
- `id` (GUID): User ID

**Response**: 204 No Content or 404 if not found

---

## Groups

### Get All Groups
```
GET /api/groups
```

**Response**: Array of Group objects

### Get Group by ID
```
GET /api/groups/{id}
```

**Parameters**:
- `id` (GUID): Group ID

**Response**: Group object or 404 if not found

### Create Group
```
POST /api/groups
```

**Body**:
```json
{
  "name": "string (required, max 200 chars)",
  "description": "string (optional, max 1000 chars)"
}
```

**Response**: Created Group object (201) or error (400)

### Update Group
```
PUT /api/groups/{id}
```

**Parameters**:
- `id` (GUID): Group ID

**Body**:
```json
{
  "id": "guid (must match URL)",
  "name": "string (required, max 200 chars)",
  "description": "string (optional, max 1000 chars)"
}
```

**Response**: Updated Group object (200) or error (400/404)

### Delete Group
```
DELETE /api/groups/{id}
```

**Parameters**:
- `id` (GUID): Group ID

**Response**: 204 No Content or 404 if not found

---

## Group Memberships

### Get All Group Memberships
```
GET /api/group-memberships
```

**Response**: Array of GroupMembership objects

### Get Group Membership by ID
```
GET /api/group-memberships/{id}
```

**Parameters**:
- `id` (GUID): GroupMembership ID

**Response**: GroupMembership object or 404 if not found

### Get Memberships by Group
```
GET /api/groups/{groupId}/memberships
```

**Parameters**:
- `groupId` (GUID): Group ID

**Response**: Array of GroupMembership objects for the specified group

### Get Memberships by User
```
GET /api/users/{userId}/memberships
```

**Parameters**:
- `userId` (GUID): User ID

**Response**: Array of GroupMembership objects for the specified user

### Create Group Membership
```
POST /api/group-memberships
```

**Body**:
```json
{
  "groupId": "guid (required)",
  "userId": "guid (required)",
  "role": "string (optional, max 50 chars)"
}
```

**Response**: Created GroupMembership object (201) or error (400/404/409)

### Update Group Membership
```
PUT /api/group-memberships/{id}
```

**Parameters**:
- `id` (GUID): GroupMembership ID

**Body**:
```json
{
  "id": "guid (must match URL)",
  "role": "string (optional, max 50 chars)"
}
```

**Response**: Updated GroupMembership object (200) or error (400/404)

### Delete Group Membership
```
DELETE /api/group-memberships/{id}
```

**Parameters**:
- `id` (GUID): GroupMembership ID

**Response**: 204 No Content or 404 if not found

---

## Workout Types

### Get All Workout Types
```
GET /api/workout-types
```

**Response**: Array of WorkoutType objects

### Get Workout Type by ID
```
GET /api/workout-types/{id}
```

**Parameters**:
- `id` (string): WorkoutType ID

**Response**: WorkoutType object or 404 if not found

### Create Workout Type
```
POST /api/workout-types
```

**Body**:
```json
{
  "name": "string (required, max 200 chars)",
  "description": "string (optional, max 1000 chars)"
}
```

**Response**: Created WorkoutType object (201) or error (400/409)

### Update Workout Type
```
PUT /api/workout-types/{id}
```

**Parameters**:
- `id` (string): WorkoutType ID

**Body**:
```json
{
  "id": "string (must match URL)",
  "name": "string (required, max 200 chars)",
  "description": "string (optional, max 1000 chars)"
}
```

**Response**: Updated WorkoutType object (200) or error (400/404/409)

### Delete Workout Type
```
DELETE /api/workout-types/{id}
```

**Parameters**:
- `id` (string): WorkoutType ID

**Response**: 204 No Content or 404 if not found

---

## Workout Sessions

### Get All Workout Sessions
```
GET /api/workout-sessions
```

**Response**: Array of WorkoutSession objects (ordered by session date descending)

### Get Workout Session by ID
```
GET /api/workout-sessions/{id}
```

**Parameters**:
- `id` (GUID): WorkoutSession ID

**Response**: WorkoutSession object or 404 if not found

### Get Workout Sessions by Group
```
GET /api/groups/{groupId}/workout-sessions
```

**Parameters**:
- `groupId` (GUID): Group ID

**Response**: Array of WorkoutSession objects for the specified group

### Get Workout Sessions by Creator
```
GET /api/users/{creatorId}/workout-sessions
```

**Parameters**:
- `creatorId` (GUID): Creator User ID

**Response**: Array of WorkoutSession objects created by the specified user

### Create Workout Session
```
POST /api/workout-sessions
```

**Body**:
```json
{
  "groupId": "guid (required)",
  "creatorId": "guid (required)",
  "sessionDate": "datetime (required)"
}
```

**Response**: Created WorkoutSession object (201) or error (400/404)

### Update Workout Session
```
PUT /api/workout-sessions/{id}
```

**Parameters**:
- `id` (GUID): WorkoutSession ID

**Body**:
```json
{
  "id": "guid (must match URL)",
  "sessionDate": "datetime (required)",
  "startedAt": "datetime (optional)",
  "endedAt": "datetime (optional)",
  "status": "number (0=Pending, 1=Active, 2=Completed, 3=Cancelled)"
}
```

**Response**: Updated WorkoutSession object (200) or error (400/404)

### Delete Workout Session
```
DELETE /api/workout-sessions/{id}
```

**Parameters**:
- `id` (GUID): WorkoutSession ID

**Response**: 204 No Content or 404 if not found

---

## Workout Session Workout Types (Stations)

### Get All Workout Session Workout Types
```
GET /api/workout-session-workout-types
```

**Response**: Array of WorkoutSessionWorkoutType objects

### Get Workout Session Workout Type by ID
```
GET /api/workout-session-workout-types/{id}
```

**Parameters**:
- `id` (GUID): WorkoutSessionWorkoutType ID

**Response**: WorkoutSessionWorkoutType object or 404 if not found

### Get Workout Types for a Session
```
GET /api/workout-sessions/{workoutSessionId}/workout-types
```

**Parameters**:
- `workoutSessionId` (GUID): WorkoutSession ID

**Response**: Array of WorkoutSessionWorkoutType objects for the specified session (ordered by station index)

### Create Workout Session Workout Type
```
POST /api/workout-session-workout-types
```

**Body**:
```json
{
  "workoutSessionId": "guid (required)",
  "workoutTypeId": "string (required)",
  "stationIndex": "number (1-4, required)"
}
```

**Response**: Created WorkoutSessionWorkoutType object (201) or error (400/404)

### Update Workout Session Workout Type
```
PUT /api/workout-session-workout-types/{id}
```

**Parameters**:
- `id` (GUID): WorkoutSessionWorkoutType ID

**Body**:
```json
{
  "id": "guid (must match URL)",
  "workoutTypeId": "string (required)",
  "stationIndex": "number (1-4, required)"
}
```

**Response**: Updated WorkoutSessionWorkoutType object (200) or error (400/404)

### Delete Workout Session Workout Type
```
DELETE /api/workout-session-workout-types/{id}
```

**Parameters**:
- `id` (GUID): WorkoutSessionWorkoutType ID

**Response**: 204 No Content or 404 if not found

---

## Workout Session Participants

### Get All Workout Session Participants
```
GET /api/workout-session-participants
```

**Response**: Array of WorkoutSessionParticipant objects

### Get Workout Session Participant by ID
```
GET /api/workout-session-participants/{id}
```

**Parameters**:
- `id` (GUID): WorkoutSessionParticipant ID

**Response**: WorkoutSessionParticipant object or 404 if not found

### Get Participants for a Session
```
GET /api/workout-sessions/{workoutSessionId}/participants
```

**Parameters**:
- `workoutSessionId` (GUID): WorkoutSession ID

**Response**: Array of WorkoutSessionParticipant objects for the specified session (ordered by participant index)

### Get Participations by User
```
GET /api/users/{userId}/participations
```

**Parameters**:
- `userId` (GUID): User ID

**Response**: Array of WorkoutSessionParticipant objects for the specified user

### Create Workout Session Participant
```
POST /api/workout-session-participants
```

**Body**:
```json
{
  "workoutSessionId": "guid (required)",
  "userId": "guid (required)",
  "participantIndex": "number (>=1, required)"
}
```

**Response**: Created WorkoutSessionParticipant object (201) or error (400/404)

### Update Workout Session Participant
```
PUT /api/workout-session-participants/{id}
```

**Parameters**:
- `id` (GUID): WorkoutSessionParticipant ID

**Body**:
```json
{
  "id": "guid (must match URL)",
  "participantIndex": "number (>=1, required)"
}
```

**Response**: Updated WorkoutSessionParticipant object (200) or error (400/404)

### Delete Workout Session Participant
```
DELETE /api/workout-session-participants/{id}
```

**Parameters**:
- `id` (GUID): WorkoutSessionParticipant ID

**Response**: 204 No Content or 404 if not found

---

## Workout Interval Scores

### Get All Workout Interval Scores
```
GET /api/workout-interval-scores
```

**Response**: Array of WorkoutIntervalScore objects (ordered by recorded date descending)

### Get Workout Interval Score by ID
```
GET /api/workout-interval-scores/{id}
```

**Parameters**:
- `id` (GUID): WorkoutIntervalScore ID

**Response**: WorkoutIntervalScore object or 404 if not found

### Get Scores by Participant
```
GET /api/workout-session-participants/{participantId}/scores
```

**Parameters**:
- `participantId` (GUID): WorkoutSessionParticipant ID

**Response**: Array of WorkoutIntervalScore objects for the specified participant (ordered by round and station)

### Get Scores by Workout Type
```
GET /api/workout-types/{workoutTypeId}/scores
```

**Parameters**:
- `workoutTypeId` (string): WorkoutType ID

**Response**: Array of WorkoutIntervalScore objects for the specified workout type (ordered by recorded date descending)

### Create Workout Interval Score
```
POST /api/workout-interval-scores
```

**Body**:
```json
{
  "participantId": "guid (required)",
  "roundNumber": "number (1-3, required)",
  "stationIndex": "number (1-4, required)",
  "workoutTypeId": "string (required)",
  "score": "number (>=0, required)",
  "weight": "decimal (optional)"
}
```

**Response**: Created WorkoutIntervalScore object (201) or error (400/404)

### Update Workout Interval Score
```
PUT /api/workout-interval-scores/{id}
```

**Parameters**:
- `id` (GUID): WorkoutIntervalScore ID

**Body**:
```json
{
  "id": "guid (must match URL)",
  "score": "number (>=0, required)",
  "weight": "decimal (optional)"
}
```

**Response**: Updated WorkoutIntervalScore object (200) or error (400/404)

**Note**: In production, scores might be immutable. This update endpoint is provided for testing purposes.

### Delete Workout Interval Score
```
DELETE /api/workout-interval-scores/{id}
```

**Parameters**:
- `id` (GUID): WorkoutIntervalScore ID

**Response**: 204 No Content or 404 if not found

---

## Error Responses

All endpoints return consistent error responses:

### 400 Bad Request
```json
{
  "error": "Error message describing what went wrong"
}
```

### 404 Not Found
```json
{
  "error": "Resource with ID 'xxx' not found."
}
```

### 409 Conflict
```json
{
  "error": "Duplicate constraint violation message"
}
```

---

## Common Patterns

### Pagination
Currently not implemented. All `GET` endpoints return full result sets. Pagination will be added in a future iteration.

### Filtering
Limited filtering is available through nested endpoints (e.g., getting memberships by group or user). More advanced filtering will be added in a future iteration.

### CORS
CORS is enabled for all origins in development mode. This will be restricted in production.

---

## Next Steps

- Add authentication/authorization
- Add pagination support
- Add advanced filtering and search
- Add batch operations
- Add data validation middleware
- Add rate limiting
