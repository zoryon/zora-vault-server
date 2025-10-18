using Microsoft.Extensions.Options;
using ZoraVault.Configuration;

namespace ZoraVault.Extensions
{
    public static class ServiceCollectionExtensions
    {
        // Register Secrets configuration with validation
        public static IServiceCollection AddAppSecrets(this IServiceCollection services, IConfiguration configuration, bool validateOnStart = true)
        {
            services
                .AddOptions<Secrets>()
                .Configure(options =>
                {
                    options.ServerSecret = configuration["ZORAVAULT_SERVER_SECRET"] ?? string.Empty;
                    options.AccessTokenSecret = configuration["ACCESS_TOKEN_SECRET"] ?? string.Empty;
                    options.RefreshTokenSecret = configuration["REFRESH_TOKEN_SECRET"] ?? string.Empty;
                    options.ChallengesApiSecret = configuration["CHALLENGES_API_SECRET"] ?? string.Empty;
                    options.SessionApiSecret = configuration["SESSION_API_SECRET"] ?? string.Empty;
                })
                .ValidateDataAnnotations()
                .Validate(o =>
                    !string.IsNullOrWhiteSpace(o.ServerSecret) &&
                    !string.IsNullOrWhiteSpace(o.AccessTokenSecret) &&
                    !string.IsNullOrWhiteSpace(o.RefreshTokenSecret) &&
                    !string.IsNullOrWhiteSpace(o.ChallengesApiSecret) &&
                    !string.IsNullOrWhiteSpace(o.SessionApiSecret),
                    "Missing required token secrets")
                .ValidateOnStart(validateOnStart);

            // Directly inject Secrets instance
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<Secrets>>().Value);

            return services;
        }

        // Helper to conditionally enable ValidateOnStart
        private static OptionsBuilder<Secrets> ValidateOnStart(this OptionsBuilder<Secrets> builder, bool enable)
        {
            if (enable)
            {
                // disponibile su .NET 7+: Microsoft.Extensions.Options v7+
                #pragma warning disable CS0618
                builder.ValidateOnStart();
                #pragma warning restore CS0618
            }
            return builder;
        }
    }
}
