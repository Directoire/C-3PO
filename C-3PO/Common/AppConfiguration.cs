using Microsoft.Extensions.Configuration;

namespace C_3PO.Common
{
    public class AppConfiguration
    {
        private string _token = null!;
        private string _database = null!;

        public AppConfiguration(IConfiguration configuration)
        {
            Token = configuration.GetValue<string>("Token");
            Database = configuration.GetValue<string>("Database");
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

        public string Database
        {
            get => _database;
            set
            {
                if (value == null)
                    throw new NullReferenceException("No database connection string was provided, please provide it through appsettings.json.");
                _database = value;
            }
        }
    }
}
