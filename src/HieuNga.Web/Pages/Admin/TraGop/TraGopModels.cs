using System.ComponentModel.DataAnnotations;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using HieuNga.Infrastructure.Services;
using HieuNga.Web.Pages.Admin.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Web.Pages.Admin.TraGop;

public class BankInputModel
{
    public Guid? Id { get; set; }

    [Required]
    public Guid BankTypeId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên ngân hàng")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên viết tắt")]
    public string ShortName { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public string? BrandColor { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class NganHangIndexModel(IRepository<Bank> bankRepo, IRepository<BankType> typeRepo, IUnitOfWork uow, HieuNgaDbContext db) : PageModel
{
    public IReadOnlyList<BankRow> Items { get; private set; } = [];
    public SelectList TypeOptions { get; private set; } = null!;

    [BindProperty]
    public BankInputModel Input { get; set; } = new();

    public record BankRow(Guid Id, string Name, string ShortName, string TypeName, bool IsActive);

    public async Task OnGetAsync(Guid? editId, CancellationToken ct)
    {
        ViewData["Title"] = "Ngân hàng trả góp";
        await EnsureBankTypeAsync(ct);
        await LoadAsync(editId, ct);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Ngân hàng trả góp";
        await EnsureBankTypeAsync(ct);
        if (!ModelState.IsValid)
        {
            await LoadAsync(Input.Id, ct);
            return Page();
        }

        if (Input.Id.HasValue)
        {
            var entity = await bankRepo.GetByIdAsync(Input.Id.Value, ct);
            if (entity is null || entity.IsDeleted) return NotFound();
            Map(entity, Input);
            await bankRepo.UpdateAsync(entity, ct);
            this.SetSuccess("Đã cập nhật ngân hàng.");
        }
        else
        {
            await bankRepo.AddAsync(Map(new Bank(), Input), ct);
            this.SetSuccess("Đã thêm ngân hàng.");
        }

        await uow.SaveChangesAsync(ct);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await bankRepo.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();
        await bankRepo.SoftDeleteAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã xóa ngân hàng.");
        return RedirectToPage();
    }

    private async Task EnsureBankTypeAsync(CancellationToken ct)
    {
        if (!await db.BankTypes.AnyAsync(t => !t.IsDeleted, ct))
        {
            await typeRepo.AddAsync(new BankType
            {
                Name = "Ngân hàng thương mại",
                Slug = "ngan-hang-thuong-mai",
                DisplayOrder = 0,
                IsActive = true
            }, ct);
            await uow.SaveChangesAsync(ct);
        }
    }

    private async Task LoadAsync(Guid? editId, CancellationToken ct)
    {
        var types = await db.BankTypes.AsNoTracking().Where(t => !t.IsDeleted).OrderBy(t => t.DisplayOrder).ToListAsync(ct);
        TypeOptions = new SelectList(types, "Id", "Name", Input.BankTypeId == Guid.Empty ? types.FirstOrDefault()?.Id : Input.BankTypeId);

        Items = await db.Banks.AsNoTracking().Include(b => b.BankType)
            .Where(b => !b.IsDeleted).OrderBy(b => b.DisplayOrder)
            .Select(b => new BankRow(b.Id, b.Name, b.ShortName, b.BankType.Name, b.IsActive))
            .ToListAsync(ct);

        if (editId.HasValue)
        {
            var b = await bankRepo.GetByIdAsync(editId.Value, ct);
            if (b is not null && !b.IsDeleted)
                Input = new BankInputModel
                {
                    Id = b.Id, BankTypeId = b.BankTypeId, Name = b.Name, ShortName = b.ShortName,
                    LogoUrl = b.LogoUrl, Description = b.Description, BrandColor = b.BrandColor,
                    DisplayOrder = b.DisplayOrder, IsActive = b.IsActive
                };
            TypeOptions = new SelectList(types, "Id", "Name", Input.BankTypeId);
        }
        else if (Input.BankTypeId == Guid.Empty && types.Count > 0)
            Input.BankTypeId = types[0].Id;
    }

    private static Bank Map(Bank entity, BankInputModel input)
    {
        entity.BankTypeId = input.BankTypeId;
        entity.Name = input.Name.Trim();
        entity.ShortName = input.ShortName.Trim();
        entity.LogoUrl = input.LogoUrl;
        entity.Description = input.Description;
        entity.BrandColor = input.BrandColor;
        entity.DisplayOrder = input.DisplayOrder;
        entity.IsActive = input.IsActive;
        return entity;
    }
}

public class FinanceRateInputModel
{
    public Guid? Id { get; set; }

    [Required]
    public Guid BankId { get; set; }

    [Required]
    public string PlanName { get; set; } = "Trả góp tiêu chuẩn";

    [Range(0, 100)]
    public decimal MonthlyInterestRatePercent { get; set; }

    [Range(0, 100)]
    public int MinDownPaymentPercent { get; set; }

    [Range(0, 100)]
    public int MaxDownPaymentPercent { get; set; } = 70;

    public int MinTermMonths { get; set; } = 6;
    public int MaxTermMonths { get; set; } = 36;
    public string? SupportedTermsMonths { get; set; }
    public string? ProcessingFeeText { get; set; }
    public string? Note { get; set; }
    public string? TrustLabel { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public class LaiSuatIndexModel(IRepository<FinanceRate> rateRepo, IUnitOfWork uow, HieuNgaDbContext db) : PageModel
{
    public IReadOnlyList<RateRow> Items { get; private set; } = [];
    public SelectList BankOptions { get; private set; } = null!;

    [BindProperty]
    public FinanceRateInputModel Input { get; set; } = new();

    public record RateRow(Guid Id, string BankName, string PlanName, decimal Rate, bool IsDefault, bool IsActive);

    public async Task OnGetAsync(Guid? editId, CancellationToken ct)
    {
        ViewData["Title"] = "Lãi suất trả góp";
        await LoadAsync(editId, ct);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Lãi suất trả góp";
        if (!ModelState.IsValid)
        {
            await LoadAsync(Input.Id, ct);
            return Page();
        }

        if (Input.Id.HasValue)
        {
            var entity = await rateRepo.GetByIdAsync(Input.Id.Value, ct);
            if (entity is null || entity.IsDeleted) return NotFound();
            Map(entity, Input);
            await rateRepo.UpdateAsync(entity, ct);
            this.SetSuccess("Đã cập nhật lãi suất.");
        }
        else
        {
            await rateRepo.AddAsync(Map(new FinanceRate(), Input), ct);
            this.SetSuccess("Đã thêm lãi suất.");
        }

        await uow.SaveChangesAsync(ct);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await rateRepo.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();
        await rateRepo.SoftDeleteAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã xóa lãi suất.");
        return RedirectToPage();
    }

    private async Task LoadAsync(Guid? editId, CancellationToken ct)
    {
        var banks = await db.Banks.AsNoTracking().Where(b => !b.IsDeleted).OrderBy(b => b.DisplayOrder).ToListAsync(ct);
        BankOptions = new SelectList(banks, "Id", "Name", Input.BankId == Guid.Empty ? banks.FirstOrDefault()?.Id : Input.BankId);

        Items = await db.FinanceRates.AsNoTracking().Include(r => r.Bank)
            .Where(r => !r.IsDeleted).OrderBy(r => r.DisplayOrder)
            .Select(r => new RateRow(r.Id, r.Bank.Name, r.PlanName, r.MonthlyInterestRatePercent, r.IsDefault, r.IsActive))
            .ToListAsync(ct);

        if (editId.HasValue)
        {
            var r = await rateRepo.GetByIdAsync(editId.Value, ct);
            if (r is not null && !r.IsDeleted)
                Input = new FinanceRateInputModel
                {
                    Id = r.Id, BankId = r.BankId, PlanName = r.PlanName,
                    MonthlyInterestRatePercent = r.MonthlyInterestRatePercent,
                    MinDownPaymentPercent = r.MinDownPaymentPercent, MaxDownPaymentPercent = r.MaxDownPaymentPercent,
                    MinTermMonths = r.MinTermMonths, MaxTermMonths = r.MaxTermMonths,
                    SupportedTermsMonths = r.SupportedTermsMonths, ProcessingFeeText = r.ProcessingFeeText,
                    Note = r.Note, TrustLabel = r.TrustLabel, DisplayOrder = r.DisplayOrder,
                    IsDefault = r.IsDefault, IsActive = r.IsActive
                };
            BankOptions = new SelectList(banks, "Id", "Name", Input.BankId);
        }
        else if (Input.BankId == Guid.Empty && banks.Count > 0)
            Input.BankId = banks[0].Id;
    }

    private static FinanceRate Map(FinanceRate entity, FinanceRateInputModel input)
    {
        entity.BankId = input.BankId;
        entity.PlanName = input.PlanName.Trim();
        entity.MonthlyInterestRatePercent = input.MonthlyInterestRatePercent;
        entity.MinDownPaymentPercent = input.MinDownPaymentPercent;
        entity.MaxDownPaymentPercent = input.MaxDownPaymentPercent;
        entity.MinTermMonths = input.MinTermMonths;
        entity.MaxTermMonths = input.MaxTermMonths;
        entity.SupportedTermsMonths = input.SupportedTermsMonths;
        entity.ProcessingFeeText = input.ProcessingFeeText;
        entity.Note = input.Note;
        entity.TrustLabel = input.TrustLabel;
        entity.DisplayOrder = input.DisplayOrder;
        entity.IsDefault = input.IsDefault;
        entity.IsActive = input.IsActive;
        return entity;
    }
}
