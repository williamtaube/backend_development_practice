    using System.Text.Json;

    // Simple User model used by the minimal API.
    // In a real project move models to their own files and use proper secure storage for passwords.
    public class User
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // Demo "encryption": Base64-encode the password to avoid storing it in plain text in logs.
        // NOTE: This is NOT secure storage; use a password hashing algorithm (e.g., Argon2, PBKDF2) in real apps.
        public void EncryptData()
        {
            if (!string.IsNullOrEmpty(Password))
            {
                Password = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Password));
            }
        }

        // Make logging and debugging output useful by serializing the object.
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }