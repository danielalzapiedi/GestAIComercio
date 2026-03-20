using GestAI.Web.Dtos;

namespace GestAI.Web.Service
{
    public class AppState
    {
        public int? AccountId { get; private set; }
        public bool IsPlatformAdmin { get; private set; }
        public InternalUserRole? Role { get; private set; }
        public IReadOnlyCollection<SaasModule> AllowedModules { get; private set; } = Array.Empty<SaasModule>();

        public event Action? OnChange;

        public void SetAccess(CurrentUserAccessDto access)
        {
            AccountId = access.AccountId;
            Role = access.Role;
            IsPlatformAdmin = access.IsPlatformAdmin;
            AllowedModules = access.Modules.Where(x => x.Allowed).Select(x => x.Module).ToArray();
            OnChange?.Invoke();
        }

        public bool CanAccess(SaasModule module) => AllowedModules.Contains(module);
    }
}
