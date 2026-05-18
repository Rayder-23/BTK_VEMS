namespace VEMS.Areas.AdminPortal.Controllers;

public abstract class FeeMgmtControllerBase : AdminBaseController
{
    protected int ResolveActorId() => ResolveStaffLoginUid() ?? 1;
}
