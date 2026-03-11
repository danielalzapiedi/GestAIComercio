namespace GestAI.Web.Service
{
    public class AppState
    {
        public int? AccountId { get; private set; }

        public event Action? OnChange;

        public void SetDefaults(int? accountId)
        {
            AccountId = accountId;
            OnChange?.Invoke();
        }
    }
}
