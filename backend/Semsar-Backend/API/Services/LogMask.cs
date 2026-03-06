namespace API.Services;

public static class LogMask
{
    public static string Phone(string? phone)
    {
        if (string.IsNullOrEmpty(phone))
            return "null";

        if (phone.Length < 6)
            return "***MASKED***";

        return phone[..3] + new string('*', phone.Length - 6) + phone[^3..];
    }

    public static string Email(string? email)
    {
        if (string.IsNullOrEmpty(email))
            return "null";

        var at = email.IndexOf('@');
        if (at <= 1)
            return "***MASKED***";

        return email[..1] + "***" + email[at..];
    }
}
