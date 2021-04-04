using System.Threading.Tasks;
using Qmmands;

namespace Disqord.Bot
{
    public sealed class RequireRoleAttribute : GuildOnlyAttribute
    {
        public Snowflake Id { get; }

        public RequireRoleAttribute(ulong id)
        {
            Id = id;
        }

        public override ValueTask<CheckResult> CheckAsync(CommandContext _)
        {
            var baseResult = base.CheckAsync(_).Result;
            if (!baseResult.IsSuccessful)
                return baseResult;

            var context = _ as DiscordCommandContext;
            return context.Member.Roles.ContainsKey(Id)
                ? CheckResult.Successful
                : CheckResult.Unsuccessful($"You do not have the required role {Id}.");

        }
    }
}
