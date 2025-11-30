using System;

namespace CreatioAutoTestsPlaywright.Environment
{
    /// <summary>
    /// Simple configuration holder for a Creatio user (username + password).
    /// Used to create users inside CreatioEnvironment.
    /// </summary>
    public sealed class CreatioUserConfig
    {
        public string Username { get; }
        public string Password { get; }

        public CreatioUserConfig(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username must be provided.", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password must be provided.", nameof(password));
            }

            Username = username;
            Password = password;
        }
    }
}
