using Microsoft.Extensions.Configuration;

namespace C_3PO.Common
{
    public class AppConfiguration
    {
        private string _token = null!;
        private ulong _guild = 0!;
        private ulong _rulesChannel = 0!;
        private ulong _rulesMessage = 0!;
        private Categories _categories = null!;
        private ulong _onboardingRole = 0!;

        public AppConfiguration(IConfiguration configuration)
        {
            Token = configuration.GetValue<string>("Token");
            Guild = configuration.GetValue<ulong>("Guild");
            RulesChannel = configuration.GetValue<ulong>("RulesChannel");
            RulesMessage = configuration.GetValue<ulong>("RulesMessage");
            OnboardingRole = configuration.GetValue<ulong>("OnboardingRole");

            var categories = configuration.GetSection("Categories");
            Categories = new Categories();
            Categories.Onboarding = categories.GetValue<ulong>("Onboarding");
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

        public Categories Categories
        {
            get => _categories;
            set
            {
                if (value == null)
                    throw new NullReferenceException("No categories were provided, please provide them through appsettings.json.");
                _categories = value;
            }
        }
    }

    public class Categories
    {
        private ulong _onboarding;

        public ulong Onboarding
        {
            get => _onboarding;
            set
            {
                if (value == 0)
                    throw new NullReferenceException("The outer rim category was not provided, please provide it through appsettings.json.");
                _onboarding = value;
            }
        }
    }
}
