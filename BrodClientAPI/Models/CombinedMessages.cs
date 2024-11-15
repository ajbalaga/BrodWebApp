namespace BrodClientAPI.Models
{
    public class CombinedMessages
    {
        public List<ClientMessage>? ClientMessages { get; set; }
        public List<TradieMessage>? TradieMessages { get; set; }
    }
}
