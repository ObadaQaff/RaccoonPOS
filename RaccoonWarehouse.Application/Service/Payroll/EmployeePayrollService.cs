using System.Data;
using System.Text.Json;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Employees;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Audit;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Employees;
using RaccoonWarehouse.Domain.Employees.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Payroll;

namespace RaccoonWarehouse.Application.Service.Payroll;

public sealed class EmployeePayrollService
{
    private readonly ApplicationDbContext _db;
    private readonly IEmployeeService _employees;
    private readonly IPermissionService _permissions;
    private readonly IUserSession _session;
    private readonly IPayrollSensitiveDataProtector _protector;
    private readonly PayrollCalculator _calculator;
    private readonly IMapper _mapper;
    private readonly IAuditLogService? _audit;
    private int? Actor=>_session.CurrentUser?.Id;
    public EmployeePayrollService(ApplicationDbContext db,IEmployeeService employees,IPermissionService permissions,IUserSession session,
        IPayrollSensitiveDataProtector protector,PayrollCalculator calculator,IMapper mapper,IAuditLogService? audit=null)
        =>(_db,_employees,_permissions,_session,_protector,_calculator,_mapper,_audit)=(db,employees,permissions,session,protector,calculator,mapper,audit);
    public async Task Require(string key){if(!await _permissions.HasCurrentUserPermissionAsync(key))throw new UnauthorizedAccessException("ليس لديك الصلاحية المطلوبة لهذه العملية.");}
    private async Task<T> Atomic<T>(Func<Task<T>> work)
    {
        await using var transaction=_db.Database.IsRelational()&&_db.Database.CurrentTransaction==null
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable):null;
        try{var value=await work();await _db.SaveChangesAsync();if(transaction!=null)await transaction.CommitAsync();return value;}
        catch{if(transaction!=null)await transaction.RollbackAsync();_db.ChangeTracker.Clear();throw;}
    }
    private async Task Audit(string action,string permission,int? id)
    {if(_audit!=null)await _audit.WriteAsync(new AuditEvent(action,PermissionKey:permission,EntityType:"EmployeePayroll",EntityId:id));}
    public async Task<List<RaccoonWarehouse.Domain.Branches.Branch>> BranchesAsync()
    {await Require("Employees.View");return await _db.Set<RaccoonWarehouse.Domain.Branches.Branch>().AsNoTracking().OrderBy(x=>x.Name).ToListAsync();}
    public EmployeeSalaryVersion SalaryTemplate(decimal basic,PayrollPolicy policy)=>new()
    {EffectiveFrom=new DateTime(DateTime.Today.Year,DateTime.Today.Month,1),BasicMonthlySalary=basic,StandardDailyHours=policy.DefaultDailyHours,
        WorkingDaysPerWeek=policy.DefaultWorkingDaysPerWeek,StandardMonthlyHours=policy.DefaultMonthlyHours,HourlyRateMethod=HourlyRateMethod.DailyWage,
        OvertimeMode=OvertimeMode.Manual,NormalOvertimeMultiplier=policy.MinimumNormalMultiplier,HolidayOvertimeMultiplier=policy.MinimumHolidayMultiplier,
        ContributionCategory=policy.Category,ChangeReason="إعداد الراتب الأولي"};
    public async Task<List<PayrollPolicy>> PoliciesAsync(){await Require("Payroll.View");return await _db.Set<PayrollPolicy>().AsNoTracking().OrderByDescending(x=>x.EffectiveFrom).ToListAsync();}
    public async Task<PayrollPolicy> PolicyAsync(string category,DateTime period)
    {
        var end=period.AddMonths(1).AddDays(-1);
        return await _db.Set<PayrollPolicy>().AsNoTracking().Where(x=>x.Category==category&&x.EffectiveFrom<=period&&x.EffectiveTo>=end)
            .OrderByDescending(x=>x.EffectiveFrom).FirstOrDefaultAsync()??throw new ArgumentException("لا توجد سياسة ضمان ورواتب سارية لهذه الفئة والشهر. حدّث السياسة وفق المصدر الرسمي.");
    }
    public async Task<int> AddPolicyAsync(PayrollPolicy policy)
    {
        await Require("Payroll.Configure");PayrollCalculator.ValidatePolicy(policy);
        return await Atomic(async()=>{
            var safeguard=await _db.Set<PayrollPolicy>().AsNoTracking().OrderByDescending(x=>x.Category=="JO-PRIVATE-GENERAL").ThenByDescending(x=>x.EffectiveFrom).FirstOrDefaultAsync();
            if(safeguard==null || policy.LegalDailyHours>safeguard.LegalDailyHours || policy.LegalWeeklyHours>safeguard.LegalWeeklyHours
                || policy.MinimumNormalMultiplier<safeguard.MinimumNormalMultiplier || policy.MinimumHolidayMultiplier<safeguard.MinimumHolidayMultiplier
                || policy.AdvanceDeductionMaximumPercent>safeguard.AdvanceDeductionMaximumPercent)
                throw new ArgumentException("لا يجوز خفض حماية ساعات العمل أو الإضافي أو حد السلف عبر إعدادات الرواتب؛ يلزم مرجع تشريعي وتحديث قانوني مستقل لأي تغيير في هذه الحدود.");
            if(policy.EffectiveFrom.Day!=1||policy.EffectiveTo.Date!=new DateTime(policy.EffectiveTo.Year,policy.EffectiveTo.Month,DateTime.DaysInMonth(policy.EffectiveTo.Year,policy.EffectiveTo.Month)))throw new ArgumentException("السياسة تبدأ بأول الشهر وتنتهي بآخر شهر.");
            if(await _db.Set<PayrollPolicy>().AnyAsync(x=>x.Category==policy.Category&&x.EffectiveFrom==policy.EffectiveFrom))throw new ArgumentException("توجد سياسة لهذه الفئة وتاريخ السريان. أضف إصداراً بتاريخ آخر.");
            policy.Id=0;policy.IsLegallyReviewed=false;policy.ReviewedBy=null;policy.CreatedDate=policy.UpdatedDate=DateTime.Now;
            _db.Add(policy);await _db.SaveChangesAsync();await Audit("Payroll.PolicyCreated","Payroll.Configure",policy.Id);return policy.Id;
        });
    }
    public async Task ReviewPolicyAsync(int id,string reason)
    {
        await Require("Payroll.Configure");if(string.IsNullOrWhiteSpace(reason))throw new ArgumentException("مرجع وسبب المراجعة القانونية مطلوبان.");
        await Atomic(async()=>{var policy=await _db.Set<PayrollPolicy>().FirstAsync(x=>x.Id==id);
            if(policy.IsLegallyReviewed)throw new ArgumentException("السياسة مراجعة مسبقاً؛ لا تعدل إصداراً تاريخياً.");
            policy.IsLegallyReviewed=true;policy.ReviewedBy=Actor;policy.ReviewReason=reason.Trim();policy.UpdatedDate=DateTime.Now;await Audit("Payroll.PolicyReviewed","Payroll.Configure",id);return true;});
    }
    public async Task<EmployeeSalaryVersion?> SalaryAsync(int employeeId,DateTime period)
    {
        await Require("Payroll.View");return await _db.Set<EmployeeSalaryVersion>().AsNoTracking().Where(x=>x.EmployeeId==employeeId&&x.EffectiveFrom<=period)
            .OrderByDescending(x=>x.EffectiveFrom).FirstOrDefaultAsync();
    }
    public async Task<List<EmployeeSalaryVersion>> SalaryHistoryAsync(int employeeId)
    {await Require("Payroll.View");return await _db.Set<EmployeeSalaryVersion>().AsNoTracking().Where(x=>x.EmployeeId==employeeId).OrderByDescending(x=>x.EffectiveFrom).ToListAsync();}
    public async Task<int> AddSalaryAsync(EmployeeSalaryVersion salary)
    {
        await Require("Payroll.Configure");
        return await Atomic(async()=>{
            if(salary.EffectiveFrom.Day!=1)throw new ArgumentException("سريان إعداد الراتب يبدأ بأول الشهر؛ يحسب التعيين أو انتهاء الخدمة داخل الشهر كنسبة مستقلة.");
            if(string.IsNullOrWhiteSpace(salary.ChangeReason))throw new ArgumentException("سبب إصدار إعداد الراتب مطلوب.");
            if(!await _db.Set<Employee>().AnyAsync(x=>x.Id==salary.EmployeeId))throw new ArgumentException("الموظف غير موجود.");
            var policy=await PolicyAsync(salary.ContributionCategory,salary.EffectiveFrom);PayrollCalculator.ValidateSalary(salary,policy);
            if(await _db.Set<EmployeeSalaryVersion>().AnyAsync(x=>x.EmployeeId==salary.EmployeeId&&x.EffectiveFrom==salary.EffectiveFrom))
                throw new ArgumentException("توجد إعدادات راتب لهذا الموظف وتاريخ السريان؛ أضف إصداراً لشهر جديد دون تغيير التاريخ السابق.");
            salary.Id=0;salary.Employee=null!;salary.ChangedBy=Actor;salary.CreatedDate=salary.UpdatedDate=DateTime.Now;
            _db.Add(salary);await _db.SaveChangesAsync();await Audit("Payroll.SalaryVersionCreated","Payroll.Configure",salary.EmployeeId);return salary.Id;
        });
    }
    public async Task<int> SaveProfileAsync(EmployeeCreateDto dto,EmployeeSalaryVersion? salary)
    {
        return await Atomic(async()=>{
            int id;
            if(dto.Id<=0){var result=await _employees.CreateAsync(dto);if(!result.Success)throw new ArgumentException(result.Message);id=result.Data.Id;}
            else{var update=_mapper.Map<EmployeeUpdateDto>(dto);var result=await _employees.UpdateAsync(update);if(!result.Success)throw new ArgumentException(result.Message);id=dto.Id;}
            if(salary!=null){salary.EmployeeId=id;await AddSalaryAsync(salary);}return id;
        });
    }
    public async Task<string?> RevealSocialSecurityNumberAsync(int employeeId)
    {
        await Require("Employees.SensitiveView");var value=await _db.Set<Employee>().Where(x=>x.Id==employeeId).Select(x=>x.SocialSecurityNumberEncrypted).FirstOrDefaultAsync();
        await Audit("Employee.SensitiveNumberViewed","Employees.SensitiveView",employeeId);return value==null?null:_protector.Unprotect(value);
    }
    public async Task<List<EmployeeReadDto>> EligibleAsync(int year,int month)
    {
        await Require("Payroll.View");var start=new DateTime(year,month,1);var end=start.AddMonths(1).AddDays(-1);
        var result=await _employees.GetListAsync();if(!result.Success)throw new ArgumentException(result.Message);
        return result.Data.Where(x=>(x.Status==EmployeeStatus.Active || (x.Status==EmployeeStatus.Terminated&&x.TerminationDate>=start))
            &&(!x.HireDate.HasValue||x.HireDate<=end)&&(!x.TerminationDate.HasValue||x.TerminationDate>=start)).ToList();
    }
    public async Task<(Employee Employee,EmployeeSalaryVersion Salary,PayrollPolicy Policy,PayrollCalculation Result)> PreviewAsync(int employeeId,int year,int month,PayrollInputs input)
    {
        await Require("Payroll.View");var period=new DateTime(year,month,1);
        var employee=await _db.Set<Employee>().FirstOrDefaultAsync(x=>x.Id==employeeId)??throw new ArgumentException("الموظف غير موجود.");
        var salary=await SalaryAsync(employeeId,period)??throw new ArgumentException("يلزم إعداد راتب مؤرخ لهذا الموظف قبل احتساب الرواتب.");
        var policy=await PolicyAsync(salary.ContributionCategory,period);
        if(input.NormalOvertimeOverride.HasValue||input.InsurableWageOverride.HasValue||input.Adjustments.Any(x=>x.Kind is PayrollAdjustmentKind.Deduction or PayrollAdjustmentKind.StatutoryDeduction))
            await Require("Payroll.Adjust");
        var result=_calculator.Calculate(employee,salary,policy,year,month,input);
        result.EmployeeName=employee.FullName;result.EmployeeCode=employee.Code;
        result.PolicySourceReference=policy.SourceReference;result.InsurableWageCeiling=policy.InsurableWageCeiling;
        result.NormalMultiplier=salary.NormalOvertimeMultiplier;result.HolidayMultiplier=salary.HolidayOvertimeMultiplier;
        result.SalaryEffectiveFrom=salary.EffectiveFrom;result.PolicyEffectiveFrom=policy.EffectiveFrom;
        return(employee,salary,policy,result);
    }
    public async Task<List<PayrollRecord>> RecordsAsync(int year,int month,int? employeeId=null)
    {
        await Require("Payroll.View");var rows=await _db.Set<EmployeePayroll>().IgnoreQueryFilters().AsNoTracking().Include(x=>x.Employee)
            .Where(x=>x.Year==year&&x.Month==month&&(!employeeId.HasValue||x.EmployeeId==employeeId)).ToListAsync();
        return rows.Select(ToRecord).ToList();
    }
    private static PayrollRecord ToRecord(EmployeePayroll row)
    {
        var result=JsonSerializer.Deserialize<PayrollCalculation>(row.SnapshotJson)!;
        return new(row.Id,string.IsNullOrWhiteSpace(result.EmployeeName)?row.Employee?.FullName??"":result.EmployeeName,row.Year,row.Month,row.State,row.Revision,
            JsonSerializer.Deserialize<PayrollInputs>(row.InputsJson)!,result,row.PolicyId,row.SalaryVersionId);
    }
    public async Task<PayrollRecord> SaveDraftAsync(int employeeId,int year,int month,PayrollInputs input,int id=0,Guid? revision=null)
    {
        await Require("Payroll.Calculate");return await Atomic(async()=>{
            var preview=await PreviewAsync(employeeId,year,month,input);
            EmployeePayroll row;
            if(id==0)
            {
                if(await _db.Set<EmployeePayroll>().AnyAsync(x=>x.EmployeeId==employeeId&&x.Year==year&&x.Month==month))throw new ArgumentException("يوجد كشف لهذا الموظف والشهر؛ افتح المسودة الموجودة بدلاً من التكرار.");
                row=new EmployeePayroll {EmployeeId=employeeId,Year=year,Month=month,CreatedDate=DateTime.Now};_db.Add(row);
            }
            else
            {
                row=await _db.Set<EmployeePayroll>().Include(x=>x.Adjustments).FirstAsync(x=>x.Id==id);
                if(row.State!=PayrollState.Draft)throw new ArgumentException("الكشف معتمد ولا يمكن استبداله؛ استخدم التصحيح الموثق.");
                if(row.EmployeeId!=employeeId||row.Year!=year||row.Month!=month||row.Revision!=revision)throw new DbUpdateConcurrencyException("تغير الكشف من مستخدم آخر؛ أعد تحميله.");
                _db.RemoveRange(row.Adjustments);row.Adjustments.Clear();
            }
            row.Employee=preview.Employee;row.SalaryVersionId=preview.Salary.Id;row.PolicyId=preview.Policy.Id;
            row.InputsJson=JsonSerializer.Serialize(input);row.SnapshotJson=JsonSerializer.Serialize(preview.Result);
            row.Revision=Guid.NewGuid();row.ModifiedBy=Actor;row.UpdatedDate=DateTime.Now;
            foreach(var x in input.Adjustments.Where(x=>x.Amount!=0m))row.Adjustments.Add(new PayrollAdjustment {Kind=x.Kind,Amount=x.Amount,DeductionBasis=x.DeductionBasis,Reason=x.Reason,LegalReference=x.LegalReference,Notes=x.Notes,AuthorizedBy=Actor,CreatedDate=DateTime.Now,UpdatedDate=DateTime.Now});
            await _db.SaveChangesAsync();await Audit("Payroll.DraftSaved","Payroll.Calculate",row.Id);return ToRecord(row);
        });
    }
    public async Task ApproveAsync(int id,Guid revision)
    {
        await Require("Payroll.Approve");await Atomic(async()=>{
            var row=await _db.Set<EmployeePayroll>().Include(x=>x.Policy).FirstAsync(x=>x.Id==id);
            if(row.State!=PayrollState.Draft||row.Revision!=revision)throw new ArgumentException("الكشف ليس مسودة حالية؛ أعد تحميله.");
            var input=JsonSerializer.Deserialize<PayrollInputs>(row.InputsJson)!;
            if(!input.AssumptionsReviewed)throw new ArgumentException("راجع الساعات وتصنيفات الإضافي والخصومات وافتراضات الفترة قبل الاعتماد.");
            if(!row.Policy.IsLegallyReviewed)throw new ArgumentException("السياسة لم تعتمد مراجعتها القانونية بعد.");
            var preview=await PreviewAsync(row.EmployeeId,row.Year,row.Month,input);
            if(preview.Salary.Id!=row.SalaryVersionId||preview.Policy.Id!=row.PolicyId||JsonSerializer.Serialize(preview.Result)!=row.SnapshotJson)
                throw new ArgumentException("تغيرت بيانات الحساب؛ أعد احتساب المسودة ومراجعتها قبل الاعتماد.");
            if(preview.Salary.SocialSecurityCovered&&(!input.InsurableWageReviewed||string.IsNullOrWhiteSpace(preview.Employee.SocialSecurityNumberEncrypted)))
                throw new ArgumentException("راجع الأجر الخاضع للضمان وأكمل رقم الضمان المشفر قبل الاعتماد.");
            row.State=PayrollState.Approved;row.ApprovedAt=DateTime.Now;row.ApprovedBy=Actor;row.Revision=Guid.NewGuid();row.UpdatedDate=DateTime.Now;
            await Audit("Payroll.Approved","Payroll.Approve",id);return true;
        });
    }
    public async Task CorrectAsync(int id,Guid revision,string reason)
    {
        await Require("Payroll.Correct");if(string.IsNullOrWhiteSpace(reason))throw new ArgumentException("سبب تصحيح الكشف المعتمد مطلوب.");
        await Atomic(async()=>{
            var row=await _db.Set<EmployeePayroll>().Include(x=>x.Adjustments).FirstAsync(x=>x.Id==id);
            if(row.State!=PayrollState.Approved||row.Revision!=revision)throw new ArgumentException("الكشف ليس نسخة معتمدة حالية.");
            _db.Add(new PayrollCorrection {EmployeePayrollId=id,OriginalRevision=row.Revision,OriginalInputsJson=row.InputsJson,OriginalSnapshotJson=row.SnapshotJson,
                OriginalAdjustmentsJson=JsonSerializer.Serialize(row.Adjustments.Select(x=>new{x.Kind,x.Amount,x.DeductionBasis,x.Reason,x.LegalReference,x.Notes,x.AuthorizedBy})),OriginalApprovedAt=row.ApprovedAt,OriginalApprovedBy=row.ApprovedBy,
                Reason=reason.Trim(),CorrectedBy=Actor,CreatedDate=DateTime.Now,UpdatedDate=DateTime.Now});
            var draftInputs=JsonSerializer.Deserialize<PayrollInputs>(row.InputsJson)!;draftInputs.AssumptionsReviewed=false;draftInputs.InsurableWageReviewed=false;row.InputsJson=JsonSerializer.Serialize(draftInputs);
            row.State=PayrollState.Draft;row.ApprovedAt=null;row.ApprovedBy=null;row.ModifiedBy=Actor;row.Revision=Guid.NewGuid();row.UpdatedDate=DateTime.Now;
            await Audit("Payroll.CorrectionOpened","Payroll.Correct",id);return true;
        });
    }
    public async Task<List<PayrollCorrection>> CorrectionsAsync(int id){await Require("Payroll.View");return await _db.Set<PayrollCorrection>().AsNoTracking().Where(x=>x.EmployeePayrollId==id).OrderByDescending(x=>x.CreatedDate).ToListAsync();}
    public async Task<List<EmployeeEmploymentHistory>> EmploymentHistoryAsync(int id){await Require("Employees.View");return await _db.Set<EmployeeEmploymentHistory>().AsNoTracking().Where(x=>x.EmployeeId==id).OrderByDescending(x=>x.CreatedDate).ToListAsync();}
}
