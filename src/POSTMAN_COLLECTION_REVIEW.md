# Postman Collection Review: Pool/Coin Parameter Analysis

## Executive Summary
✅ **The Postman collection is correctly structured and up-to-date.**

All endpoints that should have a `poolId` parameter already have it, and endpoints that should be pool-agnostic (cluster-wide) correctly do not have a pool parameter.

---

## Endpoint Analysis by Category

### 1. ✅ General Endpoints (NO poolId needed)
**Correctly designed as global/cluster-wide**

- `GET /api/help` - Returns all available API endpoints
- `GET /api/health-check` - Structured readiness/dependency check

**Reasoning**: These are informational/utility endpoints that apply to the entire cluster, not specific pools.

---

### 2. ✅ Network Endpoints (poolId REQUIRED) 
**Recently updated - ALL CORRECT**

- `GET /api/network/:poolId/overview` ✅
- `GET /api/network/:poolId/blocks` ✅
- `GET /api/network/:poolId/block/:height` ✅
- `GET /api/network/:poolId/top100` ✅

**Reasoning**: Each coin/pool has its own blockchain network with unique:
- Total supply
- Block time
- Network hashrate/difficulty
- Blockchain blocks
- Address balances

**Status**: Just updated in this session. All endpoints now correctly require poolId.

---

### 3. ✅ Cluster Endpoints (NO poolId needed)
**Correctly designed as cluster-wide aggregation**

- `GET /api/blocks?page=0&pageSize=15&state=Confirmed&state=Pending`

**Reasoning**: This endpoint aggregates blocks from **ALL pools** in the cluster. It's intentionally cross-pool for cluster-wide monitoring. The response includes `poolId` in each block object to identify which pool found it.

**Code Evidence**:
```csharp
// ClusterApiController.cs line 46-48
var blocks = (await cf.Run(con => blocksRepo.PageBlocksAsync(con, blockStates, page, pageSize, ct)))
    .Select(mapper.Map<Responses.Block>)
    .Where(x => enabledPools.Contains(x.PoolId))  // Returns all pools
    .ToArray();
```

---

### 4. ✅ Pool Endpoints (poolId REQUIRED)
**All correctly parameterized**

- `GET /api/pools` - List all pools (no poolId needed) ✅
- `GET /api/pools/:poolId` ✅
- `GET /api/pools/:poolId/performance` ✅
- `GET /api/pools/:poolId/miners` ✅
- `GET /api/pools/:poolId/blocks` ✅
- `GET /api/v2/pools/:poolId/blocks` ✅
- `GET /api/pools/:poolId/payments` ✅
- `GET /api/v2/pools/:poolId/payments` ✅

**Reasoning**: Pool-specific data and statistics. Each pool represents a different coin with separate:
- Statistics
- Performance metrics
- Miners
- Blocks found
- Payments made

---

### 5. ✅ Miner Endpoints (poolId REQUIRED)
**All correctly parameterized**

- `GET /api/pools/:poolId/miners/:address` ✅
- `GET /api/pools/:poolId/miners/:address/payments` ✅
- `GET /api/v2/pools/:poolId/miners/:address/payments` ✅
- `GET /api/pools/:poolId/miners/:address/balancechanges` ✅
- `GET /api/v2/pools/:poolId/miners/:address/balancechanges` ✅
- `GET /api/pools/:poolId/miners/:address/earnings/daily` ✅
- `GET /api/v2/pools/:poolId/miners/:address/earnings/daily` ✅
- `GET /api/pools/:poolId/miners/:address/performance` ✅
- `GET /api/pools/:poolId/miners/:address/settings` ✅
- `POST /api/pools/:poolId/miners/:address/settings` ✅

**Reasoning**: Miners mine specific coins. Their balances, payments, and statistics are pool/coin-specific.

---

### 6. ✅ Admin Endpoints (Mixed - Correctly Designed)
**GC/system endpoints - NO poolId needed**
- `GET /api/admin/gc` ✅
- `POST /api/admin/forcegc` ✅

**Reasoning**: Garbage collection and memory stats are server-wide, not pool-specific.

**Miner admin endpoints - poolId REQUIRED**
- `GET /api/admin/pools/:poolId/miners/:address/balance` ✅
- `GET /api/admin/pools/:poolId/miners/:address/settings` ✅
- `POST /api/admin/pools/:poolId/miners/:address/settings` ✅

**Reasoning**: Admin versions of miner endpoints - same reasoning as regular miner endpoints.

---

## Consistency Analysis

### Route Pattern Consistency ✅

**Pool-specific resources** follow consistent pattern:
```
/api/pools/:poolId/{resource}
/api/pools/:poolId/miners/:address/{resource}
/api/network/:poolId/{resource}
/api/admin/pools/:poolId/miners/:address/{resource}
```

**Cluster-wide resources** follow consistent pattern:
```
/api/{resource}
/api/admin/{resource}
```

---

## Default poolId Values

All pool-specific endpoints in the Postman collection use:
- **Default poolId**: `"acg"` (Aurum Gold)
- **Alternative example**: `"btc1"` (used in some descriptions for clarity)

**Recommendation**: ✅ Default values are appropriate for your use case.

---

## Potential Concerns & Recommendations

### ❓ Question: Should "Get All Pools" filter by coin type?

**Current**: `GET /api/pools?topMinersRange=24`

**Consideration**: This endpoint returns **all enabled pools** (all coins). This is correct for:
- Pool selection UI
- Multi-coin pool operators
- Cluster overview dashboards

**Recommendation**: ✅ **Keep as-is**. If you need coin-type filtering in the future, add an optional query parameter like `?coinType=Bitcoin` rather than requiring poolId in the route.

---

### ❓ Question: Admin endpoints - should they be at cluster level?

**Current Structure**:
```
/api/admin/gc                                  (cluster-wide)
/api/admin/forcegc                            (cluster-wide)
/api/admin/pools/:poolId/miners/:address/*    (pool-specific)
```

**Recommendation**: ✅ **Perfect as-is**. The distinction between cluster-wide admin operations (GC) and pool-specific admin operations (miner management) is semantically correct.

---

## V2 API Endpoints

Several endpoints have V2 versions:
- `GET /api/v2/pools/:poolId/blocks`
- `GET /api/v2/pools/:poolId/payments`
- `GET /api/v2/pools/:poolId/miners/:address/payments`
- `GET /api/v2/pools/:poolId/miners/:address/balancechanges`
- `GET /api/v2/pools/:poolId/miners/:address/earnings/daily`

**Difference**: V2 endpoints include page count in the response for better pagination UX.

**Recommendation**: ✅ V2 versioning is correct. Both versions properly include poolId.

---

## Missing Endpoints?

Based on common mining pool needs, you might consider adding:

### Optional Future Additions

1. **Network comparison endpoint** (if supporting multiple coins of same type):
   - `GET /api/network/compare?pools=btc1,btc2`

2. **Miner cross-pool stats** (if miners mine multiple pools):
   - `GET /api/miners/:address/summary` (aggregate across all pools)

3. **Pool search/filter**:
   - `GET /api/pools?coinType=Bitcoin&minHashrate=1000`

**Current Status**: ⚠️ These are **NOT required**. Your current API is complete for a standard multi-coin mining pool.

---

## Final Verdict

### ✅ Postman Collection Status: **PRODUCTION READY**

**Summary**:
- ✅ All pool-specific endpoints correctly use `:poolId` parameter
- ✅ Cluster-wide endpoints correctly do NOT use poolId
- ✅ Admin endpoints correctly distinguish between cluster-wide and pool-specific operations
- ✅ Route patterns are consistent and semantic
- ✅ Default values are appropriate
- ✅ V2 versioning is properly implemented
- ✅ Network endpoints updated in this session

**No additional changes needed before deployment.**

---

## Deployment Checklist

Before deploying the Network API changes:

1. ✅ **Backend Updated**: NetworkApiController.cs uses poolId parameter
2. ✅ **Tests Passing**: NetworkApiControllerTests validated
3. ✅ **Postman Collection Updated**: All Network endpoints include `:poolId`
4. ⚠️ **UI Update Required**: Frontend must update Network API calls to include poolId
5. 📄 **Migration Guide Available**: BREAKING_CHANGES_NETWORK_API.md created
6. 📄 **Test Documentation Available**: TEST_RESULTS_NETWORK_API.md created

---

## Example UI Update Required

### Before (will break after deployment):
```javascript
const response = await fetch('/api/network/overview');
```

### After (required):
```javascript
const poolId = 'acg'; // or get from app context
const response = await fetch(`/api/network/${poolId}/overview`);
```

See `BREAKING_CHANGES_NETWORK_API.md` for complete migration guide.
