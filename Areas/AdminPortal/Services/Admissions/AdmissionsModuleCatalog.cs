namespace VEMS.Areas.AdminPortal.Services.Admissions;

/// <summary>Legacy module discovery; layout uses <see cref="AdmissionsNavCatalog"/>.</summary>
public static class AdmissionsModuleCatalog
{
    public static bool TryGetByController(string controller) =>
        AdmissionsNavCatalog.IsAdmissionsController(controller);
}
