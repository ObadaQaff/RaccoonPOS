using RaccoonWarehouse.Domain.Employees;
using RaccoonWarehouse.Domain.Payroll;

namespace RaccoonWarehouse.Application.Service.Payroll;

public sealed class PayrollCalculator
{
    public static decimal Money(decimal amount) => Math.Round(amount, 3, MidpointRounding.AwayFromZero);
    public static void ValidatePolicy(PayrollPolicy p)
    {
        if (p.EffectiveFrom.Date > p.EffectiveTo.Date || p.SalaryDayDivisor <= 0m
            || p.LegalDailyHours <= 0m || p.LegalWeeklyHours <= 0m
            || p.MinimumNormalMultiplier < 1m || p.MinimumHolidayMultiplier < 1m
            || p.EmployeeSscPercent < 0m || p.EmployerSscPercent < 0m || p.EmployeeSscPercent + p.EmployerSscPercent > 100m
            || p.InsurableWageCeiling <= 0m || p.AdvanceDeductionMaximumPercent < 0m || p.AdvanceDeductionMaximumPercent > 100m
            || p.DefaultDailyHours<=0m || p.DefaultDailyHours>p.LegalDailyHours || p.DefaultWorkingDaysPerWeek is <1 or >7
            || p.DefaultDailyHours*p.DefaultWorkingDaysPerWeek>p.LegalWeeklyHours || p.DefaultMonthlyHours<=0m
            || p.WeekStartsOn is < 0 or > 6 || !Enum.IsDefined(p.ProrationMethod)
            || string.IsNullOrWhiteSpace(p.SourceReference) || string.IsNullOrWhiteSpace(p.Category))
            throw new ArgumentException("سياسة احتساب الراتب أو مرجعها غير صالح.");
    }
    public static void ValidateSalary(EmployeeSalaryVersion s, PayrollPolicy p)
    {
        ValidatePolicy(p);
        if (s.BasicMonthlySalary < 0m || s.FixedAllowances < 0m || s.StandardDailyHours <= 0m
            || s.StandardDailyHours > p.LegalDailyHours || s.WorkingDaysPerWeek is < 1 or > 7
            || s.StandardDailyHours * s.WorkingDaysPerWeek > p.LegalWeeklyHours || s.StandardMonthlyHours <= 0m
            || s.NormalOvertimeMultiplier < p.MinimumNormalMultiplier || s.HolidayOvertimeMultiplier < p.MinimumHolidayMultiplier
            || s.SocialSecurityEligibleWage < 0m || !Enum.IsDefined(s.HourlyRateMethod) || !Enum.IsDefined(s.OvertimeMode)
            || s.ContributionCategory != p.Category)
            throw new ArgumentException("إعدادات الراتب أو الساعات غير صالحة، أو تقل معاملات الإضافي عن السياسة القانونية المعتمدة.");
    }
    public (decimal Daily, decimal Hourly) WageRates(EmployeeSalaryVersion s, PayrollPolicy p)
    {
        ValidateSalary(s,p);
        var basis=s.BasicMonthlySalary+(p.IncludeFixedAllowancesInWageBasis?s.FixedAllowances:0m);
        var daily=basis/p.SalaryDayDivisor;
        var hourly=s.HourlyRateMethod==HourlyRateMethod.MonthlyHours?basis/s.StandardMonthlyHours:daily/s.StandardDailyHours;
        return (Math.Round(daily,6,MidpointRounding.AwayFromZero),Math.Round(hourly,6,MidpointRounding.AwayFromZero));
    }
    public PayrollCalculation Calculate(Employee employee, EmployeeSalaryVersion s, PayrollPolicy p, int year, int month, PayrollInputs input)
    {
        if (year is < 1900 or > 9998 || month is < 1 or > 12) throw new ArgumentException("شهر أو سنة الراتب غير صالح.");
        ValidateSalary(s,p);
        var start=new DateTime(year,month,1);var end=start.AddMonths(1).AddDays(-1);
        if(s.EffectiveFrom.Date>start || p.EffectiveFrom.Date>start || p.EffectiveTo.Date<end)
            throw new ArgumentException("لا توجد إعدادات راتب وسياسة سارية لكامل شهر الراتب.");
        var activeFrom=employee.HireDate?.Date>start?employee.HireDate.Value.Date:start;
        var activeTo=employee.TerminationDate?.Date<end?employee.TerminationDate.Value.Date:end;
        if(activeFrom>activeTo)throw new ArgumentException("الموظف غير مستحق لهذا الشهر بحسب تاريخ التعيين أو انتهاء العمل.");
        var days=(activeTo-activeFrom).Days+1;
        var full=activeFrom==start&&activeTo==end;
        var fraction=full?1m:Math.Min(1m,days/(p.ProrationMethod==ProrationMethod.CalendarMonth?(decimal)DateTime.DaysInMonth(year,month):p.SalaryDayDivisor));
        if(input.TotalWorkedHours<0m || input.NormalOvertimeHours<0m || input.HolidayOvertimeHours<0m
            || input.NormalOvertimeOverride<0m || input.UnpaidLeaveDays<0m || input.UnpaidLeaveDays>days
            || input.InsurableWageOverride<0m || input.Adjustments.Any(x=>x.Amount<0m || !Enum.IsDefined(x.Kind)))
            throw new ArgumentException("الساعات والأيام والمبالغ يجب أن تكون غير سالبة وضمن فترة الاستحقاق.");
        if(input.Adjustments.Any(x=>x==null))throw new ArgumentException("سطر تعديل الراتب غير صالح.");
        if(input.TotalWorkedHours>days*24m || input.NormalOvertimeHours+input.HolidayOvertimeHours>days*24m)
            throw new ArgumentException("عدد الساعات يتجاوز عدد الساعات الممكنة خلال الفترة.");
        if(input.NormalOvertimeOverride.HasValue && string.IsNullOrWhiteSpace(input.OvertimeReviewReason))
            throw new ArgumentException("سبب مراجعة وتصحيح تصنيف الإضافي مطلوب.");
        if(input.InsurableWageOverride.HasValue && string.IsNullOrWhiteSpace(input.InsurableWageReason))
            throw new ArgumentException("سبب تعديل الأجر الخاضع للضمان مطلوب.");
        int scheduledDays=0;
        for(var date=activeFrom;date<=activeTo;date=date.AddDays(1))
            if(((int)date.DayOfWeek-p.WeekStartsOn+7)%7<s.WorkingDaysPerWeek)scheduledDays++;
        var ordinary=Math.Min(s.StandardMonthlyHours*fraction,scheduledDays*s.StandardDailyHours);
        decimal normal=input.NormalOvertimeHours;
        if(s.OvertimeMode==OvertimeMode.MonthlyAggregate)
        {
            if(input.HolidayOvertimeHours>input.TotalWorkedHours)throw new ArgumentException("ساعات العطل لا تتجاوز إجمالي الساعات المدخلة.");
            normal=Math.Max(0m,input.TotalWorkedHours-input.HolidayOvertimeHours-ordinary);
        }
        normal=input.NormalOvertimeOverride??normal;
        if(s.OvertimeMode==OvertimeMode.MonthlyAggregate&&normal+input.HolidayOvertimeHours>input.TotalWorkedHours)
            throw new ArgumentException("ساعات الإضافي المصححة تتجاوز إجمالي الساعات المدخلة.");
        if(normal+input.HolidayOvertimeHours>days*24m)throw new ArgumentException("الإضافي المصحح يتجاوز ساعات الفترة.");
        var (daily,hourly)=WageRates(s,p);
        var r=new PayrollCalculation {EmployeeId=employee.Id,Year=year,Month=month,EligibleCalendarDays=days,EmploymentFraction=fraction,
            DailyWage=daily,HourlyWage=hourly,OrdinaryHoursForPeriod=ordinary,
            BasicSalary=Money(s.BasicMonthlySalary*fraction),FixedAllowances=Money(s.FixedAllowances*fraction),
            NormalOvertimeHours=normal,HolidayOvertimeHours=input.HolidayOvertimeHours,
            NormalOvertime=Money(hourly*normal*s.NormalOvertimeMultiplier),HolidayOvertime=Money(hourly*input.HolidayOvertimeHours*s.HolidayOvertimeMultiplier),
            UnpaidLeaveDeduction=Money(daily*input.UnpaidLeaveDays),
            EmployeeSscPercent=s.SocialSecurityCovered?p.EmployeeSscPercent:0m,EmployerSscPercent=s.SocialSecurityCovered?p.EmployerSscPercent:0m};
        foreach(var a in input.Adjustments)
        {
            if(a.Amount==0m)continue;
            if(string.IsNullOrWhiteSpace(a.Reason))throw new ArgumentException("سبب كل إضافة أو خصم مطلوب.");
            if(a.Kind is PayrollAdjustmentKind.Deduction or PayrollAdjustmentKind.StatutoryDeduction)
            {
                if(a.DeductionBasis==DeductionBasis.None || !Enum.IsDefined(a.DeductionBasis) || string.IsNullOrWhiteSpace(a.LegalReference))
                    throw new ArgumentException("يلزم أساس قانوني ومرجع موثق لكل خصم؛ لا يسمح بالخصومات الاعتباطية.");
                r.AdditionalDeductions+=Money(a.Amount);
            }
            else if(a.Kind==PayrollAdjustmentKind.Bonus)r.Bonuses+=Money(a.Amount);
            else if(a.Kind==PayrollAdjustmentKind.Allowance)r.AdditionalAllowances+=Money(a.Amount);
            else if(a.Kind==PayrollAdjustmentKind.EmployerCost)r.OtherEmployerCosts+=Money(a.Amount);
        }
        var advance=input.Adjustments.Where(x=>x.Kind is PayrollAdjustmentKind.Deduction or PayrollAdjustmentKind.StatutoryDeduction
            && x.DeductionBasis==DeductionBasis.AdvanceRecovery).Sum(x=>x.Amount);
        if(advance>Money((r.BasicSalary+r.FixedAllowances)*p.AdvanceDeductionMaximumPercent/100m))
            throw new ArgumentException("قسط استرداد السلفة يتجاوز الحد المسموح في السياسة القانونية.");
        r.GrossEarnings=Money(r.BasicSalary+r.FixedAllowances+r.NormalOvertime+r.HolidayOvertime+r.Bonuses+r.AdditionalAllowances);
        if(s.SocialSecurityCovered)
        {
            var wage=input.InsurableWageOverride??s.SocialSecurityEligibleWage;
            if(!wage.HasValue)throw new ArgumentException("الأجر الخاضع للضمان مطلوب للموظف المشمول.");
            // Explicit independent SSC base; overtime/unpaid leave never silently alter it.
            r.InsurableWage=Math.Min(wage.Value,p.InsurableWageCeiling);
            r.EmployeeSsc=Money(r.InsurableWage*p.EmployeeSscPercent/100m);
            r.EmployerSsc=Money(r.InsurableWage*p.EmployerSscPercent/100m);
        }
        if(r.UnpaidLeaveDeduction>r.BasicSalary+r.FixedAllowances)throw new ArgumentException("خصم الإجازة يتجاوز أجر فترة الاستحقاق.");
        r.NetSalary=Money(r.GrossEarnings-r.UnpaidLeaveDeduction-r.AdditionalDeductions-r.EmployeeSsc);
        if(r.NetSalary<0m)throw new ArgumentException("الخصومات تتجاوز مستحقات الموظف.");
        r.EmployerCost=Money(r.GrossEarnings-r.UnpaidLeaveDeduction+r.EmployerSsc+r.OtherEmployerCosts);
        r.CalculationAssumptions=$"العملة: دينار أردني. قاسم اليوم: {p.SalaryDayDivisor}. أساس الأجر: {(p.IncludeFixedAllowancesInWageBasis?"الأساسي والبدلات الثابتة":"الأساسي")}. "
            +"خصم الإجازة مرة واحدة. أساس الضمان مستقل ومراجع، ولا يتغير تلقائياً بالإضافي أو الإجازة. "
            +(s.OvertimeMode==OvertimeMode.MonthlyAggregate?"الإضافي التلقائي تقدير من إجمالي الشهر وجدول افتراضي؛ لا يثبت الامتثال للإضافي اليومي ويلزم مراجعته. ":"")
            +(!full?"الفترة جزئية؛ يلزم مراجعة توزيع الراتب وأساس الضمان. ":"");
        return r;
    }
}
