# Insurance Test Report

Date: 2026-10-13

## Automated tests

| Test | Result |
|---|---|
| Save company with administrator role and save contract plan | PASS |
| Reject claim decision amounts above submitted amount | PASS |
| Apply configured coverage, copay, and deductible | PASS |
| Insurance-focused test total | 4 passed, 0 failed |
| Existing stock/costing/permission regression subset | 33 passed, 0 failed |
| Main solution isolated-output build | PASS, 0 errors |
| Manual provider safety boundary | PASS by inspection; returns failure until an operator/provider response exists |

## Verification commands

```text
dotnet test RaccoonWarehouse.Tests/RaccoonWarehouse.Tests.csproj --no-restore --filter "FullyQualifiedName~Insurance"
dotnet build RaccoonWarehouse-master/RaccoonWarehouse.sln --no-restore -p:OutDir=build-verify/insurance-slice2/
```

## Not yet run

- SQL Server migration against a disposable database.
- Full POS insurance sale and mixed covered/uncovered invoice.
- Accounting receivable and settlement posting.
- Return after claim submission or settlement.
- External eligibility or claim submission.
- Manual WPF claims review with a production-like database.

These remain blocked by the missing POS/accounting workflow and the need for isolated test data/provider rules.

## Full-suite observation

The full suite completed with 135 passed and 8 failed. The failures are in existing voucher, purchase-expiry, sales-report, and accounting-report scenarios; none are Insurance tests. They should be triaged separately before release.
