# Breaking Changes: Network API Endpoints

## Summary
The Network API endpoints have been updated to accept a `poolId` route parameter instead of using a hardcoded pool selection. This change makes the API consistent with other pool-specific endpoints and properly supports multi-coin mining pools.

## Changed Endpoints

### Before (Old Routes)
```
GET /api/network/overview
GET /api/network/blocks
GET /api/network/block/{height}
GET /api/network/top100
```

### After (New Routes)
```
GET /api/network/{poolId}/overview
GET /api/network/{poolId}/blocks
GET /api/network/{poolId}/block/{height}
GET /api/network/{poolId}/top100
```

## Migration Guide

### Example: Get Network Overview
**Old:**
```
GET https://portal.aurumofficial.net/api/network/overview
```

**New:**
```
GET https://portal.aurumofficial.net/api/network/acg/overview
```

### Example: Get Network Blocks
**Old:**
```
GET https://portal.aurumofficial.net/api/network/blocks
```

**New:**
```
GET https://portal.aurumofficial.net/api/network/acg/blocks
```

### Example: Get Block by Height
**Old:**
```
GET https://portal.aurumofficial.net/api/network/block/1000
```

**New:**
```
GET https://portal.aurumofficial.net/api/network/acg/block/1000
```

### Example: Get Top 100 Holders
**Old:**
```
GET https://portal.aurumofficial.net/api/network/top100
```

**New:**
```
GET https://portal.aurumofficial.net/api/network/acg/top100
```

## UI Code Changes Required

If your UI currently uses a base path like `/api/network`, you'll need to update it to include the pool ID:

```javascript
// Before
const networkApiBase = '/api/network';
const overview = await fetch(`${networkApiBase}/overview`);

// After
const poolId = 'acg'; // or get from context/config
const networkApiBase = `/api/network/${poolId}`;
const overview = await fetch(`${networkApiBase}/overview`);
```

## What Was Changed

### Backend Changes
1. **NetworkApiController.cs**
   - Updated route attribute from `[Route("api/network")]` to `[Route("api/network/{poolId}")]`
   - Added `string poolId` parameter to all endpoint methods
   - Replaced `GetPrimaryPool()` calls with `GetPool(poolId)` to use the existing validation method
   - Removed the hardcoded `GetPrimaryPool()` method that looked for "acg" pool

2. **NetworkApiControllerTests.cs**
   - Updated all test routes to include `/acg/` in the path

3. **postman_collection.json**
   - Updated all Network endpoints to include `:poolId` path variable
   - Set default `poolId` value to "acg" in all Network endpoints

## Benefits of This Change
1. **Consistency**: Network endpoints now follow the same pattern as other pool-specific endpoints
2. **Multi-coin Support**: Proper support for querying network information for different coins/pools
3. **No Hardcoding**: Removes the hardcoded "acg" pool ID from the controller
4. **Flexibility**: UIs can now query network information for any configured pool

## Notes
- The `poolId` parameter is validated using the existing `GetPool(poolId)` method from `ApiControllerBase`
- Invalid pool IDs will return a 404 Not Found error
- The pool must be enabled in the cluster configuration
- The pool must have configured daemon endpoints
