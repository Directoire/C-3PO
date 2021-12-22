using Microsoft.Extensions.Configuration;

namespace C_3PO.Common
{
    public class AppConfiguration
    {
        private string _token = null!;
        private ulong _guild = 0!;
        private ulong _rulesChannel = 0!;
        private ulong _rulesMessage = 0!;
        private ulong _onboardingCategory = 0!;
        private ulong _onboardingRole = 0!;
        private ulong _welcomeChannel = 0!;
        private ulong _ejectedRole = 0!;
        private ulong _ejectedChannel = 0!;
        private ulong _civilianRole = 0!;

        public AppConfiguration(IConfiguration configuration)
        {
            Token = configuration.GetValue<string>("Token");
            Guild = configuration.GetValue<ulong>("Guild");
            RulesChannel = configuration.GetValue<ulong>("RulesChannel");
            RulesMessage = configuration.GetValue<ulong>("RulesMessage");
            OnboardingRole = configuration.GetValue<ulong>("OnboardingRole");
            OnboardingCategory = configuration.GetValue<ulong>("OnboardingCategory");
            WelcomeChannel = configuration.GetValue<ulong>("WelcomeChannel");
            EjectedRole = configuration.GetValue<ulong>("EjectedRole");
            CivilianRole = configuration.GetValue<ulong>("CivilianRole");
            EjectedChannel = configuration.GetValue<ulong>("EjectedChannel");
        }

        public string Token
        {
            get => _token;
            set
            {
                if (value == null)
                    throw new NullReferenceException("No token was provided, please provide it through appsettings.json.");
                _token = value;
            }
        }

        public ulong Guild
        {
            get => _guild;
            set
            {
                if (value == 0)
                    throw new NullReferenceException("No guild was provided, please provide it through appsettings.json.");
                _guild = value;
            }
        }

        public ulong RulesChannel
        {
            get => _rulesChannel;
            set
            {
                if (value == 0)
                    throw new NullReferenceException("No rules channel was provided, please provide it through appsettings.json.");
                _rulesChannel = value;
            }
        }

        public ulong RulesMessage
        {
            get => _rulesMessage;
            set
            {
                if (value == 0)
                    throw new NullReferenceException("No rules message was provided, please provide it through appsettings.json.");
                _rulesMessage = value;
            }
        }

        public ulong OnboardingRole
        {
            get => _onboardingRole;
            set
            {
                if (value == 0)
                    throw new NullReferenceException("No onboarding role was provided, please provide it through appsettings.json.");
                _onboardingRole = value;
            }
        }

        public ulong OnboardingCategory
        {
            get => _onboardingCategory;
            set
            {
                if (value == 0)
                    throw new NullReferenceException("No onboarding category was provided, please provide it through appsettings.json.");
                _onboardingCategory = value;
            }
        }

        public ulong WelcomeChannel
        {
            get => _welcomeChannel;
            set
            {
                if (value == 0)
                    throw new NullReferenceException("No welcome channel was provided, please provide it through appsettings.json.");
                _welcomeChannel = value;
            }
        }

        public ulong EjectedRole
        {
            get => _ejectedRole;
            set
            {
                if (value == 0)
                    throw new NullReferenceException("No ejected role was provided, please provide it through appsettings.json.");
                _ejectedRole = value;
            }
        }

        public ulong CivilianRole
        {
            get => _civilianRole;
            set
            {
                if (value == 0)
                    throw new NullReferenceException("No civilian role was provided, please provide it through appsettings.json.");
                _civilianRole = value;
            }
        }

        public ulong EjectedChannel
        {
            get => _ejectedChannel;
            set
            {
                if (value == 0)
                    throw new NullReferenceException("No ejected channel was provided, please provide it through appsettings.json.");
                _ejectedChannel = value;
            }
        }
    }
}
