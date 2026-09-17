# Test Plan — Researcher Publication Reward + UserReward APIs

This document describes how to manually exercise the new
`Research Publication Reward` logic from the API surface, complementing
the automated xUnit tests in `ARSPlatform.Tests`.

All endpoints require the caller to be authenticated. The reward
extension path is triggered by an Admin-only Paper update that sets
`Status = "Approved"` against a Paper whose Creator is a Researcher.

---

## 1. Setup — create the reward

Admin only. The reward's `Name` must normalize to
`researchpublicationreward` (case-/underscore-/space-insensitive).

### `POST /api/UserRewards`

```json
{
  "name": "Research Publication Reward",
  "description": "Thưởng khi publish bài báo",
  "rewardMonths": 3,
  "updateAt": "2026-09-17T00:00:00Z",
  "status": "Active"
}
```

**Expected**: `201 Created` with the persisted reward body.

### `GET /api/UserRewards?pageNumber=1&pageSize=10`

Should list the reward just created.

### `PATCH /api/UserRewards/{id}/toggle`

```json
{ "isActive": false }
```

Then back to `true`. Status must flip `Active` ⇄ `InActive`.

### `PUT /api/UserRewards/{id}`

```json
{
  "name": "research_publication_reward",
  "description": "Updated description",
  "rewardMonths": 6,
  "updateAt": "2026-09-17T00:00:00Z",
  "status": "Active"
}
```

The underscore variant must still match the canonical name at runtime.

### `DELETE /api/UserRewards/{id}`

Should remove the reward.

---

## 2. End-to-end — Researcher is extended when paper is approved

The TEST endpoint `PUT /api/Paper/test-update-no-verify/{id}` bypasses
the OpenAlex/ORCID guards so the extension can be exercised quickly.

### Steps

1. Create a Researcher user (or reuse an existing one), note their
   `userId`.
2. Confirm `UserSubscriptions` has no row for
   `(userId, "Researcher")`, or note its current `ExpiresAt`.
3. Create a Paper as that Researcher:
   ```
   POST /api/Paper
   { "title": "Quantum AI", "abstract": "...", "paperType": "Journal" }
   ```
4. As Admin, ensure a reward named `Research Publication Reward` with
   `Status = "Active"` exists.
5. As Admin, approve the paper via the test endpoint:
   ```
   PUT /api/Paper/test-update-no-verify/{paperId}
   { "title": "Quantum AI", "abstract": "...", "paperType": "Journal",
     "status": "Approved" }
   ```
6. Verify:
   - `GET /api/UserSubscriptions` (or DB) — a row now exists (or was
     updated) for `(userId, "Researcher")` with `ExpiresAt` extended by
     `rewardMonths` months on top of the previous ExpiresAt (or now if
     expired).
   - `GET /api/Notifications?userId={userId}` — exactly one notification
     with message:
     ```
     Bạn đã được gia hạn thời gian sử dụng vai trò này khi đăng "Quantum AI" đã được đăng lên hệ thống
     ```

### Negative cases

| Scenario | Expectation |
|---|---|
| Reward name != "Research Publication Reward" (e.g. "Lecture Reward") | No extension, no notification |
| Reward `Status = "InActive"` | No extension, no notification |
| Paper `Status` ≠ "Approved" (e.g. "Rejected") | No extension, no notification |
| Reward `RewardMonths = 0` | No extension, no notification |
| `Paper.CreatorId` is null (orphan) | No extension, no notification, no exception |

---

## 3. Name-format matrix (FE may send any of these)

All of the following must extend the subscription:

| Stored reward `Name`         | Normalized form                |
|------------------------------|--------------------------------|
| `Research Publication Reward`| `researchpublicationreward`    |
| `research_publication_reward`| `researchpublicationreward`    |
| `RESEARCH_PUBLICATION_REWARD`| `researchpublicationreward`    |
| `researchpublicationreward`  | `researchpublicationreward`    |
| `ResearchPublicationReward`  | `researchpublicationreward`    |
| `research-publication-reward`| `researchpublicationreward`    |
| `Research  Publication   Reward` | `researchpublicationreward`|
| `  research_publication_reward  ` | `researchpublicationreward`|

See `ARSPlatform.Tests/NormalizeRewardNameTests.cs` for the exhaustive
automated coverage.
